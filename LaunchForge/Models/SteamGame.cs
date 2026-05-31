namespace LaunchForge.Models;

public class SteamGame
{
    public string AppId       { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public bool   IsInstalled { get; set; }
}
