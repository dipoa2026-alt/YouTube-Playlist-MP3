using System.Runtime.CompilerServices;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;

const string defaultUrl = "https://www.youtube.com/playlist?list=PLTUMWmlKOvqaPvYe2Oq_N8OkTMxLjN6ol";

var options = ParseArgs(args);
var url = options.GetValueOrDefault("--url", defaultUrl);
var outputDirectory = Path.GetFullPath(options.GetValueOrDefault("--output", "downloads"));
var limit = int.TryParse(options.GetValueOrDefault("--limit", "1"), out var parsedLimit)
    ? parsedLimit
    : 1;

if (limit < 0)
{
    Console.Error.WriteLine("--limit must be zero (all items) or a positive integer.");
    return 2;
}

Directory.CreateDirectory(outputDirectory);
var reportPath = Path.Combine(outputDirectory, "download-report.csv");
var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

using var youtube = new YoutubeClient();
var videos = new List<IVideo>();

try
{
    if (url.Contains("list=", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Reading playlist metadata...");
        await foreach (var item in youtube.Playlists.GetVideosAsync(url, cancellation.Token))
            videos.Add(item);
    }
    else
    {
        videos.Add(await youtube.Videos.GetAsync(url, cancellation.Token));
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Could not read the video/playlist: {ex.Message}");
    return 1;
}

if (videos.Count == 0)
{
    Console.Error.WriteLine("No accessible videos were found.");
    return 1;
}

var selected = limit == 0 ? videos : videos.Take(limit).ToList();
Console.WriteLine($"Found {videos.Count} accessible item(s); processing {selected.Count}.");
Console.WriteLine($"Output: {outputDirectory}");
Console.WriteLine("Only download content you own or are authorized to download.");

var results = new List<(string Id, string Title, string Status, string Details)>();
var succeeded = 0;
var skipped = 0;
var failed = 0;

for (var index = 0; index < selected.Count; index++)
{
    cancellation.Token.ThrowIfCancellationRequested();
    var video = selected[index];
    var safeTitle = SanitizeFileName(video.Title);
    var filePath = Path.Combine(outputDirectory, $"{index + 1:000} - {safeTitle} [{video.Id}].mp3");

    Console.WriteLine($"[{index + 1}/{selected.Count}] {video.Title}");

    if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
    {
        Console.WriteLine("  Already downloaded; skipping.");
        skipped++;
        results.Add((video.Id, video.Title, "skipped", "Existing MP3"));
        continue;
    }

    try
    {
        await youtube.Videos.DownloadAsync(
            video.Url,
            filePath,
            o => o.SetContainer("mp3"),
            cancellationToken: cancellation.Token
        );

        if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            throw new InvalidOperationException("The converter returned without producing an MP3 file.");

        succeeded++;
        Console.WriteLine("  OK");
        results.Add((video.Id, video.Title, "success", ""));
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"  FAILED: {ex.Message}");
        results.Add((video.Id, video.Title, "failed", ex.Message));
    }
}

await WriteReportAsync(reportPath, results);
Console.WriteLine();
Console.WriteLine($"Finished. Success: {succeeded}; skipped: {skipped}; failed: {failed}.");
Console.WriteLine($"Report: {reportPath}");

return failed == 0 && succeeded + skipped > 0 ? 0 : 1;

static Dictionary<string, string> ParseArgs(string[] arguments)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < arguments.Length; i++)
    {
        if (!arguments[i].StartsWith("--", StringComparison.Ordinal))
            continue;
        if (i + 1 >= arguments.Length || arguments[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Missing value for {arguments[i]}");
            Environment.Exit(2);
        }
        values[arguments[i]] = arguments[++i];
    }
    return values;
}

static string SanitizeFileName(string value)
{
    var invalid = Path.GetInvalidFileNameChars();
    var cleaned = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
    return string.IsNullOrWhiteSpace(cleaned) ? "audio" : cleaned[..Math.Min(cleaned.Length, 120)];
}

static async Task WriteReportAsync(
    string path,
    IEnumerable<(string Id, string Title, string Status, string Details)> rows
)
{
    await using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
    await writer.WriteLineAsync("video_id,title,status,details");
    foreach (var row in rows)
    {
        var values = new[] { row.Id, row.Title, row.Status, row.Details };
        await writer.WriteLineAsync(string.Join(",", values.Select(CsvEscape)));
    }
}

static string CsvEscape(string value) =>
    "\"" + value.Replace("\"", "\"\"") + "\"";
