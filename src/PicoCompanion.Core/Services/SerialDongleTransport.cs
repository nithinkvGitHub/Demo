using System.IO.Ports;
using System.Text;

namespace PicoCompanion.Core.Services;

public sealed class SerialDongleTransport : IDongleTransport
{
    private readonly SerialPort _serialPort;
    private readonly Encoding _encoding = new UTF8Encoding(false);

    public SerialDongleTransport(string portName, int baudRate)
    {
        PortName = portName;
        _serialPort = new SerialPort(portName, baudRate)
        {
            Encoding = _encoding,
            NewLine = "\n",
            ReadTimeout = 3000,
            WriteTimeout = 3000,
            DtrEnable = true,
            RtsEnable = true
        };
        _serialPort.Open();
    }

    public string PortName { get; }

    public async Task<string> SendCommandAsync(
        string command,
        string payload,
        CancellationToken cancellationToken)
    {
        var frame = string.IsNullOrWhiteSpace(payload)
            ? $"{command}\n"
            : $"{command} {Convert.ToBase64String(_encoding.GetBytes(payload))}\n";

        var bytes = _encoding.GetBytes(frame);
        await _serialPort.BaseStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await _serialPort.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        using var reader = new StreamReader(_serialPort.BaseStream, _encoding, leaveOpen: true);
        var response = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        return response ?? string.Empty;
    }

    public ValueTask DisposeAsync()
    {
        _serialPort.Dispose();
        return ValueTask.CompletedTask;
    }
}
