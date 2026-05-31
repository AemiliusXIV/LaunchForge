using System.Text.Json.Serialization;

namespace LaunchForge.Models.Steps;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(LaunchAppStep),             "LaunchApp")]
[JsonDerivedType(typeof(LaunchSteamStep),           "LaunchSteam")]
[JsonDerivedType(typeof(WaitStep),                  "Wait")]
[JsonDerivedType(typeof(KillProcessStep),           "KillProcess")]
[JsonDerivedType(typeof(WaitForProcessStartStep),   "WaitForProcessStart")]
[JsonDerivedType(typeof(WaitForProcessExitStep),    "WaitForProcessExit")]
[JsonDerivedType(typeof(RunPowerShellStep),         "RunPowerShell")]
public abstract class StepBase
{
    public string Label     { get; set; } = string.Empty;
    public bool   IsEnabled { get; set; } = true;
}
