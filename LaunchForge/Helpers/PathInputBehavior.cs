using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LaunchForge.Helpers;

// Attached behavior: paste strips surrounding quotes added by Windows "Copy as path".
// Usage:  <TextBox helpers:PathInputBehavior.Enabled="True" />
public static class PathInputBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(PathInputBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj)  => (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        if ((bool)e.NewValue)
            tb.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPaste));
    }

    private static void OnPaste(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (!Clipboard.ContainsText()) return;

        var text = Clipboard.GetText();

        // Strip surrounding quotes produced by "Copy as path"
        if (text.Length > 1 && text[0] == '"' && text[^1] == '"')
            text = text[1..^1];

        tb.SetCurrentValue(TextBox.TextProperty, text);
        tb.CaretIndex = tb.Text.Length; // move caret to end
        e.Handled    = true;
    }
}
