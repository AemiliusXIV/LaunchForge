using LaunchForge.Models.Steps;
using LaunchForge.ViewModels.Steps;

namespace LaunchForge.Services;

public static class StepViewModelFactory
{
    public static StepViewModelBase FromModel(StepBase model)
    {
        StepViewModelBase vm = model switch
        {
            LaunchAppStep            => new LaunchAppStepViewModel(),
            LaunchSteamStep          => new LaunchSteamStepViewModel(),
            WaitStep                 => new WaitStepViewModel(),
            KillProcessStep          => new KillProcessStepViewModel(),
            WaitForProcessStartStep  => new WaitForProcessStartStepViewModel(),
            WaitForProcessExitStep   => new WaitForProcessExitStepViewModel(),
            RunPowerShellStep        => new RunPowerShellStepViewModel(),
            _ => throw new NotSupportedException(model.GetType().Name),
        };
        vm.FromModel(model);
        return vm;
    }

    public static StepViewModelBase CreateEmpty(string typeName) => typeName switch
    {
        "LaunchApp"            => new LaunchAppStepViewModel(),
        "LaunchSteam"          => new LaunchSteamStepViewModel(),
        "Wait"                 => new WaitStepViewModel(),
        "KillProcess"          => new KillProcessStepViewModel(),
        "WaitForProcessStart"  => new WaitForProcessStartStepViewModel(),
        "WaitForProcessExit"   => new WaitForProcessExitStepViewModel(),
        "RunPowerShell"        => new RunPowerShellStepViewModel(),
        _ => throw new ArgumentException($"Unknown step type: {typeName}", nameof(typeName)),
    };
}
