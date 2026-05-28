namespace PicoCompanion.Core.Models;

public sealed record CompanionOptions
{
    public string ControlPipeName { get; init; } = "PicoCompanion.Service";

    public string PreferredPortName { get; init; } = string.Empty;

    public int SerialBaudRate { get; init; } = 115200;

    public string BootloaderVolumeLabel { get; init; } = "RPI-RP2";

    public int FirmwareUpdateTimeoutSeconds { get; init; } = 60;

    public string DataDirectory { get; init; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PicoCompanion");
}
