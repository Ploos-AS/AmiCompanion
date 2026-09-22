#!/usr/bin/env bash
set -euo pipefail
PUBLISH_DIR="${1:?publish directory required}"
OUTPUT_DIR="${2:?output directory required}"
VERSION="${3:?version required}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
APP="$OUTPUT_DIR/AmiCompanion.app"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH_DIR"/. "$APP/Contents/MacOS/"
cp "$ROOT/packaging/macos/Info.plist" "$APP/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$APP/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion ${GITHUB_RUN_NUMBER:-1}" "$APP/Contents/Info.plist"
chmod +x "$APP/Contents/MacOS/AmiCompanion.Gui"
