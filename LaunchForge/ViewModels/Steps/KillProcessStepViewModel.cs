using CommunityToolkit.Mvvm.ComponentModel;
using LaunchForge.Models.Steps;

namespace LaunchForge.ViewModels.Steps;

public partial class KillProcessStepViewModel : StepViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private string _processName = string.Empty;

    public override string Summary  => string.IsNullOrEmpty(ProcessName) ? "(no process)" : ProcessName;
    public override string TypeTag  => "KILL";

    public override StepBase ToModel() => new KillProcessStep
    {
        Label       = Label,
        IsEnabled   = IsEnabled,
        ProcessName = ProcessName,
    };

    public override void FromModel(StepBase model)
    {
        var m = (KillProcessStep)model;
        Label       = m.Label;
        IsEnabled   = m.IsEnabled;
        ProcessName = m.ProcessName;
    }
}
