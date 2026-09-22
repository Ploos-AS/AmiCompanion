# Packaging

AmiCompanion release payloads are built self-contained for each supported runtime.

## itch.io payloads

Butler should receive an unpacked platform directory rather than a manually compressed archive. This lets itch.io perform its own differential uploads and installation handling.

Channels:

| Runtime | itch.io channel | Payload |
| --- | --- | --- |
| win-x64 | windows-x64 | self-contained GUI + CLI |
| linux-x64 | linux-x64 | self-contained GUI + CLI |
| linux-arm64 | linux-arm64 | self-contained GUI + CLI |
| osx-x64 | macos-x64 | self-contained GUI + CLI |
| osx-arm64 | macos-arm64 | self-contained GUI + CLI |

Every payload includes `RELEASE.txt` and `SHA256SUMS`.

## Native packaging roadmap

The unpacked butler payload is canonical for itch.io. Native convenience packages are additional outputs:

- Windows: portable ZIP first; installer/MSIX later.
- Linux: tar.gz first; AppImage and/or Flatpak later.
- macOS: proper `AmiCompanion.app` bundle, then DMG; signing/notarization when release credentials are available.

Native packaging must not change the application contents independently of the tagged release. Packages must be generated from the same qualified build.

## Aminet

Aminet packaging is separate. Only Amiga-native companion components or documentation should be prepared as Aminet archives; desktop binaries are not repackaged as Aminet software.
