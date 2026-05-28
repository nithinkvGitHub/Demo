using System.IO.Ports;
using PicoCompanion.Core.Models;

namespace PicoCompanion.Core.Services;

public sealed class SerialDongleTransportFactory
{
    private readonly CompanionOptions _options;

    public SerialDongleTransportFactory(CompanionOptions options)
    {
        _options = options;
    }

    public IEnumerable<string> GetCandidatePorts()
    {
        if (!string.IsNullOrWhiteSpace(_options.PreferredPortName))
        {
            yield return _options.PreferredPortName;
            yield break;
        }

        foreach (var portName in SerialPort.GetPortNames().Order(StringComparer.OrdinalIgnoreCase))
        {
            yield return portName;
        }
    }

    public IDongleTransport Open(string portName) =>
        new SerialDongleTransport(portName, _options.SerialBaudRate);
}
