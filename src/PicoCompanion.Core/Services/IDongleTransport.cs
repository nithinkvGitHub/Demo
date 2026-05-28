namespace PicoCompanion.Core.Services;

public interface IDongleTransport : IAsyncDisposable
{
    string PortName { get; }

    Task<string> SendCommandAsync(string command, string payload, CancellationToken cancellationToken);
}
