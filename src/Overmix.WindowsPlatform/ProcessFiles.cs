using System.Runtime.InteropServices;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Finds the file a program runs from without opening the program. Windows answers from its own list of
/// processes, so Overmix never holds a handle to any program, games included (ADR 0008).
/// </summary>
internal static unsafe partial class ProcessFiles
{
    private const int SystemProcessIdInformation = 88;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
    private const string NetworkDevice = @"\Device\Mup\";

    // Replaced whole, never changed, so the audio and icon threads can both read it.
    private static Dictionary<string, string>? _drives;

    /// <summary>The program file a process runs, such as C:\Program Files\Discord\Discord.exe, or null if Windows won't say.</summary>
    public static string? PathOf(int processId)
    {
        if (DevicePath(processId) is not { } devicePath) return null;
        if (devicePath.StartsWith(NetworkDevice, StringComparison.OrdinalIgnoreCase)) return @"\\" + devicePath[NetworkDevice.Length..];

        // A drive plugged in since the last look isn't known yet.
        return OnDrive(devicePath, _drives ??= DriveDevices()) ?? OnDrive(devicePath, _drives = DriveDevices());
    }

    /// <summary>The path as Windows keeps it, like \Device\HarddiskVolume3\Program Files\Discord\Discord.exe.</summary>
    private static string? DevicePath(int processId)
    {
        var length = 512;
        while (true)
        {
            var buffer = new char[length];
            fixed (char* text = buffer)
            {
                var info = new SystemProcessIdInfo
                {
                    ProcessId = processId,
                    ImageName = new UnicodeString { MaximumLength = (ushort)(length * sizeof(char)), Buffer = text },
                };
                var status = NtQuerySystemInformation(SystemProcessIdInformation, &info, (uint)sizeof(SystemProcessIdInfo), null);

                // Too long for the buffer: Windows says how much room the path needs.
                if (status == StatusInfoLengthMismatch && info.ImageName.MaximumLength > length * sizeof(char))
                {
                    length = info.ImageName.MaximumLength / sizeof(char);
                    continue;
                }

                return status >= 0 && info.ImageName.Length > 0 ? new string(text, 0, info.ImageName.Length / sizeof(char)) : null;
            }
        }
    }

    private static string? OnDrive(string devicePath, Dictionary<string, string> drives)
    {
        foreach (var (device, drive) in drives)
        {
            if (devicePath.Length > device.Length && devicePath[device.Length] == '\\' &&
                devicePath.StartsWith(device, StringComparison.OrdinalIgnoreCase))
                return drive + devicePath[device.Length..];
        }

        return null;
    }

    /// <summary>Which device each drive letter is, like \Device\HarddiskVolume3 for C:.</summary>
    private static Dictionary<string, string> DriveDevices()
    {
        var devices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var target = stackalloc char[1024];
        foreach (var root in Environment.GetLogicalDrives())
        {
            var drive = root.TrimEnd('\\');
            if (QueryDosDevice(drive, target, 1024) > 0) devices[new string(target)] = drive;
        }

        return devices;
    }

    [LibraryImport("ntdll.dll")]
    private static partial int NtQuerySystemInformation(int informationClass, void* information, uint length, uint* returnLength);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint QueryDosDevice(string deviceName, char* targetPath, uint maxLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public char* Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemProcessIdInfo
    {
        public nint ProcessId;
        public UnicodeString ImageName;
    }
}
