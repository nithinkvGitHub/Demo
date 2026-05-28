namespace PicoCompanion.Core.Models;

public sealed record DongleStatus
{
    public bool IsConnected { get; init; }

    public string PortName { get; init; } = string.Empty;

    public string FirmwareVersion { get; init; } = "unknown";

    public string PairingState { get; init; } = "unknown";

    public string ActiveProfileId { get; init; } = "default";

    public DateTimeOffset? LastSeenUtc { get; init; }

    public string LastError { get; init; } = string.Empty;

    public static DongleStatus Disconnected(string error = "") =>
        new()
        {
            IsConnected = false,
            PairingState = "disconnected",
            LastError = error
        };
}
