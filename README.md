# Pico DS5 Dongle Companion

This repository now contains the first Windows-side companion scaffold for a
Pico 2 W / RP2350 DS5 wireless dongle project.

The design keeps the real controller bridge in firmware so USB enumeration,
remote wake, suspend/resume, Bluetooth pairing, and reconnect can work while
Windows is sleeping or booting. The Windows companion provides the management
surface:

- Windows Service that starts with the machine.
- WPF UI for configuring the dongle.
- Profile storage and profile apply commands.
- UF2 firmware update flow that reboots the Pico into `RPI-RP2` bootloader mode.
- Local named-pipe API between UI and service.

## Projects

- `src/PicoCompanion.Core` - shared models, serial transport, config store, UF2
  updater, and protocol helpers.
- `src/PicoCompanion.Service` - Windows Service host for dongle monitoring and
  management commands.
- `src/PicoCompanion.Ui` - WPF configuration and firmware update UI.
- `docs/windows-companion.md` - build, install, configuration, and update flow.
- `docs/pico-control-protocol.md` - firmware control protocol draft.

## Build on Windows

Install the current .NET SDK for Windows, then run:

```powershell
dotnet restore .\PicoCompanion.sln
dotnet build .\PicoCompanion.sln -c Release
```

Install the service from an elevated PowerShell session:

```powershell
.\scripts\install-service.ps1
```

See [docs/windows-companion.md](docs/windows-companion.md) for operational
details.