#!/usr/bin/env bash
set -euo pipefail
APP="${1:?app bundle required}"
OUTPUT="${2:?output dmg required}"
VOLUME_NAME="${3:-AmiCompanion}"
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
cp -R "$APP" "$STAGE/AmiCompanion.app"
ln -s /Applications "$STAGE/Applications"
rm -f "$OUTPUT"
hdiutil create -volname "$VOLUME_NAME" -srcfolder "$STAGE" -ov -format UDZO "$OUTPUT"
