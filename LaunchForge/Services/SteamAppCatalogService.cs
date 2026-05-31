using System.IO;
using System.Net.Http;
using System.Text.Json;
using LaunchForge.Models;

namespace LaunchForge.Services;

// Fetches and caches the full Steam app catalog from Valve's public API.
// The only outbound network call LaunchForge makes; always user-initiated via RefreshAsync.
public class SteamAppCatalogService
{
    private static readonly string CacheDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "LaunchForge");

    private static readonly string CachePath =
        Path.Combine(CacheDir, "steam_app_cache.json");

    private static readonly TimeSpan StaleAfter = TimeSpan.FromDays(7);

    private readonly HttpClient _http;
    private readonly Dictionary<string, string> _nameById = new();
    private List<SteamGame> _catalog = [];

    public SteamAppCatalogService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.steampowered.com/"),
            Timeout     = TimeSpan.FromSeconds(30),
        };
        _http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
    }

    public IReadOnlyList<SteamGame> GetCachedApps() => _catalog;

    public string? GetNameByAppId(string appId) =>
        _nameById.TryGetValue(appId, out var name) ? name : null;

    public DateTime? CachedAt    { get; private set; }
    public bool      IsCacheStale =>
        CachedAt is null || (DateTime.UtcNow - CachedAt.Value) > StaleAfter;

    // Called on app start; loads the local cache into memory silently.
    public async Task LoadCacheAsync()
    {
        if (!File.Exists(CachePath)) return;
        try
        {
            var json   = await File.ReadAllTextAsync(CachePath);
            var stored = JsonSerializer.Deserialize<StoredCache>(json);
            if (stored is null) return;

            CachedAt = stored.FetchedAt;
            _catalog  = stored.Apps.Select(a => new SteamGame
            {
                AppId = a.AppId,
                Name  = a.Name,
            }).ToList();

            RebuildIndex();
        }
        catch { /* corrupt cache; ignore, will refetch on user request */ }
    }

    // Fetches fresh data from api.steampowered.com and persists it.
    // Only called when the user explicitly clicks Refresh.
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            "ISteamApps/GetAppList/v2/", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc  = JsonDocument.Parse(json);
        var       apps = doc.RootElement
            .GetProperty("applist")
            .GetProperty("apps")
            .EnumerateArray()
            .Select(el => new CatalogEntry(
                el.GetProperty("appid").GetInt64().ToString(),
                el.GetProperty("name").GetString() ?? string.Empty))
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .ToList();

        _catalog  = apps.Select(a => new SteamGame { AppId = a.AppId, Name = a.Name }).ToList();
        CachedAt  = DateTime.UtcNow;
        RebuildIndex();

        // Persist to disk
        Directory.CreateDirectory(CacheDir);
        var stored  = new StoredCache(CachedAt.Value, apps);
        var cacheJson = JsonSerializer.Serialize(stored, new JsonSerializerOptions
        {
            WriteIndented = false // keep file compact; it's 10-20 MB uncompressed
        });
        await File.WriteAllTextAsync(CachePath, cacheJson, ct);
    }

    private void RebuildIndex()
    {
        _nameById.Clear();
        foreach (var g in _catalog)
            _nameById[g.AppId] = g.Name;
    }

    // ---- Serialization types ----

    private record CatalogEntry(string AppId, string Name);

    private record StoredCache(DateTime FetchedAt, List<CatalogEntry> Apps);
}
