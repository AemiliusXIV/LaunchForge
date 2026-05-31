using System.Windows;
using System.Windows.Controls;
using LaunchForge.ViewModels.Steps;
using Microsoft.Win32;

namespace LaunchForge.Views.Steps;

public partial class LaunchAppStepEditor : UserControl
{
    public LaunchAppStepEditor()
    {
        InitializeComponent();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Executables & Shortcuts (*.exe;*.lnk;*.url)|*.exe;*.lnk;*.url|All files (*.*)|*.*",
            Title  = "Select Application",
        };
        if (dlg.ShowDialog() != true) return;
        if (DataContext is LaunchAppStepViewModel vm)
            vm.ExePath = dlg.FileName;
    }
}
