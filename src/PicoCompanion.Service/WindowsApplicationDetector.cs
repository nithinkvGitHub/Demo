using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PicoCompanion.Core.Models;

namespace PicoCompanion.Service;

public sealed class WindowsApplicationDetector : IForegroundApplicationDetector
{
    private const int QueryLimitedInformation = 0x1000;
    private const int AppmodelErrorNoPackage = 15700;

    public DetectedApplication? GetForegroundApplication()
    {
        var windowHandle = GetForegroundWindow();
        if (windowHandle == IntPtr.Zero)
        {
            return null;
        }

        _ = GetWindowThreadProcessId(windowHandle, out var processId);
        return processId == 0 ? null : ReadProcess((int)processId);
    }

    public IReadOnlyList<DetectedApplication> GetRunningApplications()
    {
        var applications = new List<DetectedApplication>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var application = ReadProcess(process);
                if (application is not null)
                {
                    applications.Add(application);
                }
            }
            finally
            {
                process.Dispose();
            }
        }

        return applications;
    }

    private static DetectedApplication? ReadProcess(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return ReadProcess(process);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private static DetectedApplication? ReadProcess(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return null;
            }

            return new DetectedApplication
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                ExecutablePath = TryGetExecutablePath(process),
                PackageFamilyName = TryGetPackageFamilyName(process),
                WindowTitle = process.MainWindowTitle
            };
        }
        catch (Exception ex) when (
            ex is InvalidOperationException
                or NotSupportedException
                or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static string TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch (Exception ex) when (
            ex is InvalidOperationException
                or NotSupportedException
                or System.ComponentModel.Win32Exception)
        {
            return string.Empty;
        }
    }

    private static string TryGetPackageFamilyName(Process process)
    {
        var handle = OpenProcess(QueryLimitedInformation, false, process.Id);
        if (handle == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var length = 0;
            var firstResult = GetPackageFamilyName(handle, ref length, null);
            if (firstResult == AppmodelErrorNoPackage || length <= 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(length);
            var result = GetPackageFamilyName(handle, ref length, builder);
            return result == 0 ? builder.ToString() : string.Empty;
        }
        finally
        {
            _ = CloseHandle(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetPackageFamilyName(
        IntPtr process,
        ref int packageFamilyNameLength,
        StringBuilder? packageFamilyName);
}
