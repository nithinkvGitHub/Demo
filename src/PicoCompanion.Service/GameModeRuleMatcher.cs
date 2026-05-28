using PicoCompanion.Core.Models;

namespace PicoCompanion.Service;

public sealed class GameModeRuleMatcher
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public (GameModeRule Rule, DetectedApplication Application)? Match(
        DongleConfiguration configuration,
        DetectedApplication? foregroundApplication,
        IReadOnlyList<DetectedApplication> runningApplications)
    {
        foreach (var rule in configuration.GameModeRules)
        {
            if (foregroundApplication is not null && Matches(rule, foregroundApplication))
            {
                return (rule, foregroundApplication);
            }

            if (rule.MatchForegroundOnly)
            {
                continue;
            }

            var runningMatch = runningApplications.FirstOrDefault(application => Matches(rule, application));
            if (runningMatch is not null)
            {
                return (rule, runningMatch);
            }
        }

        return null;
    }

    private static bool Matches(GameModeRule rule, DetectedApplication application) =>
        MatchesProcessName(rule, application)
            || MatchesPackageFamilyName(rule, application)
            || MatchesExecutablePath(rule, application);

    private static bool MatchesProcessName(GameModeRule rule, DetectedApplication application)
    {
        var processName = Path.GetFileNameWithoutExtension(application.ProcessName);
        return rule.ProcessNames.Any(candidate =>
            Comparer.Equals(Path.GetFileNameWithoutExtension(candidate), processName));
    }

    private static bool MatchesPackageFamilyName(GameModeRule rule, DetectedApplication application) =>
        !string.IsNullOrWhiteSpace(application.PackageFamilyName)
            && rule.PackageFamilyNames.Any(candidate => Comparer.Equals(candidate, application.PackageFamilyName));

    private static bool MatchesExecutablePath(GameModeRule rule, DetectedApplication application) =>
        !string.IsNullOrWhiteSpace(application.ExecutablePath)
            && rule.ExecutablePaths.Any(candidate =>
                Comparer.Equals(candidate, application.ExecutablePath)
                    || application.ExecutablePath.EndsWith(candidate, StringComparison.OrdinalIgnoreCase));
}
