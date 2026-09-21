# Architecture

AmiCompanion is cross-platform by design. Windows, Linux and macOS are first-class hosts.

## Layers

- **AmiCompanion.Core** contains portable Amiga-domain logic and must not depend on UI or host-specific APIs.
- **AmiCompanion.Hardware** defines host/hardware abstractions for block devices, serial links and future Greaseweazle support.
- **AmiCompanion.Cli** provides the `amic` command line interface.
- **AmiCompanion.Gui** provides the Avalonia desktop application.

Platform-specific code belongs behind interfaces. Core functionality must remain usable from both GUI and CLI.

## Emulator policy

AmiCompanion is not an emulator and must not require one for normal operation. Ploos-AS `amiga-runtime` may later be used for automated integration/qualification testing.

## Proprietary software

Kickstart ROMs, AmigaOS files and other proprietary software must never be committed to this repository.
