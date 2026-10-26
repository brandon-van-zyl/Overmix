# Overmix

Control your PC's sound and mic mid-game without tabbing out. Overmix lives on your second screen and pops up over your game with one shortcut.

A personal project for Windows 11 and borderless (windowed fullscreen) games. The words used here are defined in [GLOSSARY.md](GLOSSARY.md); the decisions behind them are in [docs/adr](docs/adr).

## Install it on the gaming PC

1. Sign in to GitHub and open the [latest release](https://github.com/brandon-van-zyl/Overmix/releases/latest).
2. Download `Overmix-win-Setup.exe` and run it. The build isn't signed, so Windows may say "Windows protected your PC": choose **More info**, then **Run anyway**.
3. Overmix opens its Panel and puts its icon in the tray. Add your Apps with the sliders icon.

Every push to main builds a new release ([ADR 0005](docs/adr/0005-velopack-installer.md)). To update, download the newest `Setup.exe` and run it: it installs over the old version and keeps your settings.

- Settings live in `%AppData%\Overmix\settings.json`.
- The log is `%AppData%\Overmix\overmix.log`. Look there first if something doesn't work.
- Uninstall from Windows Settings > Apps.

### Or build it on the PC

With the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Git, a build packed on the PC updates itself: after the first install, the running Overmix installs each new build within a few seconds.

```powershell
git clone https://github.com/brandon-van-zyl/Overmix
cd Overmix
powershell -ExecutionPolicy Bypass -File .\build\pack.ps1
.\releases\Overmix-win-Setup.exe
```

## v1

### Panel (second screen)

- A normal window with a Dial for each App, plus the Game Dial.
- Turns red while Mic Mute is on. So does the tray icon.
- Output Switch: flips between two chosen outputs, such as headset and speakers. The mic stays as it is.
- Now Playing: title, artist, play/pause, next and previous.
- Settings behind the sliders icon.
- No taskbar button. Lives in the tray: click to show or hide it, right-click to quit. Windows 11 may tuck the icon under the **^** arrow.
- Starts with Windows (on from the first install; Settings can turn it off) with the Panel open or hidden as you left it. Started by hand, or by the installer after an update, it always shows the Panel.
- If Overmix can't start, it says why in a message box and in the log.

### Overlay (main screen, over the Game)

- The Popup Shortcut shows it centred over the Game. Drag it to move it; it remembers where.
- It takes the mouse while open. The Popup Shortcut or Esc closes it and hands focus back to the Game.
- Shows the Dials, Mic Mute, the media buttons and the song title.

### Shortcuts

These work mid-game without opening anything, even if another app claimed the same keys or a game turns hotkeys off ([ADR 0007](docs/adr/0007-shortcuts-through-a-keyboard-hook.md)):

- the Popup Shortcut (Ctrl+Alt+O to start with)
- Mic Mute (Ctrl+Alt+M to start with)
- volume up and down for any App that has them set (5% per press)

### Apps and Dials

- Every program that plays sound gets a Dial by itself, like in Windows' volume mixer. Overmix's own sounds and Windows' shell don't.
- Remove the ones you don't want in Settings. They stay removed, and Settings can bring them back with their old setup.
- Add a program before it has ever played sound by browsing to it.
- Each Dial shows its program's icon and name, the same as its desktop shortcut.
- A Dial is greyed out until its App is Connected, then lights up by itself.
- The Game Dial comes first and follows the Game: the most recent program to fill the main screen. While a program is the Game, its own App Dial steps aside, so it never has two.
- Scroll, drag or use the arrow keys on a Dial. There's no separate mute per App: a Dial at zero silences it.

### Mic Mute

- Mutes every microphone in Windows, including one plugged in while muted. It never changes what you hear.
- The Mute Chime plays two falling notes as you mute and two rising notes as you unmute.
- The Mute Border puts a thin red line around every screen while you're muted. Clicks go straight through it, and it never takes focus from the game.
- Settings can turn the chime and the border off.
- Always shows the real Windows state, whatever changed it, and stays muted if Overmix closes.
- No Discord connection in v1, so friends hear silence rather than seeing a mute icon.

## How it stays light

- It never checks for changes on a timer. Windows tells Overmix when sound, devices, mic mute, the focused window, media or Shortcuts change, and it sleeps in between ([ADR 0006](docs/adr/0006-react-to-windows-never-poll.md)).
- Nothing redraws unless something on screen changed, and nothing animates.
- One background thread does all the audio work and another reads icons. Each icon is read once.
- The keyboard hook has a thread of its own that does nothing but compare each key with your Shortcuts.
- The Mute Border is plain Windows strips, drawn once as they appear. While it shows over a game, Windows may add about a frame of delay; turn it off in Settings if that matters to you.
- It's compiled ahead of time and runs without the background garbage-collector thread.

## Later

- Discord's own mute, deafen and per-friend volume
- a game-versus-chat balance Dial
- cover art from Steam and other launchers
- FPS and temperatures, launching apps

## Development

- C# / .NET 10 with Avalonia 12 ([ADR 0004](docs/adr/0004-csharp-avalonia.md)). Overmix runs on Windows only, but everything builds and every test runs on a Mac too.
- `dotnet test` runs all the tests. The UI tests run headless and save screenshots to `tests/Overmix.Tests/bin/.../screenshots`.
- GitHub Actions builds, tests and releases every push to main ([`.github/workflows/release.yml`](.github/workflows/release.yml)). `build\pack.ps1` does the same on a PC.

| Project | What it is |
| --- | --- |
| `src/Overmix.Core` | The rules: Dials, the Game, Mic Mute, the Output Switch, Shortcuts and settings. No Windows code. |
| `src/Overmix.WindowsPlatform` | The real Windows parts: sound, mics, outputs, Now Playing, Shortcuts, focus and icons. |
| `src/Overmix` | The app: the Panel, the Overlay, the tray icon and updates. |
| `tests/Overmix.Fakes` | A pretend gaming PC for tests. |
| `tests/Overmix.Core.Tests` | Tests for the rules. |
| `tests/Overmix.Tests` | Headless UI tests and screenshots. |

## Risks to test first on the PC

- The Overlay shows over Valorant, CS2 and Overwatch in borderless mode with their anti-cheat running.
- Shortcuts fire while each game has focus. A game running as administrator hides keys from the keyboard hook; Windows' own hotkeys still cover Ctrl or Alt combinations there.
- The Mute Border shows over each game and doesn't upset its anti-cheat.
- Closing the Overlay returns focus and mouse capture to the Game cleanly.
- Some games go quiet when they lose focus; check each game's "sound in background" setting.
- The Output Switch uses a private Windows API (IPolicyConfig). Check that headset and speakers really swap.
- A game's icon needs read access to its process, which anti-cheat may refuse. The Dial then shows the game's first letter.
