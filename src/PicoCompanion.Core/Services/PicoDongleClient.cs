using System.Text.Json;
using PicoCompanion.Core.Models;
using PicoCompanion.Core.Protocol;

namespace PicoCompanion.Core.Services;

public sealed class PicoDongleClient
{
    private readonly SerialDongleTransportFactory _transportFactory;

    public PicoDongleClient(SerialDongleTransportFactory transportFactory)
    {
        _transportFactory = transportFactory;
    }

    public async Task<(DongleStatus Status, IDongleTransport? Transport)> ProbeAsync(
        CancellationToken cancellationToken)
    {
        foreach (var portName in _transportFactory.GetCandidatePorts())
        {
            IDongleTransport? transport = null;
            try
            {
                transport = _transportFactory.Open(portName);
                var status = await GetStatusAsync(transport, cancellationToken).ConfigureAwait(false);
                return (status, transport);
            }
            catch (Exception ex) when (
                ex is IOException
                    or UnauthorizedAccessException
                    or TimeoutException
                    or InvalidOperationException
                    or ArgumentException)
            {
                if (transport is not null)
                {
                    await transport.DisposeAsync().ConfigureAwait(false);
                }

                continue;
            }
        }

        return (DongleStatus.Disconnected(), null);
    }

    public async Task<DongleStatus> GetStatusAsync(
        IDongleTransport transport,
        CancellationToken cancellationToken)
    {
        var response = await transport
            .SendCommandAsync(PicoCommands.GetStatus, string.Empty, cancellationToken)
            .ConfigureAwait(false);

        var status = ParseStatus(response);
        if (status is null)
        {
            throw new InvalidOperationException("Port did not respond like a Pico companion dongle.");
        }

        return status with
        {
            IsConnected = true,
            PortName = transport.PortName,
            LastSeenUtc = DateTimeOffset.UtcNow
        };
    }

    public async Task<DongleConfiguration> ReadConfigurationAsync(
        IDongleTransport transport,
        CancellationToken cancellationToken)
    {
        var response = await transport
            .SendCommandAsync(PicoCommands.GetConfig, string.Empty, cancellationToken)
            .ConfigureAwait(false);

        var json = PicoResponse.DecodePayload(response);
        return JsonSerializer.Deserialize<DongleConfiguration>(json, JsonDefaults.Options)
            ?? new DongleConfiguration();
    }

    public Task WriteConfigurationAsync(
        IDongleTransport transport,
        DongleConfiguration configuration,
        CancellationToken cancellationToken) =>
        SendJsonAsync(transport, PicoCommands.SetConfig, configuration, cancellationToken);

    public Task ApplyProfileAsync(
        IDongleTransport transport,
        ControllerProfile profile,
        CancellationToken cancellationToken) =>
        SendJsonAsync(transport, PicoCommands.SetProfile, profile, cancellationToken);

    public Task SetOutputModeAsync(
        IDongleTransport transport,
        ControllerOutputMode outputMode,
        CancellationToken cancellationToken) =>
        SendJsonAsync(transport, PicoCommands.SetOutputMode, new { outputMode }, cancellationToken);

    public Task EnterBootloaderAsync(IDongleTransport transport, CancellationToken cancellationToken) =>
        transport.SendCommandAsync(PicoCommands.EnterBootloader, string.Empty, cancellationToken);

    public Task RebootAsync(IDongleTransport transport, CancellationToken cancellationToken) =>
        transport.SendCommandAsync(PicoCommands.Reboot, string.Empty, cancellationToken);

    private static async Task SendJsonAsync<TPayload>(
        IDongleTransport transport,
        string command,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonDefaults.Options);
        var response = await transport.SendCommandAsync(command, json, cancellationToken).ConfigureAwait(false);

        if (!PicoResponse.IsOk(response))
        {
            throw new InvalidOperationException($"Dongle rejected {command}: {response}");
        }
    }

    private static DongleStatus? ParseStatus(string response)
    {
        try
        {
            var json = PicoResponse.DecodePayload(response);
            if (string.IsNullOrWhiteSpace(json))
            {
                return PicoResponse.IsOk(response)
                    ? new DongleStatus { IsConnected = true, PairingState = "ready" }
                    : null;
            }

            return JsonSerializer.Deserialize<DongleStatus>(json, JsonDefaults.Options)
                ?? DongleStatus.Disconnected("empty status response");
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            return null;
        }
    }
}
