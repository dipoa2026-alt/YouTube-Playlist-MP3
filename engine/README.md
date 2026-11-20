# Playlist MP3 engine (prototype)

This is an experimental local .NET 8 console engine using the maintained
[YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) packages and FFmpeg
through YoutubeExplode.Converter.

## Requirements

- .NET 8 SDK
- FFmpeg installed and available as `ffmpeg` on PATH
- Network access to YouTube
- Rights/authorization to download the selected content

## Test one authorized video

From the repository root:

```powershell
dotnet run --project engine/PlaylistMp3.csproj -- --url "https://www.youtube.com/watch?v=YOUR_AUTHORIZED_VIDEO_ID" --limit 1 --output downloads
```

## Test one item from an authorized playlist

```powershell
dotnet run --project engine/PlaylistMp3.csproj -- --url "YOUR_PLAYLIST_URL" --limit 1 --output downloads
```

## Process all accessible playlist items

Only after the one-item test succeeds:

```powershell
dotnet run --project engine/PlaylistMp3.csproj -- --url "YOUR_PLAYLIST_URL" --limit 0 --output downloads
```

The default limit is deliberately set to 1. Set `--limit 0` explicitly to
process the full playlist. Existing non-empty MP3 files are skipped. A
`download-report.csv` file records successful, skipped, and failed items.

This prototype does not bypass sign-in, age, geographic, DRM, or other access
restrictions. YoutubeExplode uses YouTube's internal interfaces, which can
change and may also be rate-limited or blocked. Do not add account passwords
or cookies to source code or commit them to Git.
