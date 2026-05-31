using LaunchForge.Models;
using Microsoft.Win32;

namespace LaunchForge.Services;

public class SteamRegistryService
{
    private const string AppsKeyPath = @"Software\Valve\Steam\Apps";

    // Reads installed Steam games from HKCU (read-only, no credentials or account data).
    public IReadOnlyList<SteamGame> GetInstalledGames()
    {
        var games = new List<SteamGame>();

        try
        {
            using var appsKey = Registry.CurrentUser.OpenSubKey(AppsKeyPath);
            if (appsKey is null) return games;

            foreach (var subKeyName in appsKey.GetSubKeyNames())
            {
                using var gameKey = appsKey.OpenSubKey(subKeyName);
                if (gameKey is null) continue;

                var installed = gameKey.GetValue("Installed") as int? ?? 0;
                if (installed == 0) continue;

                var name = gameKey.GetValue("Name") as string ?? $"App {subKeyName}";
                games.Add(new SteamGame
                {
                    AppId       = subKeyName,
                    Name        = name,
                    IsInstalled = true,
                });
            }
        }
        catch { /* registry unavailable, return empty */ }

        return [.. games.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
