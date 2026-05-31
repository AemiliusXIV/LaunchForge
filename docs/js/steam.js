// Steam game picker. Loads a bundled app list (data/steam-apps.json) lazily the
// first time the picker opens. No server, no live API call; the browser can't
// reach the Steam Web API directly (no CORS), so we ship a static list and let
// the user fall back to typing an App ID.

// Small embedded fallback so the picker works even when fetch() is blocked
// (e.g. opening index.html directly via file://). Replaced/extended by the
// bundled data/steam-apps.json when it loads.
const STEAM_SEED = [
    { appId: "359320",  name: "Elite Dangerous" },
    { appId: "250820",  name: "SteamVR" },
    { appId: "227300",  name: "Euro Truck Simulator 2" },
    { appId: "1465360", name: "SnowRunner" },
    { appId: "1174180", name: "Red Dead Redemption 2" },
    { appId: "570",     name: "Dota 2" },
    { appId: "730",     name: "Counter-Strike 2" },
    { appId: "440",     name: "Team Fortress 2" },
];

const Steam = (() => {
    let apps = null;        // [{ appId, name }]
    let loadPromise = null;
    let onPick = null;      // callback(appId, name)

    const modal   = () => document.getElementById("steamModal");
    const listEl  = () => document.getElementById("steamList");
    const filterEl= () => document.getElementById("steamFilter");
    const statusEl= () => document.getElementById("steamStatus");
    const manualEl= () => document.getElementById("steamManual");

    function load() {
        if (loadPromise) return loadPromise;
        loadPromise = fetch("data/steam-apps.json")
            .then(r => r.ok ? r.json() : Promise.reject(r.status))
            .then(data => {
                apps = (data.apps || []).map(a => ({ appId: String(a.appid ?? a.appId), name: a.name }));
                apps.sort((a, b) => a.name.localeCompare(b.name));
            })
            .catch(() => { apps = STEAM_SEED.slice().sort((a, b) => a.name.localeCompare(b.name)); });
        return loadPromise;
    }

    function render(filter) {
        const ul = listEl();
        ul.innerHTML = "";
        const f = (filter || "").toLowerCase();
        const matches = (apps || [])
            .filter(a => !f || a.name.toLowerCase().includes(f) || a.appId.includes(f))
            .slice(0, 300); // cap render for responsiveness

        for (const a of matches) {
            const li = document.createElement("li");
            li.innerHTML = `<span>${escapeHtml(a.name)}</span><span class="appid">${a.appId}</span>`;
            li.addEventListener("click", () => pick(a.appId, a.name));
            ul.appendChild(li);
        }

        const total = (apps || []).length;
        statusEl().textContent = total
            ? `${matches.length} of ${total} games shown.`
            : "No bundled game list found. Type an App ID below.";
    }

    function pick(appId, name) {
        close();
        if (onPick) onPick(appId, name);
    }

    function open(callback) {
        onPick = callback;
        modal().classList.remove("hidden");
        filterEl().value = "";
        manualEl().value = "";
        statusEl().textContent = "Loading game list…";
        load().then(() => render(""));
        filterEl().focus();
    }

    function close() { modal().classList.add("hidden"); }

    function init() {
        filterEl().addEventListener("input", e => render(e.target.value));
        document.getElementById("steamClose").addEventListener("click", close);
        modal().addEventListener("click", e => { if (e.target === modal()) close(); });
        document.getElementById("steamUseManual").addEventListener("click", () => {
            const id = manualEl().value.trim();
            if (id) pick(id, "App " + id);
        });
        manualEl().addEventListener("keydown", e => {
            if (e.key === "Enter") { const id = manualEl().value.trim(); if (id) pick(id, "App " + id); }
        });
    }

    // Look up a name for a known App ID (used by the .lnk → Steam convert flow)
    function nameForAppId(appId) {
        if (!apps) return null;
        const hit = apps.find(a => a.appId === String(appId));
        return hit ? hit.name : null;
    }

    return { init, open, load, nameForAppId };
})();

function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, c =>
        ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}
