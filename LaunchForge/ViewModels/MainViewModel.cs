using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LaunchForge.Messages;
using LaunchForge.Models;
using LaunchForge.Services;
using LaunchForge.ViewModels.Steps;
using Microsoft.Win32;

namespace LaunchForge.ViewModels;

public partial class MainViewModel : ObservableObject,
    IRecipient<ConvertToSteamMessage>
{
    private readonly SequenceSerializer     _serializer = new();
    private readonly ExecutionEngine        _engine     = new();
    private readonly SteamRegistryService   _steamReg   = new();
    private readonly SteamAppCatalogService _steamCat   = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _sequenceName = "Untitled Sequence";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    [ObservableProperty] private StepViewModelBase? _selectedStep;

    public ObservableCollection<StepViewModelBase> Steps { get; } = [];
    public ExecutionViewModel Execution { get; } = new();

    public string WindowTitle =>
        $"{SequenceName}{(IsDirty ? " *" : "")} - LaunchForge";

    public MainViewModel()
    {
        WeakReferenceMessenger.Default.Register(this);
        Steps.CollectionChanged += (_, _) =>
        {
            IsDirty = true;
            RunSequenceCommand.NotifyCanExecuteChanged();
        };
        Execution.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExecutionViewModel.IsRunning))
                RunSequenceCommand.NotifyCanExecuteChanged();
        };
        _ = _steamCat.LoadCacheAsync();
    }

    // ── Messenger ─────────────────────────────────────────────────────────────

    public void Receive(ConvertToSteamMessage message)
    {
        var idx = Steps.IndexOf(message.SourceVm);
        if (idx < 0) return;

        // Try to enrich the game name from the catalog
        var name = _steamCat.GetNameByAppId(message.AppId)
                   ?? message.GameName;

        var newVm = new LaunchSteamStepViewModel
        {
            AppId    = message.AppId,
            GameName = name,
        };
        Steps[idx]   = newVm;
        SelectedStep = newVm;
        IsDirty      = true;
    }

    // ── Step management ───────────────────────────────────────────────────────

    [RelayCommand]
    private void AddStep(string typeName)
    {
        var vm = StepViewModelFactory.CreateEmpty(typeName);
        Steps.Add(vm);
        SelectedStep = vm;
    }

    [RelayCommand]
    private void RemoveStep(StepViewModelBase? vm)
    {
        if (vm is null) return;
        var idx = Steps.IndexOf(vm);
        Steps.Remove(vm);
        if (Steps.Count > 0)
            SelectedStep = Steps[Math.Min(idx, Steps.Count - 1)];
        else
            SelectedStep = null;
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveStepUp(StepViewModelBase? vm)
    {
        if (vm is null) return;
        var idx = Steps.IndexOf(vm);
        if (idx <= 0) return;
        Steps.Move(idx, idx - 1);
    }

    private bool CanMoveUp(StepViewModelBase? vm) =>
        vm is not null && Steps.IndexOf(vm) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveStepDown(StepViewModelBase? vm)
    {
        if (vm is null) return;
        var idx = Steps.IndexOf(vm);
        if (idx < 0 || idx >= Steps.Count - 1) return;
        Steps.Move(idx, idx + 1);
    }

    private bool CanMoveDown(StepViewModelBase? vm) =>
        vm is not null && Steps.IndexOf(vm) < Steps.Count - 1;

    partial void OnSelectedStepChanged(StepViewModelBase? value)
    {
        MoveStepUpCommand.NotifyCanExecuteChanged();
        MoveStepDownCommand.NotifyCanExecuteChanged();
    }

    // ── File operations ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task NewSequenceAsync()
    {
        if (!await ConfirmUnsavedAsync()) return;
        Steps.Clear();
        SequenceName = "Untitled Sequence";
        FilePath     = null;
        IsDirty      = false;
    }

    [RelayCommand]
    private async Task OpenSequenceAsync()
    {
        if (!await ConfirmUnsavedAsync()) return;

        var dlg = new OpenFileDialog
        {
            Filter = "LaunchForge Sequence (*.lfjson)|*.lfjson|All files (*.*)|*.*",
            Title  = "Open Sequence",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var seq = await _serializer.LoadAsync(dlg.FileName);
            Steps.Clear();
            foreach (var step in seq.Steps)
                Steps.Add(StepViewModelFactory.FromModel(step));
            SequenceName = seq.Name;
            FilePath     = dlg.FileName;
            IsDirty      = false;
            SelectedStep = Steps.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not open file:\n{ex.Message}", "Open Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SaveSequenceAsync()
    {
        if (FilePath is null)
        {
            await SaveSequenceAsAsync();
            return;
        }
        await SaveToPathAsync(FilePath);
    }

    [RelayCommand]
    private async Task SaveSequenceAsAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter      = "LaunchForge Sequence (*.lfjson)|*.lfjson",
            Title       = "Save Sequence As",
            FileName    = SequenceName,
            DefaultExt  = ".lfjson",
        };
        if (dlg.ShowDialog() != true) return;
        await SaveToPathAsync(dlg.FileName);
    }

    private async Task SaveToPathAsync(string path)
    {
        var seq = BuildSequence();
        try
        {
            await _serializer.SaveAsync(seq, path);
            FilePath = path;
            IsDirty  = false;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not save file:\n{ex.Message}", "Save Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    // ── Script export ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ExportBatAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter     = "Batch Script (*.bat)|*.bat",
            Title      = "Export as .bat",
            FileName   = SequenceName,
            DefaultExt = ".bat",
        };
        if (dlg.ShowDialog() != true) return;
        var script = ScriptGenerator.GenerateBat(BuildSequence());
        await System.IO.File.WriteAllTextAsync(dlg.FileName, script);
    }

    [RelayCommand]
    private async Task ExportPs1Async()
    {
        var dlg = new SaveFileDialog
        {
            Filter     = "PowerShell Script (*.ps1)|*.ps1",
            Title      = "Export as .ps1",
            FileName   = SequenceName,
            DefaultExt = ".ps1",
        };
        if (dlg.ShowDialog() != true) return;
        var script = ScriptGenerator.GeneratePs1(BuildSequence());
        await System.IO.File.WriteAllTextAsync(dlg.FileName, script);
    }

    // ── Execution ─────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunSequenceAsync()
    {
        if (Steps.Count == 0) return;

        var models = Steps.Select(vm => vm.ToModel()).ToList();
        Execution.Begin(models.Count);

        var progress = new Progress<ExecutionProgress>(Execution.ReportProgress);
        var ct = Execution.CancellationSource!.Token;

        await Task.Run(() => _engine.RunAsync(models, progress, ct));
    }

    private bool CanRun() => Steps.Count > 0 && !Execution.IsRunning;

    // ── Steam picker (called from LaunchSteamStepEditor) ─────────────────────

    public SteamPickerViewModel CreateSteamPickerViewModel() =>
        new(_steamReg, _steamCat);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Sequence BuildSequence() => new()
    {
        Name  = SequenceName,
        Steps = [.. Steps.Select(vm => vm.ToModel())],
    };

    // Returns true if safe to continue (user saved or discarded, or there was nothing to save)
    private Task<bool> ConfirmUnsavedAsync()
    {
        if (!IsDirty) return Task.FromResult(true);
        var result = System.Windows.MessageBox.Show(
            "You have unsaved changes. Discard them?",
            "Unsaved Changes",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        return Task.FromResult(result == System.Windows.MessageBoxResult.Yes);
    }
}
