using CommunityToolkit.Mvvm.ComponentModel;
using LaunchForge.Models.Steps;

namespace LaunchForge.ViewModels.Steps;

public partial class LaunchSteamStepViewModel : StepViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private string _appId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private string _gameName = string.Empty;

    public override string Summary  =>
        !string.IsNullOrEmpty(GameName) ? GameName :
        !string.IsNullOrEmpty(AppId)    ? $"App {AppId}" : "(no game set)";

    public override string TypeTag => "STEAM";

    public override StepBase ToModel() => new LaunchSteamStep
    {
        Label    = Label,
        IsEnabled = IsEnabled,
        AppId    = AppId,
        GameName = GameName,
    };

    public override void FromModel(StepBase model)
    {
        var m = (LaunchSteamStep)model;
        Label    = m.Label;
        IsEnabled = m.IsEnabled;
        AppId    = m.AppId;
        GameName = m.GameName;
    }
}
