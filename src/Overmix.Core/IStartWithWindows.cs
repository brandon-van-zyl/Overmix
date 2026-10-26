namespace Overmix.Core;

/// <summary>Whether Overmix starts when you sign in to Windows.</summary>
public interface IStartWithWindows
{
    bool IsOn { get; }

    void Set(bool on);
}
