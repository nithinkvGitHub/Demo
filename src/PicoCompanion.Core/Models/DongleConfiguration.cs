namespace PicoCompanion.Core.Models;

public sealed record DongleConfiguration
{
    public string DeviceName { get; init; } = "Pico DS5 Dongle";

    public bool AutoReconnect { get; init; } = true;

    public bool EnableUsbRemoteWake { get; init; } = true;

    public string PreferredControllerAddress { get; init; } = string.Empty;

    public string ActiveProfileId { get; init; } = "default";

    public IReadOnlyList<ControllerProfile> Profiles { get; init; } =
    [
        new ControllerProfile
        {
            Id = "default",
            Name = "Default",
            LowLatencyMode = true,
            EnableAudio = false,
            EnableHaptics = true,
            LedBrightnessPercent = 50,
            KeepAliveIntervalMs = 1000
        }
    ];
}
