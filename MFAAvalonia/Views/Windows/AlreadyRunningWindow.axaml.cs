using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SukiUI.Controls;

namespace MFAAvalonia.Views.Windows;

/// <summary>
/// Window displayed when another instance of the application is already running
/// </summary>
public partial class AlreadyRunningWindow : SukiWindow
{
    public static readonly StyledProperty<string?> MessageTextProperty =
        AvaloniaProperty.Register<AlreadyRunningWindow, string?>(nameof(MessageText), string.Empty);

    /// <summary>
    /// The message to display to the user
    /// </summary>
    public string? MessageText
    {
        get => GetValue(MessageTextProperty);
        set => SetValue(MessageTextProperty, value);
    }

    public AlreadyRunningWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Handles the OK button click - closes the window
    /// </summary>
    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
