# LaunchForge

Build app-launch sequences in your browser and export them as `.bat` or `.ps1` scripts. No install.

**Use it now: https://aemiliusxiv.github.io/LaunchForge/**

Chain together app launches, Steam game starts, waits, and process checks, then download a script you can double-click. Never hand-write another `timeout /t 20` line again.

## What it does

- **Launch apps** by path (paste with "Copy as path"; quotes are stripped automatically)
- **Launch Steam games** by App ID, with a searchable picker covering the full Steam catalog
- **Wait** a set number of seconds
- **Kill a process** by name
- **Wait for a process to start or exit** (with optional timeout)
- **Run a PowerShell snippet** inline

Build a sequence, watch the live `.bat`/`.ps1` preview update as you edit, then export. Sequences also save as `.lfjson` project files so you can reopen and change them later. The Import button reads those projects back, and it can also open an existing `.bat` or `.ps1` so you can pick up where you left off.

## Output formats

| Format | Notes |
|---|---|
| `.bat` (default) | Double-click to run, no setup needed. Matches hand-written batch style. |
| `.ps1` | For people who prefer PowerShell. Needs an execution policy that permits local scripts. |

The scripts target the Windows command interpreter (`cmd.exe`) and Windows PowerShell 5+ respectively. LaunchForge produces the script text; it doesn't run anything itself.

## Disclaimer

LaunchForge is provided as-is, without warranty of any kind. Scripts it generates can launch, stop, and interact with programs and files on your PC. Review any generated script before running it, and only run sequences you have built yourself or trust the source of.

## Licence

MIT; see [LICENSE](LICENSE). You are free to use, modify, and redistribute this project, provided the original copyright notice and licence text are retained.

## Trademarks and affiliation

LaunchForge is an independent, unofficial project. It is not affiliated with, endorsed by, sponsored by, or otherwise associated with any of the companies named below. Every trademark is the property of its owner and appears here only to describe what the tool does. No claim of ownership over any third-party mark is made or implied.

### Valve Corporation

Steam, the Steam logo, and the `steam://` URL scheme are trademarks or registered trademarks of Valve Corporation in the United States and other countries. A "Launch Steam Game" step produces a `steam://rungameid/...` link that asks your own installed Steam client to start a game; LaunchForge is not built by Valve and Valve has neither reviewed nor endorsed it. The game list in the picker comes from Valve's public Steam Web API, used under its terms of use, and no Valve account data is read or stored.

### Microsoft Corporation

Microsoft, Windows, PowerShell, and the Windows Command Processor (`cmd.exe`) are trademarks of the Microsoft group of companies. The `.bat` and `.ps1` files LaunchForge generates are plain text meant to be run on Windows with those built-in tools. LaunchForge is not built by Microsoft and Microsoft has neither reviewed nor endorsed it.

### Game titles and other names

Game names shown in the Steam picker, drawn from Valve's public app list, are trademarks of their respective publishers and developers. Any other product, company, or service name that appears in this project belongs to its respective owner. Mentioning a name does not imply endorsement, sponsorship, or any partnership in either direction.
