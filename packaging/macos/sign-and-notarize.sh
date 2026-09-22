#!/usr/bin/env bash
set -euo pipefail
APP="${1:?app bundle required}"
DMG="${2:?dmg required}"

if [[ -z "${APPLE_SIGNING_IDENTITY:-}" ]]; then
  echo "APPLE_SIGNING_IDENTITY is not configured; leaving macOS artifacts unsigned."
  exit 0
fi

codesign --force --deep --options runtime --timestamp --sign "$APPLE_SIGNING_IDENTITY" "$APP"
codesign --verify --deep --strict --verbose=2 "$APP"

if [[ -z "${APPLE_ID:-}" || -z "${APPLE_TEAM_ID:-}" || -z "${APPLE_APP_PASSWORD:-}" ]]; then
  echo "Notarization credentials are incomplete; app is signed but notarization is skipped."
  exit 0
fi

xcrun notarytool submit "$DMG" --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --password "$APPLE_APP_PASSWORD" --wait
xcrun stapler staple "$DMG"
xcrun stapler validate "$DMG"
