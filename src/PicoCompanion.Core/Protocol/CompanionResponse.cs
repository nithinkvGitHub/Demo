using System.Text.Json;

namespace PicoCompanion.Core.Protocol;

public sealed record CompanionResponse
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }

    public static CompanionResponse Ok<TPayload>(TPayload payload, string message = "") =>
        new()
        {
            Success = true,
            Message = message,
            Payload = JsonSerializer.SerializeToElement(payload, JsonDefaults.Options)
        };

    public static CompanionResponse Ok(string message = "") => new() { Success = true, Message = message };

    public static CompanionResponse Fail(string message) => new() { Success = false, Message = message };
}
