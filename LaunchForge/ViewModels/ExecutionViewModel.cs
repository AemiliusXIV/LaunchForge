using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaunchForge.Services;

namespace LaunchForge.ViewModels;

public partial class ExecutionViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _currentStepIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _totalSteps;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _log           = string.Empty;

    public double Progress => TotalSteps > 0
        ? (double)(CurrentStepIndex + 1) / TotalSteps
        : 0;

    public CancellationTokenSource? CancellationSource { get; private set; }

    private bool CanCancel() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => CancellationSource?.Cancel();

    public void Begin(int totalSteps)
    {
        Log               = string.Empty;
        TotalSteps        = totalSteps;
        CurrentStepIndex  = -1;
        StatusMessage     = "Starting...";
        IsRunning         = true;
        CancellationSource = new CancellationTokenSource();
    }

    public void ReportProgress(ExecutionProgress p)
    {
        // Called via IProgress<T> from the main thread (Progress<T> marshals automatically)
        switch (p.Kind)
        {
            case ExecutionProgressKind.StepStarted:
                CurrentStepIndex = p.StepIndex;
                StatusMessage    = $"Step {p.StepIndex + 1}/{TotalSteps}: {p.Message}";
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Starting: {p.Message}");
                break;

            case ExecutionProgressKind.StepCompleted:
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Done: {p.Message}");
                break;

            case ExecutionProgressKind.StepSkipped:
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Skipped: step {p.StepIndex + 1}");
                break;

            case ExecutionProgressKind.Error:
                StatusMessage = p.Message;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] ERROR: {p.Message}");
                IsRunning     = false;
                break;

            case ExecutionProgressKind.Finished:
                StatusMessage = p.Message;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {p.Message}");
                IsRunning     = false;
                break;
        }
    }

    private void AppendLog(string line)
    {
        Log = string.IsNullOrEmpty(Log) ? line : Log + Environment.NewLine + line;
    }
}
