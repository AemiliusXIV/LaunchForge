namespace LaunchForge.Models.Steps;

public class WaitForProcessStartStep : StepBase
{
    public string ProcessName    { get; set; } = string.Empty;
    public int    TimeoutSeconds { get; set; } = 60; // 0 = wait indefinitely
}
