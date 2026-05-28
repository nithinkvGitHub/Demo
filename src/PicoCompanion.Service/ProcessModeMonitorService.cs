using PicoCompanion.Core.Models;
using PicoCompanion.Core.Services;

namespace PicoCompanion.Service;

public sealed class ProcessModeMonitorService : BackgroundService
{
    private readonly IForegroundApplicationDetector _applicationDetector;
    private readonly GameModeRuleMatcher _ruleMatcher;
    private readonly JsonFileStore<DongleConfiguration> _configurationStore;
    private readonly DongleRuntimeState _runtimeState;
    private readonly PicoDongleClient _dongleClient;
    private readonly ILogger<ProcessModeMonitorService> _logger;

    private ControllerOutputMode? _lastOutputMode;
    private string _lastProfileId = string.Empty;

    public ProcessModeMonitorService(
        IForegroundApplicationDetector applicationDetector,
        GameModeRuleMatcher ruleMatcher,
        JsonFileStore<DongleConfiguration> configurationStore,
        DongleRuntimeState runtimeState,
        PicoDongleClient dongleClient,
        ILogger<ProcessModeMonitorService> logger)
    {
        _applicationDetector = applicationDetector;
        _ruleMatcher = ruleMatcher;
        _configurationStore = configurationStore;
        _runtimeState = runtimeState;
        _dongleClient = dongleClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automatic game mode evaluation failed.");
                await _runtimeState.SetGameModeStatusAsync(
                        _runtimeState.GameModeStatus with
                        {
                            LastEvaluatedUtc = DateTimeOffset.UtcNow,
                            LastError = ex.Message
                        },
                        stoppingToken)
                    .ConfigureAwait(false);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        var configuration = await _configurationStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var foregroundApplication = _applicationDetector.GetForegroundApplication();
        var runningApplications = configuration.GameModeRules.Any(rule => !rule.MatchForegroundOnly)
            ? _applicationDetector.GetRunningApplications()
            : Array.Empty<DetectedApplication>();

        var match = configuration.EnableAutomaticModeSwitching
            ? _ruleMatcher.Match(configuration, foregroundApplication, runningApplications)
            : null;

        var desiredMode = match is null ? configuration.DefaultOutputMode : match.Value.Rule.OutputMode;
        var desiredProfileId = match is null ? configuration.ActiveProfileId : match.Value.Rule.ProfileId;
        var desiredProfile = configuration.Profiles.FirstOrDefault(profile => profile.Id == desiredProfileId)
            ?? configuration.Profiles.FirstOrDefault(profile => profile.Id == configuration.ActiveProfileId);

        if (_runtimeState.Status.IsConnected
            && (_lastOutputMode != desiredMode || !string.Equals(_lastProfileId, desiredProfileId, StringComparison.Ordinal)))
        {
            await _runtimeState.UseTransportAsync(
                    async transport =>
                    {
                        await _dongleClient.SetOutputModeAsync(transport, desiredMode, cancellationToken)
                            .ConfigureAwait(false);

                        if (desiredProfile is not null)
                        {
                            await _dongleClient.ApplyProfileAsync(transport, desiredProfile, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            _lastOutputMode = desiredMode;
            _lastProfileId = desiredProfileId;
        }

        var status = new GameModeStatus
        {
            CurrentOutputMode = desiredMode,
            ActiveRuleId = match is null ? string.Empty : match.Value.Rule.Id,
            ActiveRuleName = match is null ? "Default" : match.Value.Rule.DisplayName,
            ActiveProcessName = match is null
                ? foregroundApplication?.ProcessName ?? string.Empty
                : match.Value.Application.ProcessName,
            ActivePackageFamilyName = match is null
                ? foregroundApplication?.PackageFamilyName ?? string.Empty
                : match.Value.Application.PackageFamilyName,
            ActiveProfileId = desiredProfileId,
            LastEvaluatedUtc = DateTimeOffset.UtcNow
        };

        await _runtimeState.SetGameModeStatusAsync(status, cancellationToken).ConfigureAwait(false);
    }
}
