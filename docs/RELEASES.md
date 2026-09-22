# AmiCompanion releases

## Primary distribution: itch.io

AmiCompanion uses itch.io as the primary end-user release channel. Tagged releases should build all supported desktop targets and publish them directly to separate itch.io channels with **butler**.

Planned channels:

- `windows-x64`
- `linux-x64`
- `linux-arm64`
- `macos-x64`
- `macos-arm64`

Release artifacts must be produced from the same Git tag and version. CI must build and test before deployment. Publishing requires an itch.io project created outside CI and a repository secret containing the butler API key. The exact itch.io account/project slug is intentionally not hard-coded until configured.

GitHub releases may remain useful for source archives, checksums, SBOM/provenance and developer-facing artifacts, but itch.io is the canonical binary distribution channel.

## Aminet

Aminet is a secondary release target where it makes sense for Amiga-side components, companion agents, documentation or Amiga-native utilities. The Windows/Linux/macOS desktop application itself should not be treated as an Aminet-native package.

Any Aminet submission must be generated from the same tagged source release, with an Aminet-compatible archive/readme and without copyrighted AmigaOS/Kickstart material.

## Release gates

1. Tag/version consistency.
2. Windows, Linux and macOS build/test qualification.
3. Package each supported architecture.
4. Generate checksums and release metadata.
5. Validate itch.io build directories with butler.
6. Publish each platform/architecture to its own itch.io channel.
7. Optionally prepare eligible Amiga-side Aminet package(s).
