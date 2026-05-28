namespace PicoCompanion.Core.Models;

public sealed record ControllerProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string Name { get; init; } = "Default";

    public bool LowLatencyMode { get; init; } = true;

    public bool EnableAudio { get; init; }

    public bool EnableHaptics { get; init; } = true;

    public int LedBrightnessPercent { get; init; } = 50;

    public int KeepAliveIntervalMs { get; init; } = 1000;

    public string Notes { get; init; } = string.Empty;
}
