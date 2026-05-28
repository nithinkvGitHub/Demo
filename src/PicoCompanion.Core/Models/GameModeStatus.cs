namespace PicoCompanion.Core.Models;

public sealed record GameModeStatus
{
    public ControllerOutputMode CurrentOutputMode { get; init; } = ControllerOutputMode.NativeDualSenseHid;

    public string ActiveRuleId { get; init; } = string.Empty;

    public string ActiveRuleName { get; init; } = string.Empty;

    public string ActiveProcessName { get; init; } = string.Empty;

    public string ActivePackageFamilyName { get; init; } = string.Empty;

    public string ActiveProfileId { get; init; } = "default";

    public DateTimeOffset LastEvaluatedUtc { get; init; }

    public string LastError { get; init; } = string.Empty;
}
