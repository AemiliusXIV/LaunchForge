using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaunchForge.Models;
using LaunchForge.ViewModels;

namespace LaunchForge.Views;

public partial class SteamPickerDialog : Window
{
    public SteamPickerDialog()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        // If nothing is selected in the list but a manual App ID was typed, synthesise a game object
        if (DataContext is SteamPickerViewModel vm && vm.SelectedGame is null
            && !string.IsNullOrWhiteSpace(vm.ManualAppId))
        {
            vm.SelectedGame = new SteamGame { AppId = vm.ManualAppId.Trim(), Name = $"App {vm.ManualAppId.Trim()}" };
        }

        if (DataContext is SteamPickerViewModel vm2 && vm2.SelectedGame is not null)
            DialogResult = true;
    }

    private void ListBox_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is not null)
            DialogResult = true;
    }
}
