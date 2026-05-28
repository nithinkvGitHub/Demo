# Pico control protocol draft

This draft defines the management channel used by the Windows companion service.
It is separate from the USB HID/gamepad interface used by the host for gameplay.

## Transport

Initial transport: USB CDC serial at `115200` baud.

Each request is one line:

```text
COMMAND
COMMAND base64-encoded-json-payload
```

Each response is one line:

```text
OK
OK base64-encoded-json-payload
ERR human readable message
{json status object}
```

The current service accepts a raw JSON object for `GET_STATUS` and `GET_CONFIG`
responses to keep firmware bring-up simple. Later firmware should standardize
on `OK <base64-json>` for every command that returns structured data.

## Commands

### `GET_STATUS`

Returns dongle state:

```json
{
  "isConnected": true,
  "portName": "COM5",
  "firmwareVersion": "0.1.0",
  "pairingState": "connected",
  "activeProfileId": "default",
  "lastSeenUtc": "2026-05-28T00:00:00Z",
  "lastError": ""
}
```

### `GET_CONFIG`

Returns the persisted firmware configuration.

### `SET_CONFIG <payload>`

Payload is a `DongleConfiguration` JSON object. Firmware should validate the
payload, persist it safely, and reply `OK` only after it is durable.

### `SET_PROFILE <payload>`

Payload is a single `ControllerProfile`. Firmware should apply runtime-only
settings immediately and persist the selected profile if required.

### `SET_OUTPUT_MODE <payload>`

Payload selects the controller presentation mode:

```json
{
  "outputMode": "NativeDualSenseHid"
}
```

Supported values in the Windows companion model:

- `NativeDualSenseHid` - firmware presents as a DualSense-compatible USB HID
  device for games with native DS5 support.
- `DInputHid` - firmware presents as a generic DirectInput/HID gamepad.
- `XInputVirtual` - host-side virtual Xbox/XInput output is expected. Firmware
  should either switch to a control-only/raw-input role or to a descriptor that
  the host-side mapper can hide from games to avoid double input.
- `Disabled` - no gameplay reports should be exposed.

The Pico alone should not pretend to be a licensed Xbox controller unless the
project has a legally supported firmware/device path. For normal Windows XInput
compatibility, the companion stack should use a signed virtual controller driver
or supported virtual gamepad framework on the PC.

### `ENTER_BOOTLOADER`

Firmware disconnects from the normal USB interface and enters the RP2350 UF2
bootloader. In Pico SDK firmware this is typically:

```c
reset_usb_boot(0, 0);
```

### `REBOOT`

Firmware performs a normal reboot back into the dongle application.

## Required firmware behavior

- Keep the HID/gamepad USB interface independent from this control channel.
- Reject malformed JSON without changing active settings.
- Avoid flash writes while time-critical audio/haptics work is running, or gate
  writes through an existing multicore flash lockout.
- Store enough config to reconnect the trusted DS5 controller without Windows
  after reboot.
- Preserve USB remote-wake behavior in firmware; the Windows service cannot
  wake the host from sleep on behalf of a sleeping USB device.
