# Mic Mute is a Windows mute, not Discord's

Mic Mute mutes every microphone in Windows itself, and v1 doesn't connect to Discord at all. Discord's own mute would show friends the mute icon, but it needs a personal app registered on Discord's developer site, only one app at a time may control Discord's voice settings, and Discord undoes the mute when the controlling app disconnects, so a crash would leave you live. A Windows mute also covers in-game voice chat, which Discord's mute doesn't.

## Consequences

- Friends on Discord hear silence but don't see a mute icon.
- Muting inside Discord itself doesn't turn the Panel red.
