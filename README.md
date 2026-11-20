# YouTube Playlist → MP3 (GitHub Actions)

Convert a playlist to MP3 with **yt-dlp** and **FFmpeg**, then download the resulting ZIP from the workflow artifacts.

> **Important:** Use this workflow only for videos you own or are explicitly authorized to download. Respect copyright and YouTube's Terms of Service. This repository does not bypass private access, DRM, or other access controls.

## Run it

1. Open the **Actions** tab of this repository.
2. Select **Download playlist as MP3**.
3. Click **Run workflow**.
4. Keep the default playlist URL, or paste another playlist/video URL you are authorized to download.
5. Once the run finishes, open the run summary and download the **playlist-mp3** artifact.

The artifact is a ZIP containing the MP3 files. GitHub keeps it for 7 days.

## Files

- `main.py`: runs yt-dlp and converts audio to MP3.
- `.github/workflows/download.yml`: manually triggered GitHub Actions workflow.

## Notes

- The default URL is the playlist supplied when this repository was set up.
- Videos unavailable to the runner (for example, private, age-restricted, or region-restricted videos) may be skipped.
- A failed/unavailable item does not stop the rest of the playlist.
- GitHub-hosted runners have execution and artifact-size limits. Very large playlists may need to be split into smaller batches.
- No YouTube cookies, passwords, or personal account credentials are requested or stored.
