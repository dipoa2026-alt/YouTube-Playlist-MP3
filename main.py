#!/usr/bin/env python3
"""Extract MP3 audio from a YouTube playlist the user is authorized to download.

This tool uses yt-dlp and FFmpeg. Use it only for content you own or have
permission to download, and in accordance with YouTube's terms.
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

DEFAULT_PLAYLIST = "https://www.youtube.com/playlist?list=PLTUMWmlKOvqaPvYe2Oq_N8OkTMxLjN6ol"


def safe_folder_name(value: str) -> str:
    value = re.sub(r"[\\/:*?\"<>|]+", "_", value).strip(" ._")
    return value[:100] or "playlist"


def main() -> int:
    parser = argparse.ArgumentParser(description="Convert an authorized YouTube playlist to MP3.")
    parser.add_argument("--url", default=os.getenv("PLAYLIST_URL", DEFAULT_PLAYLIST),
                        help="Playlist or video URL (default: configured playlist)")
    parser.add_argument("--output", default="downloads", help="Output directory")
    args = parser.parse_args()

    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    archive = output / "downloaded.txt"

    command = [
        sys.executable, "-m", "yt_dlp",
        "--yes-playlist",
        "--ignore-errors",
        "--no-abort-on-error",
        "--extract-audio",
        "--audio-format", "mp3",
        "--audio-quality", "0",
        "--embed-metadata",
        "--download-archive", str(archive),
        "--output", str(output / "%(playlist_index)03d - %(title)s.%(ext)s"),
        "--windows-filenames",
        "--trim-filenames", "180",
        args.url,
    ]
    print("Starting audio extraction. Files will be saved under:", output.resolve(), flush=True)
    try:
        result = subprocess.run(command, check=False)
    except KeyboardInterrupt:
        print("Interrupted by user.", file=sys.stderr)
        return 130

    mp3_files = list(output.rglob("*.mp3"))
    if not mp3_files:
        print(
            "ERROR: yt-dlp did not produce any MP3 files. "
            "Check the workflow logs; YouTube may be requiring sign-in or blocking the runner.",
            file=sys.stderr,
        )
        return result.returncode or 1

    print(f"Successfully created {len(mp3_files)} MP3 file(s).", flush=True)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
