namespace LaunchForge.Models.Steps;

public class WaitForProcessExitStep : StepBase
{
    public string ProcessName    { get; set; } = string.Empty;
    public int    TimeoutSeconds { get; set; } = 0; // 0 = wait indefinitely
}
