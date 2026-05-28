using PicoCompanion.Core.Models;
using PicoCompanion.Core.Services;

namespace PicoCompanion.Service;

public sealed class DongleRuntimeState : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IDongleTransport? _transport;
    private DongleStatus _status = DongleStatus.Disconnected();
    private GameModeStatus _gameModeStatus = new();

    public DongleStatus Status => _status;

    public GameModeStatus GameModeStatus => _gameModeStatus;

    public async Task SetConnectionAsync(
        DongleStatus status,
        IDongleTransport? transport,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!ReferenceEquals(_transport, transport) && (transport is not null || !status.IsConnected))
            {
                if (_transport is not null)
                {
                    await _transport.DisposeAsync().ConfigureAwait(false);
                }

                _transport = transport;
            }

            _status = status;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TResult> UseTransportAsync<TResult>(
        Func<IDongleTransport, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_transport is null)
            {
                throw new InvalidOperationException("Pico dongle is not connected.");
            }

            return await action(_transport).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UseTransportAsync(
        Func<IDongleTransport, Task> action,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_transport is null)
            {
                throw new InvalidOperationException("Pico dongle is not connected.");
            }

            await action(_transport).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetGameModeStatusAsync(
        GameModeStatus status,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _gameModeStatus = status;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transport is not null)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }
}
