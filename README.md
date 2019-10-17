# windows-terminal-quake
Companion program for the new Windows Terminal that enables Quake-style drop down. Forked from original work by [flyingpie](https://github.com/flyingpie/windows-terminal-quake)

![Preview](https://files.flyingpie.nl/windows-terminal-quake.gif)

- Runs alongside the new Windows Terminal
- Toggle using CTRL+~ or CTRL+Q
- Shows up on the screen where the mouse is (eg. multi-monitor and multi-workspace)

## Usage
There are a couple of options:

- Download the latest release from the [releases page](https://github.com/flyingpie/windows-terminal-quake/releases).
- Clone/download the source and run **build.ps1**.
- Clone/download the source and build using Visual Studio.

## Features

- Persists window dimensions between sessions
- Pressing hotkey when terminal window is not foreground will bring it to foreground; otherwise it will "slide out"
- Pressing hotkey when terminal window is minimized will "slide in"
- Relaunching `windows-terminal-quake` will also give focus to active terminal window

## Known issues/limitations

- Some minor flicker when restoring window after a "minimize"
- Should support features like "fit width", hover, etc
- Probably will not be improved much except to fix really bad bugs, since it's expected that Windows Terminal will probably natively support this/similar much-requested feature
