#!/usr/bin/env python3
"""Publish a portable x64 Voxen folder, including verified FFmpeg and native SDL3 assets."""
import argparse
import hashlib
import io
from pathlib import Path
import shutil
import subprocess
import tarfile
import urllib.request
import zipfile

RELEASE = "autobuild-2026-10-06-13-06"
BUILDS = {
    "linux-x64": ("ffmpeg-n9.0.2-22-g46d8f462ee-linux64-gpl-9.0.tar.xz", "a10f413252c857ab073fe3e75dbd56162073fb2fd9e9b6b1836fcc8a551fd775"),
    "win-x64": ("ffmpeg-n9.0.2-22-g46d8f462ee-win64-gpl-9.0.zip", "bf721765dbb181cd7a076bdd53c2665fa2c371dac07472b461fd7f357b1f2057"),
}
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--rid", choices=BUILDS, required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
output = args.output.resolve()
subprocess.run(["dotnet", "publish", str(root / "Voxen.csproj"), "-c", "Release", "-r", args.rid,
                "--self-contained", "true", "-o", str(output)], check=True)
filename, expected = BUILDS[args.rid]
cache = root / ".runtime" / "downloads"
cache.mkdir(parents=True, exist_ok=True)
archive = cache / filename
url = f"https://github.com/BtbN/FFmpeg-Builds/releases/download/{RELEASE}/{filename}"
if not archive.exists():
    print(f"Downloading pinned FFmpeg build for {args.rid}…", flush=True)
    temporary = archive.with_suffix(archive.suffix + ".partial")
    with urllib.request.urlopen(url, timeout=120) as response, temporary.open("wb") as target:
        shutil.copyfileobj(response, target)
    temporary.replace(archive)
with archive.open("rb") as source:
    actual = hashlib.file_digest(source, "sha256").hexdigest()
if actual != expected:
    raise RuntimeError(f"FFmpeg SHA-256 mismatch for {filename}; refusing to package it.")
target = output / "tools" / "ffmpeg"
target.mkdir(parents=True, exist_ok=True)
executable = "ffmpeg.exe" if args.rid == "win-x64" else "ffmpeg"
if archive.suffix == ".zip":
    with zipfile.ZipFile(archive) as bundle:
        name = next(name for name in bundle.namelist() if name.endswith("/bin/" + executable))
        with bundle.open(name) as source, (target / executable).open("wb") as dest:
            shutil.copyfileobj(source, dest)
        for name in bundle.namelist():
            if name.rsplit("/", 1)[-1].upper().startswith(("LICENSE", "COPYING")) and not name.endswith("/"):
                (target / name.rsplit("/", 1)[-1]).write_bytes(bundle.read(name))
else:
    with tarfile.open(archive, "r:xz") as bundle:
        member = next(member for member in bundle.getmembers() if member.name.endswith("/bin/" + executable))
        with bundle.extractfile(member) as source, (target / executable).open("wb") as dest:
            shutil.copyfileobj(source, dest)
        for member in bundle.getmembers():
            if member.isfile() and member.name.rsplit("/", 1)[-1].upper().startswith(("LICENSE", "COPYING")):
                (target / member.name.rsplit("/", 1)[-1]).write_bytes(bundle.extractfile(member).read())
    (target / executable).chmod(0o755)
(target / "SOURCE.md").write_text(f"FFmpeg GPL build from {url}\nSHA-256: {expected}\nBuild/source recipes: https://github.com/BtbN/FFmpeg-Builds/tree/{RELEASE}\nFFmpeg source: https://github.com/FFmpeg/FFmpeg/tree/release/9.0\n")
shutil.copy2(root / "README.md", output / "README.md")
shutil.copy2(root / "DESIGN.md", output / "DESIGN.md")
shutil.copytree(root / "docs", output / "docs", dirs_exist_ok=True)
print(f"Portable Voxen ready: {output}")
