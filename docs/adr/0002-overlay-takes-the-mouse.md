# The Overlay takes the mouse while it's open

Pressing the Popup Shortcut brings the Overlay to the front with a visible cursor, so Dials work by clicking and scrolling; pressing it again hands focus straight back to the Game. The alternative was keeping the Game focused and driving the Overlay with keys the app hides from the Game, which avoids the side effects below but is clumsier and depends on intercepting keys mid-match. Actions that must never interrupt play (Mic Mute, App volume up and down) are Shortcuts instead.

## Consequences

- While the Overlay is open the Game gets no input, and some games pause or go quiet when they lose focus.
- Closing the Overlay must reliably return focus, and the mouse, to the Game.
