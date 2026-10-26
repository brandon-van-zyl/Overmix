# Plain overlay window; borderless games only

The Overlay is an ordinary always-on-top window: it never injects into or hooks the game, and it isn't an Xbox Game Bar widget. Drawing over true exclusive-fullscreen games needs injection, which risks anti-cheat bans in the competitive shooters this app is for (Valorant, CS2, Overwatch). So we support borderless (windowed fullscreen) games only; all three offer that mode, and on Windows 11 it costs little or no performance.

## Considered Options

- **Inject like the Discord and Steam overlays**: works in exclusive fullscreen, but anti-cheat treats unknown injected code as a cheat.
- **Xbox Game Bar widget**: Microsoft's supported overlay, but widgets can't change other apps' volume without a separate full-trust helper process.

## Consequences

- Anything drawn over the game stays small and shows only when needed. While it's visible, Windows may add about a frame of delay, depending on the graphics card.
- The Overlay is never a full-screen transparent layer: that is exactly what wallhack overlays look like, and anti-cheat scans for them. For the same reason the Mute Border is four thin solid strips along each screen's edges, not one see-through window over it.
