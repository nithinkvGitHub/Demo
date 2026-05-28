using PicoCompanion.Core.Models;

namespace PicoCompanion.Service;

public interface IForegroundApplicationDetector
{
    DetectedApplication? GetForegroundApplication();

    IReadOnlyList<DetectedApplication> GetRunningApplications();
}
