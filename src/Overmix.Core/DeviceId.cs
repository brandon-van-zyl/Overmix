namespace Overmix.Core;

/// <summary>Windows' ID for a sound device: a microphone or a sound output.</summary>
public readonly record struct DeviceId(string Value)
{
    public override string ToString() => Value;
}
