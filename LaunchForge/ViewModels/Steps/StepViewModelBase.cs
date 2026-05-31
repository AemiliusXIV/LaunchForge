using CommunityToolkit.Mvvm.ComponentModel;
using LaunchForge.Models.Steps;

namespace LaunchForge.ViewModels.Steps;

public abstract partial class StepViewModelBase : ObservableObject
{
    [ObservableProperty] private string _label     = string.Empty;
    [ObservableProperty] private bool   _isEnabled = true;

    public abstract string   Summary  { get; }
    public abstract StepBase ToModel();
    public abstract void     FromModel(StepBase model);

    // Human-readable type tag shown in the step list chip
    public abstract string TypeTag { get; }
}
