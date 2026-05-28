namespace PicoCompanion.Core.Models;

public sealed record DongleConfiguration
{
    public string DeviceName { get; init; } = "Pico DS5 Dongle";

    public bool AutoReconnect { get; init; } = true;

    public bool EnableUsbRemoteWake { get; init; } = true;

    public string PreferredControllerAddress { get; init; } = string.Empty;

    public string ActiveProfileId { get; init; } = "default";

    public bool EnableAutomaticModeSwitching { get; init; } = true;

    public ControllerOutputMode DefaultOutputMode { get; init; } = ControllerOutputMode.NativeDualSenseHid;

    public IReadOnlyList<ControllerProfile> Profiles { get; init; } =
    [
        new ControllerProfile
        {
            Id = "default",
            Name = "Default",
            PreferredOutputMode = ControllerOutputMode.NativeDualSenseHid,
            LowLatencyMode = true,
            EnableAudio = false,
            EnableHaptics = true,
            LedBrightnessPercent = 50,
            KeepAliveIntervalMs = 1000
        },
        new ControllerProfile
        {
            Id = "xinput",
            Name = "Xbox/XInput Compatibility",
            PreferredOutputMode = ControllerOutputMode.XInputVirtual,
            LowLatencyMode = true,
            EnableAudio = false,
            EnableHaptics = false,
            LedBrightnessPercent = 25,
            KeepAliveIntervalMs = 1000,
            Notes = "Use for Xbox app, Xbox Game Bar navigation, and games that only support XInput."
        },
        new ControllerProfile
        {
            Id = "native-ds5",
            Name = "Native DualSense",
            PreferredOutputMode = ControllerOutputMode.NativeDualSenseHid,
            LowLatencyMode = true,
            EnableAudio = true,
            EnableHaptics = true,
            LedBrightnessPercent = 75,
            KeepAliveIntervalMs = 1000,
            Notes = "Use for PC games with native DualSense or DirectInput/HID support."
        }
    ];

    public IReadOnlyList<GameModeRule> GameModeRules { get; init; } =
    [
        new GameModeRule
        {
            Id = "xbox-app-navigation",
            DisplayName = "Xbox app and Game Bar",
            ProcessNames = ["XboxPcApp", "GameBar", "GameBarFTServer"],
            OutputMode = ControllerOutputMode.XInputVirtual,
            ProfileId = "xinput",
            MatchForegroundOnly = false,
            Notes = "Navigation generally expects an Xbox/XInput-compatible controller."
        }
    ];
}
