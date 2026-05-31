using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LaunchForge.Messages;
using LaunchForge.Models.Steps;
using LaunchForge.Services;

namespace LaunchForge.ViewModels.Steps;

public partial class LaunchAppStepViewModel : StepViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyPropertyChangedFor(nameof(IsDesktopWarning))]
    [NotifyPropertyChangedFor(nameof(IsSteamConvertSuggestion))]
    [NotifyPropertyChangedFor(nameof(SuggestedSteamAppId))]
    private string _exePath = string.Empty;

    [ObservableProperty] private string _arguments   = string.Empty;
    [ObservableProperty] private bool   _waitForExit = false;

    // Set by the ShortcutResolver analysis triggered from OnExePathChanged
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSteamConvertSuggestion))]
    private string? _suggestedSteamAppId;

    [ObservableProperty] private string? _suggestedGameName;

    public bool IsDesktopWarning       => !string.IsNullOrEmpty(ExePath) && ShortcutResolver.IsDesktopPath(ExePath);
    public bool IsSteamConvertSuggestion => SuggestedSteamAppId is not null;

    public override string  Summary => string.IsNullOrEmpty(ExePath)
        ? "(no path set)"
        : Path.GetFileName(ExePath);

    public override string TypeTag => "APP";

    partial void OnExePathChanged(string value)
    {
        // Analyse the new path asynchronously so the UI stays responsive
        Task.Run(() => AnalysePath(value));
    }

    private void AnalysePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            SuggestedSteamAppId = null;
            SuggestedGameName   = null;
            return;
        }

        var appId = ShortcutResolver.TryGetSteamAppId(path);
        SuggestedSteamAppId = appId;
        // Game name lookup happens via the catalog; the VM that hosts this (MainViewModel)
        // injects it after conversion. For the suggestion chip we only need the AppId.
        SuggestedGameName = appId is not null ? $"App {appId}" : null;
    }

    [RelayCommand]
    private void ConvertToSteam()
    {
        if (SuggestedSteamAppId is null) return;
        WeakReferenceMessenger.Default.Send(
            new ConvertToSteamMessage(this, SuggestedSteamAppId, SuggestedGameName ?? string.Empty));
    }

    public override StepBase ToModel() => new LaunchAppStep
    {
        Label       = Label,
        IsEnabled   = IsEnabled,
        ExePath     = ExePath,
        Arguments   = Arguments,
        WaitForExit = WaitForExit,
    };

    public override void FromModel(StepBase model)
    {
        var m = (LaunchAppStep)model;
        Label       = m.Label;
        IsEnabled   = m.IsEnabled;
        ExePath     = m.ExePath;
        Arguments   = m.Arguments;
        WaitForExit = m.WaitForExit;
    }
}
