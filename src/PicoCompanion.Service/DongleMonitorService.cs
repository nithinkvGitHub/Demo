using PicoCompanion.Core.Models;
using PicoCompanion.Core.Services;

namespace PicoCompanion.Service;

public sealed class DongleMonitorService : BackgroundService
{
    private readonly PicoDongleClient _dongleClient;
    private readonly DongleRuntimeState _runtimeState;
    private readonly ILogger<DongleMonitorService> _logger;

    public DongleMonitorService(
        PicoDongleClient dongleClient,
        DongleRuntimeState runtimeState,
        ILogger<DongleMonitorService> logger)
    {
        _dongleClient = dongleClient;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = _runtimeState.Status.IsConnected
                    ? await _runtimeState.UseTransportAsync(
                            transport => _dongleClient.GetStatusAsync(transport, stoppingToken),
                            stoppingToken)
                        .ConfigureAwait(false)
                    : null;

                if (status is null)
                {
                    IDongleTransport? transport;
                    (status, transport) = await _dongleClient.ProbeAsync(stoppingToken).ConfigureAwait(false);
                    await _runtimeState.SetConnectionAsync(status, transport, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    await _runtimeState.SetConnectionAsync(status, null, stoppingToken).ConfigureAwait(false);
                }

                if (status.IsConnected)
                {
                    _logger.LogInformation(
                        "Pico dongle connected on {PortName}, firmware {FirmwareVersion}.",
                        status.PortName,
                        status.FirmwareVersion);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dongle monitor pass failed.");
                await _runtimeState
                    .SetConnectionAsync(DongleStatus.Disconnected(ex.Message), null, stoppingToken)
                    .ConfigureAwait(false);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }
    }
}
