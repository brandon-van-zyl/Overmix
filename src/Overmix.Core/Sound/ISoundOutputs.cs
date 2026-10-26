namespace Overmix.Core.Sound;

/// <summary>A sound output on the PC, such as a headset or speakers.</summary>
public sealed record SoundOutput(DeviceId Id, string Name);

/// <summary>The PC's sound outputs. Raises events on the UI thread.</summary>
public interface ISoundOutputs
{
    IReadOnlyList<SoundOutput> All { get; }

    /// <summary>The output Windows plays sound through by default.</summary>
    DeviceId? Current { get; }

    void MakeCurrent(DeviceId output);

    /// <summary>An output was plugged in or unplugged, or the current one changed.</summary>
    event EventHandler? Changed;
}
