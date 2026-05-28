using System.IO.Pipes;
using System.Text.Json;
using PicoCompanion.Core.Models;
using PicoCompanion.Core.Protocol;

namespace PicoCompanion.Ui.Services;

public sealed class CompanionServiceClient
{
    private readonly string _pipeName;

    public CompanionServiceClient(string pipeName = "PicoCompanion.Service")
    {
        _pipeName = pipeName;
    }

    public Task<DongleStatus> GetStatusAsync(CancellationToken cancellationToken) =>
        SendForPayloadAsync<DongleStatus>(CompanionRequest.Create("getStatus"), cancellationToken);

    public Task<GameModeStatus> GetGameModeStatusAsync(CancellationToken cancellationToken) =>
        SendForPayloadAsync<GameModeStatus>(CompanionRequest.Create("getGameModeStatus"), cancellationToken);

    public Task<DongleConfiguration> GetConfigurationAsync(CancellationToken cancellationToken) =>
        SendForPayloadAsync<DongleConfiguration>(CompanionRequest.Create("getConfig"), cancellationToken);

    public Task SaveConfigurationAsync(
        DongleConfiguration configuration,
        CancellationToken cancellationToken) =>
        SendAsync(CompanionRequest.Create("saveConfig", configuration), cancellationToken);

    public Task ApplyProfileAsync(ControllerProfile profile, CancellationToken cancellationToken) =>
        SendAsync(CompanionRequest.Create("applyProfile", profile), cancellationToken);

    public Task UpdateFirmwareAsync(string uf2Path, CancellationToken cancellationToken) =>
        SendAsync(CompanionRequest.Create("updateFirmware", new { uf2Path }), cancellationToken);

    public Task RebootAsync(CancellationToken cancellationToken) =>
        SendAsync(CompanionRequest.Create("reboot"), cancellationToken);

    private async Task<TPayload> SendForPayloadAsync<TPayload>(
        CompanionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.Payload is null)
        {
            throw new InvalidOperationException("Service response did not include a payload.");
        }

        return response.Payload.Value.Deserialize<TPayload>(JsonDefaults.Options)
            ?? throw new InvalidOperationException("Service response payload could not be read.");
    }

    private async Task<CompanionResponse> SendAsync(
        CompanionRequest request,
        CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await pipe.ConnectAsync(5000, cancellationToken).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(pipe, request, JsonDefaults.Options, cancellationToken)
            .ConfigureAwait(false);
        await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);

        var response = await JsonSerializer.DeserializeAsync<CompanionResponse>(
                pipe,
                JsonDefaults.Options,
                cancellationToken)
            .ConfigureAwait(false)
            ?? CompanionResponse.Fail("Service closed the pipe without a response.");

        if (!response.Success)
        {
            throw new InvalidOperationException(response.Message);
        }

        return response;
    }
}
