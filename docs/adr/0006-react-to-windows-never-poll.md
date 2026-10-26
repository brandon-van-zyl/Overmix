# React to Windows' notifications; never poll

Overmix sits in the background while you play, so it does no work until something changes. Windows notifies it about audio sessions, sound devices, mic mute, the foreground window, media and Shortcuts, and each notification triggers one fresh look at what changed. All the audio work runs on one background thread that otherwise sleeps. Checking every second would have been simpler to write, but it would wake the PC constantly for nothing.

## Consequences

- Notifications come in bursts, so Overmix waits 150 ms for a burst to settle before looking. A mic plugged in while Mic Mute is on is muted within that time, not instantly.
- A slow look every 30 seconds catches anything Windows forgets to report.
