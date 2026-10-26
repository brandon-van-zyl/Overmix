namespace Overmix.Core.Sound;

/// <summary>
/// A program's executable name, such as "Discord" or "VALORANT-Win64-Shipping".
/// Compared without regard to case, and without the ".exe" ending.
/// </summary>
public readonly record struct ProgramName
{
    public ProgramName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var name = value.Trim();
        Value = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    public string Value { get; }

    public bool Equals(ProgramName other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
