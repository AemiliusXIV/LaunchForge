using CommunityToolkit.Mvvm.ComponentModel;
using LaunchForge.Models.Steps;

namespace LaunchForge.ViewModels.Steps;

public partial class RunPowerShellStepViewModel : StepViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private string _script = string.Empty;

    [ObservableProperty] private bool _waitForExit = true;

    public override string Summary =>
        string.IsNullOrEmpty(Script) ? "(empty script)" :
        Script.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "(script)";

    public override string TypeTag => "PS1";

    public override StepBase ToModel() => new RunPowerShellStep
    {
        Label       = Label,
        IsEnabled   = IsEnabled,
        Script      = Script,
        WaitForExit = WaitForExit,
    };

    public override void FromModel(StepBase model)
    {
        var m = (RunPowerShellStep)model;
        Label       = m.Label;
        IsEnabled   = m.IsEnabled;
        Script      = m.Script;
        WaitForExit = m.WaitForExit;
    }
}
