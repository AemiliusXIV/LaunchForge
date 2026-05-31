# Security notes

This document explains what LaunchForge does and does not do, for automated scanners and human reviewers.

## What LaunchForge does

LaunchForge is a sequence builder. The user assembles an ordered list of steps (launch an app, wait, kill a process, etc.), then either generates a `.bat` or `.ps1` script from the sequence, or runs it directly inside the app.

## What it does not do

- **No automatic network requests.** The only outbound connection is the Steam app catalog fetch described below, and it only happens when the user explicitly clicks "Refresh" in the Steam game picker.
- **No reading of Steam credentials, tokens, or account information.** The registry read described below accesses only game name and install state.
- **No process memory access.** The app never calls `OpenProcess`, `ReadProcessMemory`, or equivalent. It has no awareness of other processes except to check if they are running (via `Process.GetProcessesByName`) or to start/stop them as instructed by the user's sequence.
- **No write access to arbitrary files.** All file writes go to paths the user selects via a Save dialog, except for the temp `.ps1` file described below and the Steam catalog cache.
- **No screen capture or OCR.**
- **No persistence beyond the user's chosen save path** and the Steam catalog cache in `%AppData%\LaunchForge\`.

## Surface-by-surface breakdown

### `Process.Start`

Used to launch apps and Steam games as defined in the user's sequence. Every launch corresponds to a step the user explicitly added. The app never launches anything without an explicit user-defined step triggering it.

### `WScript.Shell` COM (`dynamic`)

Used **read-only** to resolve `.lnk` shortcut files, specifically to read the `TargetPath` and `Arguments` properties. This is how LaunchForge detects whether a pasted desktop shortcut points to a Steam game so it can suggest a conversion. The shell object is never used to execute anything; only `CreateShortcut` is called, not `Run` or `Exec`.

### Registry access (`HKCU\Software\Valve\Steam\Apps\`)

Read-only. LaunchForge reads the `Name` string value and `Installed` DWORD for each subkey to populate the Steam game picker with installed games. No tokens, no account data, no write access.

### Temp `.ps1` file

When the user includes a **Run PowerShell** step and runs the sequence directly in-app, the step's script is written to `%TEMP%\lf_ps_<guid>.ps1`. The file is deleted in a `finally` block immediately after the PowerShell process exits (or if it fails to start). No other files are written to `%TEMP%`.

### Clipboard access

LaunchForge overrides the WPF Paste command on path input fields. When the user pastes text, the app reads it from the clipboard, strips surrounding quotes (which Windows "Copy as path" adds), and inserts the cleaned text into the field. Nothing is stored or transmitted. The clipboard is only read during a user-initiated paste action.

### `powershell -ExecutionPolicy Bypass` in generated scripts

This appears only in `.bat` files the user explicitly exports via the Export button. The app itself does not use this flag when running sequences directly (it calls `powershell.exe` with a temp file path). The flag bypasses execution policy only for the user's own generated script on their own machine; it does not modify the system policy.

### Steam catalog network request

When the user clicks **Refresh** in the Steam game picker, the app fetches:

```
GET https://api.steampowered.com/ISteamApps/GetAppList/v2/
```

This is Valve's public, unauthenticated app catalog endpoint. No user data is sent; the request contains no headers beyond a standard `Accept-Encoding: gzip`. The response (a list of all Steam app names and IDs) is cached locally at `%AppData%\LaunchForge\steam_app_cache.json`. The cache is never sent anywhere.

## Data flow summary

```
User pastes path → clipboard read (strip quotes) → local field only

User pastes .lnk path → WScript.Shell CreateShortcut (read-only) → local analysis only

User opens Steam picker → registry read (HKCU, read-only) → local picker only

User clicks Refresh → GET api.steampowered.com (no user data sent) → local cache only

User saves sequence → JSON write to user-chosen .lfjson path only

User runs sequence → Process.Start per step → no data captured, no off-machine logging

User runs PowerShell step → script written to %TEMP%\lf_ps_<guid>.ps1 → deleted after run

User exports → .bat or .ps1 written to user-chosen path only
```

Nothing leaves the machine except the unauthenticated catalog request the user explicitly triggers.
