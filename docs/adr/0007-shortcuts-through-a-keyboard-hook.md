# Shortcuts through a keyboard hook, with Windows hotkeys as backup

Shortcuts are caught by a low-level keyboard hook on a thread of its own, which also keeps each Shortcut's key from the focused app. Windows' RegisterHotKey alone failed in practice: a key combination belongs to whichever app claims it first (Ctrl+Alt+M was taken on the gaming PC), and games can turn hotkeys off while they have focus. The hook sees keys before either of those applies. RegisterHotKey stays registered too, because the hook can't see keys while an app running as administrator has focus; a press that arrives both ways counts once.

## Consequences

- Every key press on the PC passes through the hook. It only compares the key with the Shortcuts and passes everything else straight on.
- A Shortcut wins over another app that uses the same keys; that app no longer gets them.
- Settings can turn the hook off, for anyone wary of how anti-cheat sees keyboard hooks. Shortcuts then rely on RegisterHotKey alone, so one that another app holds stops working, and Settings says so.
