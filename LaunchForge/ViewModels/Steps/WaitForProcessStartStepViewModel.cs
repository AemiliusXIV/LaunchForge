using CommunityToolkit.Mvvm.ComponentModel;
using LaunchForge.Models.Steps;

namespace LaunchForge.ViewModels.Steps;

public partial class WaitForProcessStartStepViewModel : StepViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private string _processName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private int _timeoutSeconds = 60;

    public override string Summary => string.IsNullOrEmpty(ProcessName)
        ? "(no process)"
        : TimeoutSeconds > 0 ? $"{ProcessName} (max {TimeoutSeconds}s)" : $"{ProcessName} (∞)";

    public override string TypeTag => "WAIT↑";

    public override StepBase ToModel() => new WaitForProcessStartStep
    {
        Label          = Label,
        IsEnabled      = IsEnabled,
        ProcessName    = ProcessName,
        TimeoutSeconds = TimeoutSeconds,
    };

    public override void FromModel(StepBase model)
    {
        var m = (WaitForProcessStartStep)model;
        Label          = m.Label;
        IsEnabled      = m.IsEnabled;
        ProcessName    = m.ProcessName;
        TimeoutSeconds = m.TimeoutSeconds;
    }
}
