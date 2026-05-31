using System.Windows.Controls;

namespace LaunchForge.Views;

public partial class ExecutionPanel : UserControl
{
    public ExecutionPanel()
    {
        InitializeComponent();
    }

    // Auto-scroll the log to the bottom when new text arrives
    private void Log_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.ScrollToEnd();
    }
}
