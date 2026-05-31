using CommunityToolkit.Mvvm.ComponentModel;
using LaunchForge.Models.Steps;

namespace LaunchForge.ViewModels.Steps;

public partial class WaitStepViewModel : StepViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private int _seconds = 5;

    public override string Summary  => $"{Seconds}s";
    public override string TypeTag  => "WAIT";

    public override StepBase ToModel() => new WaitStep
    {
        Label     = Label,
        IsEnabled = IsEnabled,
        Seconds   = Seconds,
    };

    public override void FromModel(StepBase model)
    {
        var m = (WaitStep)model;
        Label     = m.Label;
        IsEnabled = m.IsEnabled;
        Seconds   = m.Seconds;
    }
}
