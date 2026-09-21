# AmiCompanion

Cross-platform companion toolkit for working with Commodore Amiga systems from Windows, Linux and macOS.

AmiCompanion is **not an emulator**. It helps inspect, prepare, transfer and manage Amiga software, disk images and physical media.

## M0

M0 establishes the portable project foundation:

- .NET 10
- Avalonia desktop shell for Windows, Linux and macOS
- `amic` command-line application
- shared `AmiCompanion.Core` library
- `AmiCompanion.Hardware` platform/hardware abstraction
- automated tests
- GitHub Actions on Windows, Linux and macOS
- architecture and roadmap documentation

No Kickstart ROMs, AmigaOS files or other proprietary Amiga software are included.

## Planned capabilities

ADF/HDF and OFS/FFS tooling, RDB/partition inspection, ROM and Hunk inspection, checksums, Amiga-oriented archive workflows, Gotek/FlashFloppy preparation, Greaseweazle integration, CF/SD preparation, and serial/network transfer.

## Build

Requires the .NET 10 SDK.

```sh
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run CLI:

```sh
dotnet run --project src/AmiCompanion.Cli -- --help
```

Run GUI:

```sh
dotnet run --project src/AmiCompanion.Gui
```

## License

MIT for software. See `LICENSE`.
