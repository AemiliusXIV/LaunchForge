namespace LaunchForge.Models.Steps;

public class RunPowerShellStep : StepBase
{
    public string Script      { get; set; } = string.Empty;
    public bool   WaitForExit { get; set; } = true;
}
