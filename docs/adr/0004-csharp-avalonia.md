# C# with Avalonia, so the UI runs on the Mac

Overmix is written in C#/.NET with Avalonia. Code is written on a Mac but the app only does its job on the Windows gaming PC; Avalonia lets the Panel and Overlay run on the Mac with pretend sound data, so most work can be seen and tested without the PC. Everything Windows-only (sound control, Mic Mute, Shortcuts, window focus, the tray icon's start-with-Windows) sits behind interfaces, with a fake implementation for the Mac.

## Considered Options

- **WPF**: the proven choice for Windows tray utilities (EarTrumpet uses it), but it runs only on Windows, so every UI change would need the PC.
- **WinUI 3**: Windows-only, and awkward for tray icons and always-on-top tool windows.
