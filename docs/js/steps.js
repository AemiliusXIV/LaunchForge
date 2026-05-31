// Step type definitions. The `type` value is the discriminator written to .lfjson
// as the "Type" property, kept identical to the desktop app so files interop.
//
// Each field may carry:
//   info  - hover help text shown via a "?" badge next to the label
//   hint  - short note always shown under the field
// Each type carries a `blurb` shown in the Add menu.

const STEP_TYPES = {
    LaunchApp: {
        label: "Launch App",
        tag: "APP",
        blurb: "Open a program or shortcut on your PC.",
        defaults: { exePath: "", arguments: "", waitForExit: false },
        fields: [
            { key: "exePath", type: "path", label: "Program or shortcut path",
              info: "Find the program in your Start menu or on your desktop, right-click it, choose \"Copy as path\", then paste it here. The quotes Windows adds are removed automatically. You can also type a full path like C:\\Program Files\\App\\app.exe." },
            { key: "arguments", type: "text", label: "Arguments (optional)",
              info: "Extra options passed to the program when it starts. Most launches need none, so leave this blank if you're unsure." },
            { key: "waitForExit", type: "checkbox", label: "Wait for this program to close before the next step",
              info: "Leave this off to start the program and move straight on. Turn it on only if later steps should wait until you close this program." },
        ],
        summary: s => s.exePath ? fileName(s.exePath) : "(no path set)",
    },

    LaunchSteam: {
        label: "Launch Steam Game",
        tag: "STEAM",
        blurb: "Start a Steam game by its App ID.",
        defaults: { appId: "", gameName: "" },
        fields: [
            { key: "appId", type: "steam", label: "Steam App ID",
              info: "The easiest way is to click \"Pick game\" and search by name. The App ID is also the number in a game's Steam store link, for example store.steampowered.com/app/359320/." },
            { key: "gameName", type: "text", label: "Game name (label only)",
              info: "A friendly name so you can recognise this step in the list. It has no effect on the generated script." },
        ],
        summary: s => s.gameName || (s.appId ? "App " + s.appId : "(no game set)"),
    },

    Wait: {
        label: "Wait",
        tag: "WAIT",
        blurb: "Pause for a set number of seconds.",
        defaults: { seconds: 5 },
        fields: [
            { key: "seconds", type: "number", label: "Seconds to wait", min: 1,
              info: "How long to pause before running the next step. Useful for giving a program time to finish loading." },
        ],
        summary: s => s.seconds + "s",
    },

    KillProcess: {
        label: "Kill Process",
        tag: "KILL",
        blurb: "Force-close a running program.",
        defaults: { processName: "" },
        fields: [
            { key: "processName", type: "text", label: "Process name (without .exe)",
              info: "The program's process name, without the .exe. For example \"steam\" or \"EDMarketConnector\". You can see process names in Task Manager under the Details tab.",
              hint: "e.g. 'steam', 'chrome', 'EDMarketConnector'. The .exe is added for you." },
        ],
        summary: s => s.processName || "(no process)",
    },

    WaitForProcessStart: {
        label: "Wait for Process Start",
        tag: "WAIT+",
        blurb: "Pause until a program has started.",
        defaults: { processName: "", timeoutSeconds: 60 },
        fields: [
            { key: "processName", type: "text", label: "Wait until this process is running",
              info: "The sequence pauses here until a program with this name appears. Handy when something is slow to start and a later step depends on it." },
            { key: "timeoutSeconds", type: "number", label: "Give up after (seconds)", min: 0,
              info: "Stop waiting after this many seconds even if the program never appeared. Set it to 0 to wait for as long as it takes." },
        ],
        summary: s => !s.processName ? "(no process)"
            : s.timeoutSeconds > 0 ? `${s.processName} (max ${s.timeoutSeconds}s)` : `${s.processName} (no limit)`,
    },

    WaitForProcessExit: {
        label: "Wait for Process Exit",
        tag: "WAIT-",
        blurb: "Pause until a program has closed.",
        defaults: { processName: "", timeoutSeconds: 0 },
        fields: [
            { key: "processName", type: "text", label: "Wait until this process has closed",
              info: "The sequence pauses here until the named program is no longer running. Useful for waiting on a launcher to hand off before continuing." },
            { key: "timeoutSeconds", type: "number", label: "Give up after (seconds)", min: 0,
              info: "Stop waiting after this many seconds even if the program is still running. Set it to 0 to wait for as long as it takes." },
        ],
        summary: s => !s.processName ? "(no process)"
            : s.timeoutSeconds > 0 ? `${s.processName} exits (max ${s.timeoutSeconds}s)` : `${s.processName} exits (no limit)`,
    },

    RunPowerShell: {
        label: "Run PowerShell",
        tag: "PS",
        blurb: "Advanced: run your own PowerShell.",
        defaults: { script: "", waitForExit: true },
        fields: [
            { key: "script", type: "textarea", label: "PowerShell script", rows: 8,
              info: "Advanced. Any PowerShell commands to run at this point in the sequence. If you're not familiar with PowerShell, you can skip this step type entirely." },
            { key: "waitForExit", type: "checkbox", label: "Wait for the script to finish before the next step",
              info: "Leave on to let the script complete before moving on. Turn off to start it in the background and continue immediately." },
        ],
        summary: s => {
            if (!s.script) return "(empty script)";
            const first = s.script.split("\n").find(l => l.trim());
            return first ? first.trim() : "(script)";
        },
    },
};

// Order shown in the Add menu
const STEP_ORDER = [
    "LaunchApp", "LaunchSteam", "Wait", "KillProcess",
    "WaitForProcessStart", "WaitForProcessExit", "RunPowerShell",
];

function fileName(p) {
    if (!p) return "";
    const parts = p.split(/[\\/]/);
    return parts[parts.length - 1] || p;
}

// Create a fresh step object with the discriminator + shared fields + type defaults
function makeStep(type) {
    return Object.assign(
        { Type: type, label: "", isEnabled: true },
        structuredClone(STEP_TYPES[type].defaults)
    );
}
