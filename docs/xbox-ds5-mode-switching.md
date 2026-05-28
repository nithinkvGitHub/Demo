# Xbox and DualSense mode switching

The companion can support one-controller switching between Xbox-style navigation
and native DualSense gameplay, but it needs clear boundaries.

## What is possible

- Detect the foreground Windows process.
- Detect many packaged Xbox/Microsoft Store games by process and package family
  name when Windows allows the service to inspect the process.
- Maintain a compatibility list that maps games to:
  - `NativeDualSenseHid` for games with native DS5/DualSense support.
  - `DInputHid` for generic DirectInput/HID games.
  - `XInputVirtual` for Xbox app, Game Bar navigation, and games that only work
    well with Xbox/XInput controllers.
- Ask the Pico firmware to switch USB/controller presentation mode.
- Apply a matching controller profile at the same time.

## What is not guaranteed

Windows does not expose a stable public API that says "this Xbox library game is
running and expects XInput" for every installed game. Xbox app, Microsoft Store,
Steam, Epic, and standalone games all launch differently.

The reliable approach is a compatibility database:

- process names, such as `GameBar`, `XboxPcApp`, or a game executable;
- package family names for packaged games;
- optional executable paths for non-packaged games;
- desired output mode and profile.

The UI exposes these rules so we can add known games over time.

## XInput requirement

Game Bar navigation and many Xbox/PC games expect an Xbox/XInput-compatible
controller. A normal user-mode app cannot create a real system-wide XInput
controller by itself.

For `XInputVirtual` mode, the project needs one of these:

1. A signed virtual controller driver or supported virtual gamepad framework on
   Windows.
2. A legally supported firmware/device path that Windows accepts as an Xbox-like
   controller.

Without that layer, the service can choose the mode and tell the Pico, but games
will still only see whatever USB HID device the Pico exposes.

## Avoiding double input

If the DS5 HID device and virtual XInput device are both visible to a game, many
games will receive duplicate input. The final implementation should ensure only
one gameplay path is visible:

- Native DS5 game: expose the Pico as DualSense/DInput HID and disable virtual
  XInput output.
- Xbox/XInput game: enable virtual XInput output and hide or suppress the Pico's
  gameplay HID reports from games.
- Xbox app/Game Bar navigation: prefer `XInputVirtual`.

## Current implementation scaffold

The service now includes `ProcessModeMonitorService`, which periodically:

1. Loads `%ProgramData%\PicoCompanion\dongle-config.json`.
2. Reads the foreground app and, for non-foreground rules, running apps.
3. Matches `GameModeRules`.
4. Selects the configured output mode/profile.
5. Sends `SET_OUTPUT_MODE` and `SET_PROFILE` to the Pico when connected.
6. Exposes the active rule/mode to the UI through the named pipe API.

The default rule treats Xbox app and Game Bar processes as `XInputVirtual`.
Native DualSense game rules can be added by process/package name as we identify
supported games.
