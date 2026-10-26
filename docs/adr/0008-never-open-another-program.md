# Never open another program

Overmix never opens another program's process, the game included. Anti-cheat watches which programs open the game, and even the weakest access Windows offers counts. Names and icons are the only things Overmix needs from other programs. It gets program names from Windows' list of running processes. It gets each program's file from the same list (`NtQuerySystemInformation` with `SystemProcessIdInformation`), then reads the name and icon from that file on disk. The usual documented way, `OpenProcess` with `QueryFullProcessImageName`, is simpler but opens the program.

## Consequences

- The file lookup uses an information class that Windows doesn't document, though process viewers have relied on it for years. If a Windows update ever breaks it, Dials fall back to the process name and the program's first letter.
- Features that need to look inside a game, such as reading its FPS, are out unless they can be done without opening it.
