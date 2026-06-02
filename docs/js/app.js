// LaunchForge web: state + UI wiring. Vanilla JS, no build step.

const state = {
    name: "Untitled Sequence",
    steps: [],          // array of step objects (see steps.js)
    selectedIndex: -1,
    previewFmt: "bat",
};

// ── Element refs ──
const el = {
    seqName:    document.getElementById("seqName"),
    stepList:   document.getElementById("stepList"),
    editor:     document.getElementById("editor"),
    preview:    document.getElementById("preview"),
    addMenu:    document.getElementById("addMenu"),
    fileInput:  document.getElementById("fileInput"),
};

// ── Rendering ──

function renderStepList() {
    el.stepList.innerHTML = "";

    if (!state.steps.length) {
        const li = document.createElement("li");
        li.className = "empty-state";
        li.innerHTML =
            `<div class="es-title">No steps yet</div>` +
            `Click <strong>+ Add</strong> above to add your first step.<br><br>` +
            `New to this? <a id="esHelp">Open the guide</a> or <a id="esExample">load an example</a>.`;
        el.stepList.appendChild(li);
        li.querySelector("#esHelp").addEventListener("click", openHelp);
        li.querySelector("#esExample").addEventListener("click", loadExample);
        return;
    }

    state.steps.forEach((step, i) => {
        const def = STEP_TYPES[step.Type];
        const li = document.createElement("li");
        li.className = "step-item"
            + (i === state.selectedIndex ? " selected" : "")
            + (step.isEnabled ? "" : " disabled");
        li.innerHTML =
            `<span class="step-num">${i + 1}</span>` +
            `<span class="step-tag">${def.tag}</span>` +
            `<span class="step-summary">${escapeHtml(def.summary(step))}</span>`;
        li.addEventListener("click", () => selectStep(i));
        el.stepList.appendChild(li);
    });
}

function renderEditor() {
    const step = state.steps[state.selectedIndex];
    if (!step) {
        el.editor.innerHTML = `<p class="placeholder">Pick a step on the left to edit it, or click <strong>+ Add</strong> to create one.<br><br>Hover the <span class="help-badge">?</span> icons anywhere for help.</p>`;
        return;
    }
    const def = STEP_TYPES[step.Type];
    const frag = document.createElement("div");

    // Steam convert warning placeholder (for LaunchApp shortcut detection)
    if (step.Type === "LaunchApp") {
        frag.appendChild(buildShortcutWarning(step));
    }

    for (const field of def.fields) {
        frag.appendChild(buildField(step, field));
    }

    // Shared: label + enabled
    frag.appendChild(buildField(step, { key: "label", type: "text", label: "Label (optional note)" }));
    frag.appendChild(buildField(step, { key: "isEnabled", type: "checkbox", label: "Step enabled" }));

    el.editor.innerHTML = "";
    el.editor.appendChild(frag);
}

function buildField(step, field) {
    const wrap = document.createElement("div");
    wrap.className = "field" + (field.type === "checkbox" ? " checkbox" : "");

    if (field.type === "checkbox") {
        const id = "f_" + field.key;
        wrap.innerHTML = `<label><input type="checkbox" id="${id}" ${step[field.key] ? "checked" : ""}> ${escapeHtml(field.label)}</label>`;
        if (field.info) wrap.querySelector("label").appendChild(helpBadge(field.info));
        wrap.querySelector("input").addEventListener("change", e => {
            step[field.key] = e.target.checked;
            onStepEdited();
        });
        return wrap;
    }

    const labelEl = document.createElement("label");
    labelEl.className = "fld-label";
    labelEl.textContent = field.label;
    if (field.info) labelEl.appendChild(helpBadge(field.info));
    wrap.appendChild(labelEl);

    if (field.type === "textarea") {
        const ta = document.createElement("textarea");
        ta.rows = field.rows || 6;
        ta.value = step[field.key] || "";
        ta.addEventListener("input", e => { step[field.key] = e.target.value; onStepEdited(); });
        wrap.appendChild(ta);
    }
    else if (field.type === "steam") {
        const row = document.createElement("div");
        row.className = "row";
        const input = document.createElement("input");
        input.type = "text";
        input.value = step[field.key] || "";
        input.addEventListener("input", e => { step[field.key] = e.target.value; onStepEdited(); });
        const btn = document.createElement("button");
        btn.className = "btn small";
        btn.textContent = "Pick game…";
        btn.addEventListener("click", () => {
            Steam.open((appId, name) => {
                step.appId = appId;
                if (!step.gameName) step.gameName = name;
                renderEditor();
                onStepEdited();
            });
        });
        row.appendChild(input);
        row.appendChild(btn);
        wrap.appendChild(row);
    }
    else if (field.type === "path") {
        const input = document.createElement("input");
        input.type = "text";
        input.value = step[field.key] || "";
        input.addEventListener("input", e => { step[field.key] = e.target.value; onStepEdited(); });
        // Strip surrounding quotes from "Copy as path" on paste
        input.addEventListener("paste", e => {
            const text = (e.clipboardData || window.clipboardData).getData("text");
            if (text.length > 1 && text.startsWith('"') && text.endsWith('"')) {
                e.preventDefault();
                const cleaned = text.slice(1, -1);
                step[field.key] = cleaned;
                input.value = cleaned;
                onStepEdited();
                analyseShortcut(step);
            }
        });
        wrap.appendChild(input);
    }
    else { // text / number
        const input = document.createElement("input");
        input.type = field.type === "number" ? "number" : "text";
        if (field.min !== undefined) input.min = field.min;
        input.value = step[field.key] ?? "";
        input.addEventListener("input", e => {
            step[field.key] = field.type === "number" ? Number(e.target.value) : e.target.value;
            onStepEdited();
        });
        wrap.appendChild(input);
    }

    if (field.hint) {
        const hint = document.createElement("div");
        hint.className = "hint";
        hint.textContent = field.hint;
        wrap.appendChild(hint);
    }
    return wrap;
}

// ── Shortcut analysis (web-limited: filename heuristics only) ──
// A browser can't resolve .lnk targets or read the registry, so we detect the
// obvious cases: a .url Steam shortcut name, or a desktop path string.

function buildShortcutWarning(step) {
    const box = document.createElement("div");
    const path = step.exePath || "";
    const isDesktop = /[\\/]Desktop[\\/]/i.test(path);
    if (!isDesktop && !step._steamAppId) { box.className = "hidden"; return box; }

    box.className = "warn";
    let html = "";
    if (isDesktop) {
        html += "⚠ This looks like a desktop shortcut. If the shortcut is removed the script will break.";
    }
    if (step._steamAppId) {
        html += `<div>The target appears to be a Steam game (App ID ${step._steamAppId}). A Launch Steam step is more reliable.</div>`;
        html += `<button class="btn small convert">Convert to Launch Steam</button>`;
    }
    box.innerHTML = html;
    const convertBtn = box.querySelector(".convert");
    if (convertBtn) {
        convertBtn.addEventListener("click", () => convertToSteam(step));
    }
    return box;
}

function analyseShortcut(step) {
    // .url files are plain text we can't read from a path string alone in-browser,
    // but the filename / path can still carry a rungameid in some exports.
    const m = (step.exePath || "").match(/rungameid[\/=](\d+)/i);
    step._steamAppId = m ? m[1] : null;
    renderEditor();
}

function convertToSteam(appStep) {
    const idx = state.steps.indexOf(appStep);
    if (idx < 0) return;
    const appId = appStep._steamAppId;
    const newStep = makeStep("LaunchSteam");
    newStep.appId = appId;
    newStep.gameName = Steam.nameForAppId(appId) || ("App " + appId);
    newStep.label = appStep.label;
    state.steps[idx] = newStep;
    renderAll();
}

// ── Preview ──

function currentSequence() {
    return { name: state.name, steps: state.steps };
}

function renderPreview() {
    const seq = currentSequence();
    el.preview.textContent = state.previewFmt === "bat"
        ? generateBat(seq)
        : generatePs1(seq);
}

function renderAll() {
    el.seqName.value = state.name;
    renderStepList();
    renderEditor();
    renderPreview();
}

function onStepEdited() {
    renderStepList();
    renderPreview();
}

// ── Step operations ──

function selectStep(i) {
    state.selectedIndex = i;
    renderStepList();
    renderEditor();
}

function addStep(type) {
    state.steps.push(makeStep(type));
    state.selectedIndex = state.steps.length - 1;
    renderAll();
}

function removeStep() {
    if (state.selectedIndex < 0) return;
    state.steps.splice(state.selectedIndex, 1);
    state.selectedIndex = Math.min(state.selectedIndex, state.steps.length - 1);
    renderAll();
}

function moveStep(delta) {
    const i = state.selectedIndex;
    const j = i + delta;
    if (i < 0 || j < 0 || j >= state.steps.length) return;
    [state.steps[i], state.steps[j]] = [state.steps[j], state.steps[i]];
    state.selectedIndex = j;
    renderAll();
}

// ── File operations ──

function download(filename, text) {
    const blob = new Blob([text], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
}

function safeName() {
    return (state.name || "sequence").replace(/[^\w.\- ]+/g, "_").trim() || "sequence";
}

function exportBat() {
    if (!state.steps.length) { toast("Add at least one step before exporting."); return; }
    const name = safeName() + ".bat";
    download(name, generateBat(currentSequence()));
    toast(`Downloaded ${name}. Find it in your Downloads folder and double-click to run.`);
}
function exportPs1() {
    if (!state.steps.length) { toast("Add at least one step before exporting."); return; }
    const name = safeName() + ".ps1";
    download(name, generatePs1(currentSequence()));
    toast(`Downloaded ${name}. Right-click it and choose "Run with PowerShell".`);
}

function saveProject() {
    const now = new Date().toISOString();
    const obj = {
        name: state.name,
        steps: state.steps.map(stripPrivate),
        createdAt: now,
        modifiedAt: now,
    };
    download(safeName() + ".lfjson", JSON.stringify(obj, null, 2));
}

// Drop UI-only fields (prefixed _) before serializing
function stripPrivate(step) {
    const copy = {};
    for (const k of Object.keys(step)) if (!k.startsWith("_")) copy[k] = step[k];
    return copy;
}

function importProject(file) {
    const reader = new FileReader();
    reader.onload = () => {
        const res = Parser.importAny(file.name, String(reader.result));

        if (!res.steps.length) {
            alert("Couldn't import any steps from this file.\n\n" + res.warnings.join("\n"));
            return;
        }

        state.name = res.name;
        state.steps = res.steps;
        state.selectedIndex = 0;
        renderAll();
        resolveSteamNames();

        if (res.warnings.length) {
            const shown = res.warnings.slice(0, 12).join("\n");
            const more = res.warnings.length > 12 ? `\n…and ${res.warnings.length - 12} more` : "";
            alert(`Imported ${res.steps.length} step(s) from a .${res.format} file.\n\n` +
                  `${res.warnings.length} line(s) couldn't be read and were skipped:\n${shown}${more}`);
        }
    };
    reader.readAsText(file);
}

// After an import, the catalog usually isn't loaded yet, so Launch Steam steps
// come in with just an App ID. Load it (once) and backfill the game names.
function resolveSteamNames() {
    const pending = state.steps.filter(s => s.Type === "LaunchSteam" && s.appId && !s.gameName);
    if (!pending.length) return;
    Steam.load().then(() => {
        let changed = false;
        for (const s of pending) {
            const name = Steam.nameForAppId(s.appId);
            if (name) { s.gameName = name; changed = true; }
        }
        if (changed) renderAll();
    });
}

function newSequence() {
    if (state.steps.length && !confirm("Discard the current sequence?")) return;
    state.name = "Untitled Sequence";
    state.steps = [];
    state.selectedIndex = -1;
    renderAll();
}

// ── Add menu ──

function buildAddMenu() {
    el.addMenu.innerHTML = "";
    for (const type of STEP_ORDER) {
        const def = STEP_TYPES[type];
        const b = document.createElement("button");
        b.innerHTML = `<span class="mi-name">${escapeHtml(def.label)}</span>` +
                      `<span class="mi-blurb">${escapeHtml(def.blurb)}</span>`;
        b.addEventListener("click", () => { addStep(type); el.addMenu.classList.add("hidden"); });
        el.addMenu.appendChild(b);
    }
}

// ── Help badges + floating tooltip ──

function helpBadge(text) {
    const b = document.createElement("span");
    b.className = "help-badge";
    b.tabIndex = 0;
    b.textContent = "?";
    b.setAttribute("data-tip", text);
    return b;
}

const Tooltip = (() => {
    let tip;
    const ensure = () => {
        if (!tip) { tip = document.createElement("div"); tip.className = "tooltip hidden"; document.body.appendChild(tip); }
        return tip;
    };
    function show(target) {
        const text = target.getAttribute("data-tip");
        if (!text) return;
        const t = ensure();
        t.textContent = text;
        t.classList.remove("hidden");
        const r = target.getBoundingClientRect();
        let left = r.left + r.width / 2 - t.offsetWidth / 2;
        left = Math.max(8, Math.min(left, window.innerWidth - t.offsetWidth - 8));
        let top = r.bottom + 8;
        if (top + t.offsetHeight > window.innerHeight - 8) top = r.top - t.offsetHeight - 8;
        t.style.left = left + "px";
        t.style.top = top + "px";
    }
    const hide = () => tip && tip.classList.add("hidden");
    function init() {
        document.addEventListener("mouseover", e => { const t = e.target.closest("[data-tip]"); if (t) show(t); });
        document.addEventListener("mouseout",  e => { const t = e.target.closest("[data-tip]"); if (t) hide(); });
        document.addEventListener("focusin",   e => { const t = e.target.closest("[data-tip]"); if (t) show(t); });
        document.addEventListener("focusout", hide);
        window.addEventListener("scroll", hide, true);
    }
    return { init };
})();

// ── Toast ──

function toast(message, ms = 5000) {
    const host = document.getElementById("toastHost");
    const t = document.createElement("div");
    t.className = "toast";
    t.textContent = message;
    host.appendChild(t);
    requestAnimationFrame(() => t.classList.add("show"));
    setTimeout(() => { t.classList.remove("show"); setTimeout(() => t.remove(), 250); }, ms);
}

// ── Help modal + example ──

function openHelp()  { document.getElementById("helpModal").classList.remove("hidden"); }
function closeHelp() { document.getElementById("helpModal").classList.add("hidden"); }

function loadExample() {
    closeHelp();
    state.name = "Elite Dangerous";
    state.steps = [
        Object.assign(makeStep("LaunchApp"),   { exePath: "C:\\Users\\Public\\Desktop\\EDMarketConnector.lnk", label: "Market connector" }),
        Object.assign(makeStep("LaunchSteam"), { appId: "250820", gameName: "SteamVR" }),
        Object.assign(makeStep("Wait"),        { seconds: 20, label: "Let SteamVR settle" }),
        Object.assign(makeStep("LaunchSteam"), { appId: "359320", gameName: "Elite Dangerous" }),
    ];
    state.selectedIndex = 0;
    renderAll();
    toast("Loaded an example. Edit any step, or export it to try it out.");
}

// ── Wire up ──

function init() {
    buildAddMenu();
    Steam.init();
    Tooltip.init();

    el.seqName.addEventListener("input", e => { state.name = e.target.value; renderPreview(); });

    document.getElementById("btnAdd").addEventListener("click", e => {
        e.stopPropagation();
        el.addMenu.classList.toggle("hidden");
    });
    document.addEventListener("click", () => el.addMenu.classList.add("hidden"));

    document.getElementById("btnUp").addEventListener("click", () => moveStep(-1));
    document.getElementById("btnDown").addEventListener("click", () => moveStep(1));
    document.getElementById("btnRemove").addEventListener("click", removeStep);

    document.getElementById("btnExportBat").addEventListener("click", exportBat);
    document.getElementById("btnExportPs1").addEventListener("click", exportPs1);
    document.getElementById("btnSaveProj").addEventListener("click", saveProject);
    document.getElementById("btnNew").addEventListener("click", newSequence);

    document.getElementById("btnImport").addEventListener("click", () => el.fileInput.click());
    el.fileInput.addEventListener("change", e => {
        if (e.target.files[0]) importProject(e.target.files[0]);
        e.target.value = "";
    });

    document.getElementById("btnHelp").addEventListener("click", openHelp);
    document.getElementById("helpClose").addEventListener("click", closeHelp);
    document.getElementById("helpModal").addEventListener("click", e => {
        if (e.target.id === "helpModal") closeHelp();
    });
    document.getElementById("helpLoadExample").addEventListener("click", loadExample);
    document.addEventListener("keydown", e => { if (e.key === "Escape") { closeHelp(); Steam && document.getElementById("steamModal").classList.add("hidden"); } });

    for (const tab of document.querySelectorAll(".tab")) {
        tab.addEventListener("click", () => {
            document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));
            tab.classList.add("active");
            state.previewFmt = tab.dataset.fmt;
            renderPreview();
        });
    }

    renderAll();

    // Show the guide automatically on a first visit.
    try {
        if (!localStorage.getItem("lf_help_seen")) {
            openHelp();
            localStorage.setItem("lf_help_seen", "1");
        }
    } catch { /* storage blocked; just skip the auto-open */ }
}

document.addEventListener("DOMContentLoaded", init);
