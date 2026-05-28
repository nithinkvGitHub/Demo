using System.Text.Json;

namespace PicoCompanion.Core.Protocol;

public sealed record CompanionRequest
{
    public string Command { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }

    public static CompanionRequest Create<TPayload>(string command, TPayload payload) =>
        new()
        {
            Command = command,
            Payload = JsonSerializer.SerializeToElement(payload, JsonDefaults.Options)
        };

    public static CompanionRequest Create(string command) => new() { Command = command };
}
