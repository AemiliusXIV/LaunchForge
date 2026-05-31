namespace LaunchForge.Models.Steps;

public class KillProcessStep : StepBase
{
    // Process name without .exe, e.g. "chrome", "steam"
    public string ProcessName { get; set; } = string.Empty;
}
