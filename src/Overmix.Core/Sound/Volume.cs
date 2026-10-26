namespace Overmix.Core.Sound;

/// <summary>A sound level from 0 (silent) to 100 (full).</summary>
public readonly record struct Volume
{
    public Volume(int percent) => Percent = Math.Clamp(percent, 0, 100);

    public int Percent { get; }

    /// <summary>Converts from Windows' 0.0 to 1.0 scale.</summary>
    public static Volume FromScalar(float scalar) => new((int)MathF.Round(scalar * 100));

    /// <summary>Converts to Windows' 0.0 to 1.0 scale.</summary>
    public float ToScalar() => Percent / 100f;

    /// <summary>Turns the volume up (positive) or down (negative) by a number of percentage points.</summary>
    public Volume Nudge(int by) => new(Percent + by);

    public override string ToString() => $"{Percent}%";
}
