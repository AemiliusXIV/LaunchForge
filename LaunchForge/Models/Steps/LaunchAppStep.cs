namespace LaunchForge.Models.Steps;

public class LaunchAppStep : StepBase
{
    public string ExePath     { get; set; } = string.Empty;
    public string Arguments   { get; set; } = string.Empty;
    public bool   WaitForExit { get; set; } = false;
}
