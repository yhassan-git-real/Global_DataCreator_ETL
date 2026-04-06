using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace GlobalDataCreatorETL.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Direct call: Opened fires before WM_PAINT, so this invalidation
        // is processed by the first layout/render pass — not after it.
        InvalidateMeasure();

        // Activated safety net: fires after DWM completes its async frame
        // extension (DwmExtendFrameIntoClientArea + WM_NCCALCSIZE round-trip).
        // Catches the case where the decoration margin update arrives after
        // the first paint and would otherwise leave buttons invisible.
        Activated += OnWindowFirstActivated;
    }

    private void OnWindowFirstActivated(object? sender, EventArgs e)
    {
        Activated -= OnWindowFirstActivated;
        InvalidateMeasure();
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        // Don't start drag when clicking a window control button
        if (e.Source is Visual v && v.FindAncestorOfType<Button>() is not null)
            return;

        BeginMoveDrag(e);
    }

    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
