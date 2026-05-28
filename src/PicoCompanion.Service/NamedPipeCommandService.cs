using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using PicoCompanion.Core.Models;
using PicoCompanion.Core.Protocol;
using PicoCompanion.Core.Services;

namespace PicoCompanion.Service;

public sealed class NamedPipeCommandService : BackgroundService
{
    private readonly CompanionOptions _options;
    private readonly DongleRuntimeState _runtimeState;
    private readonly PicoDongleClient _dongleClient;
    private readonly Uf2FirmwareUpdater _firmwareUpdater;
    private readonly JsonFileStore<DongleConfiguration> _configurationStore;
    private readonly ILogger<NamedPipeCommandService> _logger;

    public NamedPipeCommandService(
        CompanionOptions options,
        DongleRuntimeState runtimeState,
        PicoDongleClient dongleClient,
        Uf2FirmwareUpdater firmwareUpdater,
        JsonFileStore<DongleConfiguration> configurationStore,
        ILogger<NamedPipeCommandService> logger)
    {
        _options = options;
        _runtimeState = runtimeState;
        _dongleClient = dongleClient;
        _firmwareUpdater = firmwareUpdater;
        _configurationStore = configurationStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pipe = CreatePipeServer();
            await pipe.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);
            _ = Task.Run(() => HandleClientAsync(pipe, stoppingToken), stoppingToken);
        }
    }

    private NamedPipeServerStream CreatePipeServer()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(
            new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));

        security.AddAccessRule(
            new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            _options.ControlPipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            inBufferSize: 16 * 1024,
            outBufferSize: 16 * 1024,
            security);
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<CompanionRequest>(
                    pipe,
                    JsonDefaults.Options,
                    cancellationToken)
                .ConfigureAwait(false);

            var response = request is null
                ? CompanionResponse.Fail("Missing request.")
                : await DispatchAsync(request, cancellationToken).ConfigureAwait(false);

            await JsonSerializer.SerializeAsync(pipe, response, JsonDefaults.Options, cancellationToken)
                .ConfigureAwait(false);
            await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Named pipe request failed.");
        }
        finally
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<CompanionResponse> DispatchAsync(
        CompanionRequest request,
        CancellationToken cancellationToken)
    {
        return request.Command switch
        {
            "getStatus" => CompanionResponse.Ok(_runtimeState.Status),
            "getConfig" => CompanionResponse.Ok(await _configurationStore.LoadAsync(cancellationToken)
                .ConfigureAwait(false)),
            "saveConfig" => await SaveConfigAsync(request, cancellationToken).ConfigureAwait(false),
            "applyProfile" => await ApplyProfileAsync(request, cancellationToken).ConfigureAwait(false),
            "updateFirmware" => await UpdateFirmwareAsync(request, cancellationToken).ConfigureAwait(false),
            "reboot" => await RebootAsync(cancellationToken).ConfigureAwait(false),
            _ => CompanionResponse.Fail($"Unknown command '{request.Command}'.")
        };
    }

    private async Task<CompanionResponse> SaveConfigAsync(
        CompanionRequest request,
        CancellationToken cancellationToken)
    {
        var configuration = DeserializePayload<DongleConfiguration>(request);
        await _configurationStore.SaveAsync(configuration, cancellationToken).ConfigureAwait(false);

        if (_runtimeState.Status.IsConnected)
        {
            await _runtimeState.UseTransportAsync(
                    transport => _dongleClient.WriteConfigurationAsync(transport, configuration, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return CompanionResponse.Ok("Configuration saved.");
    }

    private async Task<CompanionResponse> ApplyProfileAsync(
        CompanionRequest request,
        CancellationToken cancellationToken)
    {
        var profile = DeserializePayload<ControllerProfile>(request);
        await _runtimeState.UseTransportAsync(
                transport => _dongleClient.ApplyProfileAsync(transport, profile, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        var configuration = await _configurationStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var updated = configuration with { ActiveProfileId = profile.Id };
        await _configurationStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return CompanionResponse.Ok("Profile applied.");
    }

    private async Task<CompanionResponse> UpdateFirmwareAsync(
        CompanionRequest request,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<FirmwareUpdateRequest>(request);

        await _runtimeState.UseTransportAsync(
                transport => _dongleClient.EnterBootloaderAsync(transport, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        await _firmwareUpdater.UpdateAsync(payload.Uf2Path, cancellationToken).ConfigureAwait(false);
        return CompanionResponse.Ok("Firmware update copied. Waiting for dongle reconnect.");
    }

    private async Task<CompanionResponse> RebootAsync(CancellationToken cancellationToken)
    {
        await _runtimeState.UseTransportAsync(
                transport => _dongleClient.RebootAsync(transport, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        return CompanionResponse.Ok("Reboot command sent.");
    }

    private static T DeserializePayload<T>(CompanionRequest request)
    {
        if (request.Payload is null)
        {
            throw new InvalidOperationException("Request payload is required.");
        }

        return request.Payload.Value.Deserialize<T>(JsonDefaults.Options)
            ?? throw new InvalidOperationException("Request payload could not be read.");
    }

    private sealed record FirmwareUpdateRequest(string Uf2Path);
}
