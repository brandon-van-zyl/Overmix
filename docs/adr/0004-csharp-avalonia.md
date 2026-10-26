# C# with Avalonia, so the UI can be tested on the Mac

Overmix is written in C#/.NET 10 with Avalonia 12 and runs on Windows only. Code is written on a Mac and tried out on the gaming PC; Avalonia's headless mode runs the real Panel and Overlay in tests on the Mac and saves screenshots of them, so most UI work is checked before it reaches the PC. Everything Windows-only (sound, mics, outputs, Now Playing, Shortcuts, focus, icons) sits behind interfaces in Overmix.Core, implemented in Overmix.WindowsPlatform, with fakes for tests.

## Considered Options

- **WPF**: the proven choice for Windows tray utilities (EarTrumpet uses it), but its UI can't run anywhere else, not even headless in tests on the Mac.
- **WinUI 3**: Windows-only, and awkward for tray icons and always-on-top tool windows.
