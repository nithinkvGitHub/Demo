namespace PicoCompanion.Core.Models;

public sealed record GameModeRule
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; init; } = "Game";

    public IReadOnlyList<string> ProcessNames { get; init; } = [];

    public IReadOnlyList<string> PackageFamilyNames { get; init; } = [];

    public IReadOnlyList<string> ExecutablePaths { get; init; } = [];

    public ControllerOutputMode OutputMode { get; init; } = ControllerOutputMode.XInputVirtual;

    public string ProfileId { get; init; } = "default";

    public bool MatchForegroundOnly { get; init; } = true;

    public string Notes { get; init; } = string.Empty;
}
