using PicoCompanion.Core.Models;

namespace PicoCompanion.Core.Services;

public sealed class Uf2FirmwareUpdater
{
    private readonly CompanionOptions _options;

    public Uf2FirmwareUpdater(CompanionOptions options)
    {
        _options = options;
    }

    public async Task UpdateAsync(string uf2Path, CancellationToken cancellationToken)
    {
        if (!File.Exists(uf2Path))
        {
            throw new FileNotFoundException("Firmware image was not found.", uf2Path);
        }

        if (!string.Equals(Path.GetExtension(uf2Path), ".uf2", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Firmware updates must use a .uf2 image.");
        }

        var bootDrive = await WaitForBootloaderDriveAsync(cancellationToken).ConfigureAwait(false);
        var destination = Path.Combine(bootDrive.RootDirectory.FullName, Path.GetFileName(uf2Path));

        File.Copy(uf2Path, destination, overwrite: true);

        await WaitForBootloaderToDisconnectAsync(bootDrive.RootDirectory.FullName, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<DriveInfo> WaitForBootloaderDriveAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(_options.FirmwareUpdateTimeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var drive = FindBootloaderDrive();
            if (drive is not null)
            {
                return drive;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Timed out waiting for {_options.BootloaderVolumeLabel} bootloader drive.");
    }

    private async Task WaitForBootloaderToDisconnectAsync(
        string rootDirectory,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(_options.FirmwareUpdateTimeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(rootDirectory))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }
    }

    private DriveInfo? FindBootloaderDrive()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady
                    && string.Equals(
                        drive.VolumeLabel,
                        _options.BootloaderVolumeLabel,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return drive;
                }
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
        }

        return null;
    }
}
