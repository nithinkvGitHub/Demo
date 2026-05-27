# Waveshare RP2350B-Plus-W build notes

This project is based on [awalol/DS5Dongle](https://github.com/awalol/DS5Dongle). The default build target remains Raspberry Pi Pico 2 W, and this fork adds an opt-in target for the Waveshare RP2350B-Plus-W.

## What changed

- Added a `WAVESHARE_RP2350B_PLUS_W` CMake option.
- Added a local Pico SDK board definition for `waveshare_rp2350b_plus_w`.
- Configured the Waveshare CYW43439/RM2 Bluetooth pins before `cyw43_arch_init()`.
- Set the Waveshare flash size to 16 MB so config storage is placed at the end of the actual flash.
- Made the RP2350 overclock and vreg enum build-time settings:
  - `DS5_SYS_CLOCK_KHZ`, default `320000`
  - `DS5_VREG_VOLTAGE`, default `VREG_VOLTAGE_1_20`
- Reduced stale Bluetooth output latency by making the HID send FIFO shallower by default and dropping the oldest queued output report when the queue is full.
- Cached the speaker gain calculation so the hot audio loop does not recompute `powf()` on every USB audio read.
- Added a multicore flash-write lockout path around config saves while the Opus encoder is running on core 1.

## Building

Initialize dependencies from the repository root:

```sh
git submodule update --init --recursive
```

Build for Raspberry Pi Pico 2 W:

```sh
cd ds5dongle
cmake -S . -B build-pico2w
cmake --build build-pico2w
```

Build for Waveshare RP2350B-Plus-W:

```sh
cd ds5dongle
cmake -S . -B build-waveshare -DWAVESHARE_RP2350B_PLUS_W=ON
cmake --build build-waveshare
```

If a board is unstable at the default overclock, lower the clock without editing code:

```sh
cmake -S . -B build-waveshare \
  -DWAVESHARE_RP2350B_PLUS_W=ON \
  -DDS5_SYS_CLOCK_KHZ=300000
cmake --build build-waveshare
```

You can also select a different Pico SDK vreg enum if your hardware needs it:

```sh
cmake -S . -B build-waveshare \
  -DWAVESHARE_RP2350B_PLUS_W=ON \
  -DDS5_VREG_VOLTAGE=VREG_VOLTAGE_1_25
cmake --build build-waveshare
```

The output UF2 is produced by `pico_add_extra_outputs()` in the selected build directory.

## Performance expectations

The Waveshare RP2350B-Plus-W uses the same RP2350 class CPU, 520 KB SRAM, USB full-speed device path, and CYW43439 Bluetooth chip class as Pico 2 W. This port is therefore not expected to produce a large raw performance gain.

The included performance work focuses on avoiding avoidable latency and instability inside the existing design:

- stale Bluetooth output packets are discarded under congestion instead of growing latency;
- repeated speaker gain math is avoided in the USB audio loop;
- flash config writes coordinate with the second core used by Opus encoding;
- clock/voltage tuning no longer requires source edits.

Remaining audio stutter or latency can still come from Bluetooth airtime, USB scheduling, overclock margin, and the Opus/haptics encode workload.

## Flashing

Put the target board into BOOTSEL/UF2 mode and copy the generated `.uf2` file to the mass-storage drive. Use the Waveshare build for RP2350B-Plus-W; the Pico 2 W UF2 should not be assumed to work because the Waveshare radio pins are different.
