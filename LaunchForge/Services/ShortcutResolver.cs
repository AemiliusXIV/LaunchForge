using System.IO;
using System.Text.RegularExpressions;

namespace LaunchForge.Services;

public static class ShortcutResolver
{
    private static readonly string UserDesktop =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    private static readonly string PublicDesktop =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

    // Resolve a .lnk file via WScript.Shell COM (read-only, no execution).
    public static (string TargetPath, string Arguments)? TryResolveLnk(string lnkPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return null;
            dynamic shell    = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(lnkPath);
            string target    = shortcut.TargetPath;
            string args      = shortcut.Arguments;
            return (target, args);
        }
        catch
        {
            return null;
        }
    }

    // Try to extract a Steam App ID from a .url or .lnk file.
    // Returns null if the file is not a Steam shortcut.
    public static string? TryGetSteamAppId(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (ext == ".url")
        {
            return ParseSteamUrl(path);
        }

        if (ext == ".lnk")
        {
            var resolved = TryResolveLnk(path);
            if (resolved is null) return null;

            // Steam.exe with -applaunch <id>
            if (resolved.Value.TargetPath.EndsWith("steam.exe", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(resolved.Value.Arguments, @"-applaunch\s+(\d+)",
                    RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;
            }

            // Sometimes the target itself is steam://rungameid/<id> (older shortcuts)
            var urlMatch = Regex.Match(resolved.Value.TargetPath,
                @"steam://rungameid/(\d+)", RegexOptions.IgnoreCase);
            if (urlMatch.Success) return urlMatch.Groups[1].Value;
        }

        return null;
    }

    // Returns true if the given path (or its parent folder) is the user or public Desktop.
    public static bool IsDesktopPath(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        return dir.Equals(UserDesktop,    StringComparison.OrdinalIgnoreCase)
            || dir.Equals(PublicDesktop,  StringComparison.OrdinalIgnoreCase);
    }

    // ----

    private static string? ParseSteamUrl(string urlPath)
    {
        try
        {
            foreach (var line in File.ReadLines(urlPath))
            {
                if (!line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase)) continue;
                var url = line[4..];
                var m   = Regex.Match(url, @"steam://rungameid/(\d+)", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;
            }
        }
        catch { /* file unreadable */ }

        return null;
    }
}
