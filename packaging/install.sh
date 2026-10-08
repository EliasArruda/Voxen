#!/usr/bin/env bash
set -euo pipefail
source_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
install_dir="${XDG_DATA_HOME:-$HOME/.local/share}/voxen"
applications="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
if [[ "$source_dir" == "$install_dir" ]]; then
  echo 'Voxen is already installed here.'
  exit 0
fi
mkdir -p "$install_dir" "$applications"
cp -a "$source_dir/." "$install_dir/"
chmod +x "$install_dir/Voxen" "$install_dir/tools/ffmpeg/ffmpeg"
# Desktop entry values escape backslashes, quotes, backticks and dollar signs.
exec_path="${install_dir//\\/\\\\}"
exec_path="${exec_path//\"/\\\"}"
exec_path="${exec_path//\$/\\\$}"
exec_path="${exec_path//\`/\\\`}"
exec_path="${exec_path//%/%%}"
cat > "$applications/voxen.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=Voxen
Comment=Discover music and build your library
Exec="$exec_path/Voxen"
Icon=$install_dir/Assets/voxen.png
Terminal=false
Categories=AudioVideo;Audio;Player;
DESKTOP
command -v update-desktop-database >/dev/null && update-desktop-database "$applications" || true
printf 'Installed Voxen. Open it from your application menu.\nLinux requires WebKitGTK 4.1 (Arch: webkit2gtk-4.1; Ubuntu: libwebkit2gtk-4.1-0).\n'
