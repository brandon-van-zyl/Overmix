using Overmix.Core.Sound;
using Overmix.Core.Voice;

namespace Overmix.Core.Shortcuts;

/// <summary>Registers every Shortcut with the PC and does what each one is for.</summary>
public sealed class ShortcutDispatcher
{
    /// <summary>How far an App's volume Shortcuts turn its Dial, in percentage points.</summary>
    public const int VolumeStep = 5;

    private readonly IShortcutKeys _keys;
    private readonly AppList _apps;
    private readonly Mixer _mixer;
    private readonly MicMute _micMute;
    private Dictionary<Shortcut, Action> _actions = [];
    private bool _useKeyboardHook;

    public ShortcutDispatcher(
        IShortcutKeys keys,
        AppList apps,
        Mixer mixer,
        MicMute micMute,
        Shortcut? popupShortcut,
        Shortcut? micMuteShortcut,
        bool useKeyboardHook)
    {
        _keys = keys;
        _apps = apps;
        _mixer = mixer;
        _micMute = micMute;
        PopupShortcut = popupShortcut;
        MicMuteShortcut = micMuteShortcut;
        _useKeyboardHook = keys.UseKeyboardHook = useKeyboardHook;
        keys.Pressed += (_, shortcut) =>
        {
            if (_actions.TryGetValue(shortcut, out var action)) action();
        };
        apps.Changed += (_, _) => Rebind();
        Rebind();
    }

    public Shortcut? PopupShortcut { get; private set; }

    public Shortcut? MicMuteShortcut { get; private set; }

    /// <summary>
    /// Whether a keyboard hook catches Shortcuts that another app already uses or a game blocks. Turned off,
    /// only Windows' own hotkeys work, and a Shortcut another app uses becomes unavailable.
    /// </summary>
    public bool UseKeyboardHook
    {
        get => _useKeyboardHook;
        set
        {
            if (value == _useKeyboardHook) return;
            _useKeyboardHook = _keys.UseKeyboardHook = value;
            Rebind();
        }
    }

    /// <summary>Shortcuts the PC wouldn't register, usually because another app already uses them.</summary>
    public IReadOnlyCollection<Shortcut> Unavailable { get; private set; } = [];

    /// <summary>The Popup Shortcut was pressed.</summary>
    public event EventHandler? PopupPressed;

    /// <summary>A Shortcut changed, or became available or unavailable.</summary>
    public event EventHandler? Changed;

    public void SetPopupShortcut(Shortcut? shortcut)
    {
        if (shortcut == PopupShortcut) return;
        PopupShortcut = shortcut;
        Rebind();
    }

    public void SetMicMuteShortcut(Shortcut? shortcut)
    {
        if (shortcut == MicMuteShortcut) return;
        MicMuteShortcut = shortcut;
        Rebind();
    }

    private void Rebind()
    {
        foreach (var shortcut in _actions.Keys) _keys.Unregister(shortcut);

        var actions = new Dictionary<Shortcut, Action>();
        if (PopupShortcut is { } popup) actions.TryAdd(popup, () => PopupPressed?.Invoke(this, EventArgs.Empty));
        if (MicMuteShortcut is { } mute) actions.TryAdd(mute, _micMute.Toggle);
        foreach (var app in _apps.Items)
        {
            var target = DialTarget.For(app.Program);
            if (app.VolumeUp is { } up) actions.TryAdd(up, () => _mixer.Nudge(target, VolumeStep));
            if (app.VolumeDown is { } down) actions.TryAdd(down, () => _mixer.Nudge(target, -VolumeStep));
        }

        Unavailable = actions.Keys.Where(shortcut => !_keys.Register(shortcut)).ToList();
        _actions = actions;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
