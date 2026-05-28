# Windows companion service and UI

The companion app is the host-side management layer for the Pico 2 W DS5 dongle.
It is intentionally not the real-time controller bridge. The Pico firmware
should continue to own USB enumeration, USB remote wake, suspend/resume, Bluetooth
pairing, reconnect, and low-latency input forwarding so the dongle still behaves
like hardware when Windows is sleeping or booting.

## Components

- `PicoCompanion.Core` contains shared models, JSON protocol helpers, serial
  transport, config persistence, and UF2 bootloader copy logic.
- `PicoCompanion.Service` runs as a Windows Service at startup. It probes the
  Pico control channel, stores configuration in
  `%ProgramData%\PicoCompanion\dongle-config.json`, applies profiles, sends
  reboot/update commands, and copies UF2 firmware to `RPI-RP2`.
- `PicoCompanion.Ui` is a WPF app for normal users. It talks to the service over
  the local named pipe `PicoCompanion.Service`.

## Build

Install the current .NET SDK for Windows, then run:

```powershell
dotnet restore .\PicoCompanion.sln
dotnet build .\PicoCompanion.sln -c Release
```

## Install service

Run PowerShell as Administrator:

```powershell
.\scripts\install-service.ps1
```

The service is installed as `PicoCompanionService`, starts automatically, and
runs `PicoCompanion.Service.exe`. The WPF UI is published beside it under
`%ProgramFiles%\PicoCompanion\Ui\PicoCompanion.Ui.exe`.

To remove it:

```powershell
.\scripts\uninstall-service.ps1
```

## Configure the dongle

1. Start the `PicoCompanion.Ui` WPF application.
2. Confirm the status line shows the Pico control port and firmware version.
3. Change configuration or apply a profile.
4. Click **Save Configuration**.

The service persists the config first, then sends it to the Pico when connected.
If the dongle is not connected, the saved config remains available and can be
applied on the next connection.

## Update firmware

The firmware must implement the control command that enters the RP2350 USB
bootloader, equivalent to calling `reset_usb_boot(0, 0)` from Pico SDK firmware.

1. Open the UI's **Firmware** tab.
2. Select the `.uf2` file built for the correct board.
3. Click **Update Firmware**.
4. The service sends `ENTER_BOOTLOADER`, waits for the `RPI-RP2` mass-storage
   drive, copies the UF2, and waits for the bootloader drive to disappear.

If firmware is damaged badly enough that it cannot receive control commands,
hold BOOTSEL while plugging in the Pico and copy the UF2 manually.

## Firmware boundary

The service assumes the Pico exposes a lightweight serial control channel. That
channel can be USB CDC today and can move to vendor HID later without changing
the UI/service boundary. Real controller reports should not depend on the
Windows service, because USB wake and pre-login behavior need real firmware.
