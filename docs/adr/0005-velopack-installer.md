# Velopack installer, built by GitHub Actions

Overmix is installed with Velopack. Every push to main builds it on a Windows runner, runs the tests and publishes `Overmix-win-Setup.exe` as a GitHub release, so the gaming PC needs no build tools: download the newest Setup.exe and it installs over the old version, keeping settings. A build packed on the PC with `build\pack.ps1` also updates itself from that PC's `releases\` folder, which suits a build-and-try loop there. MSIX would need a trusted signing certificate, and an Inno Setup or MSI installer can't update itself.

## Consequences

- Releases come from a private repo, so the installed app can't fetch them by itself; updating a downloaded install means running the newer Setup.exe. Self-updating from GitHub would need public releases or a stored token.
- Builds aren't code-signed, so Windows SmartScreen may warn the first time a Setup.exe runs.
- A self-update restarts Overmix, but never while the Overlay is open.
