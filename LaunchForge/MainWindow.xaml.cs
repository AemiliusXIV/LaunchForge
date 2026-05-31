using System.Windows;
using System.Windows.Controls;
using LaunchForge.ViewModels;

namespace LaunchForge;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Open the "add step" popup
    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        AddStepPopup.IsOpen = true;
    }

    // One of the step-type menu items was clicked
    private void AddStepMenu_Click(object sender, RoutedEventArgs e)
    {
        AddStepPopup.IsOpen = false;

        if (sender is not Button btn || btn.Tag is not string typeName) return;
        if (DataContext is not MainViewModel vm) return;

        vm.AddStepCommand.Execute(typeName);
    }
}
