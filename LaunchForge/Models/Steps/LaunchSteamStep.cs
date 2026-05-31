namespace LaunchForge.Models.Steps;

public class LaunchSteamStep : StepBase
{
    public string AppId    { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty; // display only
}
