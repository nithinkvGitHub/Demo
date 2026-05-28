namespace PicoCompanion.Core.Models;

public sealed record DetectedApplication
{
    public string ProcessName { get; init; } = string.Empty;

    public string ExecutablePath { get; init; } = string.Empty;

    public string PackageFamilyName { get; init; } = string.Empty;

    public string WindowTitle { get; init; } = string.Empty;

    public int ProcessId { get; init; }
}
