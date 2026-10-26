using System.Runtime.InteropServices;

namespace Overmix.WindowsPlatform;

/// <summary>
/// Changes Windows' default sound output. Windows has no public API for this; IPolicyConfig is the
/// long-stable private one that EarTrumpet, SoundSwitch and AudioDeviceCmdlets use.
/// </summary>
internal static class PolicyConfig
{
    private const int Console = 0;
    private const int Multimedia = 1;
    private const int Communications = 2;

    public static void SetDefaultOutput(string deviceId)
    {
        var config = (IPolicyConfig)new PolicyConfigClient();
        try
        {
            foreach (var role in new[] { Console, Multimedia, Communications })
                Marshal.ThrowExceptionForHR(config.SetDefaultEndpoint(deviceId, role));
        }
        finally
        {
            Marshal.ReleaseComObject(config);
        }
    }

    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClient
    {
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        // Only SetDefaultEndpoint is used. The methods before it hold its place in the interface.
        [PreserveSig]
        int GetMixFormat(nint deviceId, nint format);

        [PreserveSig]
        int GetDeviceFormat(nint deviceId, int useDefault, nint format);

        [PreserveSig]
        int ResetDeviceFormat(nint deviceId);

        [PreserveSig]
        int SetDeviceFormat(nint deviceId, nint endpointFormat, nint mixFormat);

        [PreserveSig]
        int GetProcessingPeriod(nint deviceId, int useDefault, nint defaultPeriod, nint minimumPeriod);

        [PreserveSig]
        int SetProcessingPeriod(nint deviceId, nint period);

        [PreserveSig]
        int GetShareMode(nint deviceId, nint mode);

        [PreserveSig]
        int SetShareMode(nint deviceId, nint mode);

        [PreserveSig]
        int GetPropertyValue(nint deviceId, nint key, nint value);

        [PreserveSig]
        int SetPropertyValue(nint deviceId, nint key, nint value);

        [PreserveSig]
        int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);

        [PreserveSig]
        int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int visible);
    }
}
