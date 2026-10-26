# Overmix

A Windows app for controlling sound and voice while gaming, without tabbing out of the game. It stays open on a second screen and pops up over the game on demand.

## Language

### Screens

**Panel**:
The app's main window, kept open on the second screen while you play. It shows every Dial and the Mic Mute state, and holds the settings.
_Avoid_: Dashboard, main window, dock

**Overlay**:
A compact strip of the same Dials that pops up over the game on the main screen, then hides again.
_Avoid_: HUD, popup, widget

### Shortcuts

**Shortcut**:
A keyboard combination that works mid-game without leaving the game: the Popup Shortcut, the Mic Mute Shortcut, or an App's volume up and down Shortcuts.
_Avoid_: Hotkey, keybind, binding

**Popup Shortcut**:
The Shortcut that shows and hides the Overlay.
_Avoid_: Overlay key, toggle key

### Sound

**App**:
A program whose sound you control, such as Discord or Spotify, with its own Dial. Every program that plays sound becomes an App by itself, unless you've removed it; removed Apps stay removed until you bring them back.
_Avoid_: Application, program, source

**Dial**:
The volume control for one App, or for the Game.
_Avoid_: Slider, knob, fader

**Connected**:
An App that is running and using sound, so its Dial works. A Dial whose App isn't Connected is greyed out.
_Avoid_: Active, running, live, online

**Game**:
The most recent program to fill the main screen. It stays the Game while the Overlay is open, until you quit it or another program fills the screen.
_Avoid_: Foreground app, current app

**Game Dial**:
The Dial that always controls the Game, first in line. While a program is the Game, its own App Dial steps aside for it.
_Avoid_: Game slot, auto dial

**Output Switch**:
The button that flips your sound between two chosen outputs, normally your headset and your speakers. Your microphone stays as it is.
_Avoid_: Device switcher, audio toggle

**Now Playing**:
Whatever song or video is playing in any app, shown with its title and artist alongside play/pause, next and previous.
_Avoid_: Media player, music widget

### Voice

**Mic Mute**:
Every microphone on the PC muted in Windows itself, so no app can hear you: not Discord, not in-game voice chat, nothing. It never changes what you hear.
_Avoid_: Mute (on its own), mic off

**Mute Chime**:
The short sound as Mic Mute turns on (two falling notes) or off (two rising notes).
_Avoid_: Beep, mute sound

**Mute Border**:
A red border around every screen while Mic Mute is on.
_Avoid_: Red frame, mute overlay
