using System.Windows;
using System.Windows.Controls;
using LaunchForge.ViewModels;
using LaunchForge.ViewModels.Steps;

namespace LaunchForge.Views.Steps;

public partial class LaunchSteamStepEditor : UserControl
{
    public LaunchSteamStepEditor()
    {
        InitializeComponent();
    }

    private void PickGame_Click(object sender, RoutedEventArgs e)
    {
        // Walk up to MainViewModel to get the picker VM (keeps the editor decoupled from the service)
        var mainVm = Application.Current.MainWindow?.DataContext as MainViewModel;
        if (mainVm is null) return;

        var pickerVm = mainVm.CreateSteamPickerViewModel();
        pickerVm.Load();

        var dialog = new SteamPickerDialog { DataContext = pickerVm };
        if (dialog.ShowDialog() == true && pickerVm.SelectedGame is { } game)
        {
            if (DataContext is LaunchSteamStepViewModel vm)
            {
                vm.AppId    = game.AppId;
                vm.GameName = game.Name;
            }
        }
    }
}
