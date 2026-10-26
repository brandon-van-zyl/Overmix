# Overmix

Control your PC's sound and mic mid-game without tabbing out. Overmix lives on your second screen and pops up over your game with one shortcut.

A personal project for Windows 11 and borderless (windowed fullscreen) games. The words used here are defined in [GLOSSARY.md](GLOSSARY.md); the decisions behind them are in [docs/adr](docs/adr).

## v1

### Panel (second screen)

- A normal window with a Dial for each App, plus the Game Dial.
- Turns red while Mic Mute is on.
- Output Switch: flips between two chosen outputs, such as headset and speakers. The mic stays as it is.
- Now Playing: title, artist, play/pause, next and previous.
- Settings behind a gear icon.
- No taskbar button. Lives in the tray: click to show or hide it, right-click to quit. Starts with Windows, open or hidden as you left it.

### Overlay (main screen, over the Game)

- The Popup Shortcut shows it centred over the Game. Drag it to move it; it remembers where.
- It takes the mouse while open. The Popup Shortcut or Esc closes it and hands focus back to the Game.
- Shows the Dials, Mic Mute, the media buttons and the song title.

### Shortcuts

These work mid-game without opening anything:

- the Popup Shortcut
- Mic Mute
- volume up and down for any App that has them set (5% per press)

### Apps and Dials

- You build your App list, for example Discord and Spotify. Programs not on the list are ignored.
- Add an App by picking from programs that are making sound now, or by browsing to the program.
- A Dial is greyed out until its App is Connected, then lights up by itself.
- The Game Dial follows the Game: the most recent program to fill the main screen that isn't an App.
- Scroll or drag a Dial to change it. There's no separate mute per App: a Dial at zero silences it.

### Mic Mute

- Mutes every microphone in Windows, including one plugged in while muted. It never changes what you hear.
- Always shows the real Windows state, whatever changed it, and stays muted if Overmix closes.
- No Discord connection in v1, so friends hear silence rather than seeing a mute icon.

## Later

- Discord's own mute, deafen and per-friend volume
- a game-versus-chat balance Dial
- FPS and temperatures, launching apps

## Development

- C# / .NET with Avalonia. Written on a Mac; run and tested on the Windows gaming PC.
- Windows-only parts sit behind interfaces with fakes for the Mac ([ADR 0004](docs/adr/0004-csharp-avalonia.md)).

## Risks to test first on the PC

- The Overlay shows over Valorant, CS2 and Overwatch in borderless mode with their anti-cheat running.
- Shortcuts fire while each game has focus. Some games block normal shortcuts, so a keyboard-hook fallback may be needed.
- Closing the Overlay returns focus and mouse capture to the Game cleanly.
- Some games go quiet when they lose focus; check each game's "sound in background" setting.
