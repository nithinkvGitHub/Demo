# Controller compatibility notes

## Current repository status

This checkout does not include the firmware or Windows application code needed
to implement controller transport features. There is no Pico SDK project, USB
device stack, Bluetooth stack, DS5 parser, XInput bridge, virtual gamepad driver,
or GameInput client in this repository.

The notes below describe the implementation requirements for the requested
features and the API boundaries that matter for a future firmware/application
project.

## Raspberry Pi Pico 2 W support

Raspberry Pi Pico 2 W can be a reasonable target for a controller adapter if the
project is built around the Pico SDK and the `pico2_w` board target. A real
implementation needs:

- Pico SDK support for RP2350 and the Pico 2 W board definition.
- TinyUSB or another USB device stack for HID/gamepad presentation.
- CYW43/Bluetooth support if wireless controller pairing is required.
- Persistent storage for trusted devices, reconnect preferences, and user
  configuration.

This repository does not currently build for Pico 2 W, so the answer for this
checkout is "not yet".

## USB wake from sleep and hibernate

USB remote wake is firmware and host dependent. The device firmware must:

1. Advertise remote-wakeup support in the USB configuration descriptor.
2. Track whether the host enabled the remote-wakeup feature.
3. Enter a suspend-safe low-power mode while keeping the USB wake path armed.
4. Call the USB stack's remote-wakeup API only after a valid wake input.
5. Resume normal polling and reports after the host resumes the bus.

Important limits:

- Remote wake from normal sleep/suspend is realistic when the host, hub, port,
  BIOS/UEFI, and operating system allow the USB device to wake the machine.
- Wake from hibernate is not guaranteed. Many systems remove power from USB
  ports or do not arm USB wake sources for hibernate/S4.
- Deep dormant modes that fully stop the USB peripheral or required clocks are
  usually incompatible with staying enumerated as a USB wake device.

For a TinyUSB-based Pico implementation, this usually means wiring the suspend
callback, resume callback, and remote-wakeup call through a small power-state
manager rather than placing the board into the deepest sleep mode while USB is
active.

## Pairing, disconnect, and reconnect logic

Controller pairing should be handled as an explicit state machine instead of
single-shot connect calls. The key states are:

- `idle`: no active scan or connection attempt.
- `pairing`: user-requested discovery mode, with a clear timeout.
- `connecting`: connection attempt to a selected or previously trusted device.
- `connected`: normal input path is active.
- `disconnecting`: local disconnect or cleanup after link loss.
- `backoff`: retry delay after transient failures.

A robust reconnect implementation should:

- Persist bonded/trusted device identities.
- Prefer the most recently successful controller.
- Use bounded exponential backoff with jitter for repeated failures.
- Distinguish user-requested disconnects from link-loss events.
- Clear stale partial pairing state before starting a new pairing attempt.
- Debounce short USB/Bluetooth drops before tearing down the entire input path.
- Surface a visible status code or log event for pairing failures.

## Microsoft GameInput and DS5/Xbox Game Bar navigation

Microsoft GameInput is the right modern API for a Windows application that needs
to read controller input, including DS5/DualSense-style devices when supported by
the installed GameInput runtime. The current PC integration path is the
`Microsoft.GameInput` NuGet package plus the GameInput redistributable.

GameInput is an input-consumption API. It lets an application read gamepad,
HID/raw-device, motion, and related input data. It does not create a system-wide
virtual Xbox controller and does not inject controller navigation events into
Xbox Game Bar by itself.

Implications for a Windows Xbox/Game Bar mod:

- Use GameInput to read DS5 input inside your own Windows app or Game Bar
  widget.
- If Xbox Game Bar itself must be navigated by a DS5, Windows must see an input
  device that Game Bar accepts for navigation. That typically requires a
  supported HID gamepad path or a signed virtual controller/virtual bus driver
  that presents an Xbox/XInput-compatible device.
- A user-mode GameInput loop can translate DS5 input for your own process, but
  it cannot globally remap DS5 input into Game Bar navigation without an OS-level
  output/injection mechanism.

Recommended architecture for a full project:

1. Firmware presents a stable USB HID gamepad interface and supports remote wake.
2. Firmware or host app maintains Bluetooth pairing/reconnect state.
3. Windows helper app reads native DS5 data through GameInput when available.
4. If Game Bar navigation is required, the helper outputs through a supported
   virtual gamepad driver or a USB firmware mode that Windows/Game Bar recognizes.
