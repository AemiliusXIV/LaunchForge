// Regenerate docs/data/steam-apps.json from Valve's catalog.
//
//   STEAM_API_KEY=xxxx node docs/tools/fetch-steam-apps.mjs
//
// Uses IStoreService/GetAppList/v1 (the old ISteamApps/GetAppList/v2 was retired
// in Nov 2025). That endpoint needs a free Steam Web API key and pages 50k apps
// at a time, so we walk the last_appid cursor until the catalog is exhausted.
// Games only; DLC, tools, videos, and hardware are filtered out for a cleaner
// search list.
//
// Get a key at https://steamcommunity.com/dev/apikey. In CI it comes from the
// STEAM_API_KEY repo secret.

import { writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const KEY = process.env.STEAM_API_KEY;
if (!KEY) {
    console.error("STEAM_API_KEY is not set. Get one at https://steamcommunity.com/dev/apikey");
    process.exit(1);
}

const OUT = join(dirname(fileURLToPath(import.meta.url)), "..", "data", "steam-apps.json");
const PAGE = 50000;

const seen = new Set();
const apps = [];
let cursor = 0;
let page = 0;

while (true) {
    const url = new URL("https://api.steampowered.com/IStoreService/GetAppList/v1/");
    url.searchParams.set("key", KEY);
    url.searchParams.set("include_games", "true");
    url.searchParams.set("include_dlc", "false");
    url.searchParams.set("include_software", "false");
    url.searchParams.set("include_videos", "false");
    url.searchParams.set("include_hardware", "false");
    url.searchParams.set("max_results", String(PAGE));
    if (cursor) url.searchParams.set("last_appid", String(cursor));

    const res = await fetch(url, { headers: { "User-Agent": "LaunchForge-catalog/1.0" } });
    if (!res.ok) {
        console.error(`Fetch failed on page ${page}: ${res.status} ${res.statusText}`);
        process.exit(1);
    }

    const body = (await res.json()).response || {};
    const batch = body.apps || [];
    for (const a of batch) {
        const name = (a.name || "").trim();
        if (!name || seen.has(a.appid)) continue;
        seen.add(a.appid);
        apps.push({ appid: a.appid, name });
    }

    page++;
    if (!body.have_more_results) break;
    cursor = body.last_appid;
}

apps.sort((x, y) => x.name.localeCompare(y.name));
await writeFile(OUT, JSON.stringify({ apps }, null, 0));
console.log(`Wrote ${apps.length} games to ${OUT} (${page} page${page === 1 ? "" : "s"})`);
