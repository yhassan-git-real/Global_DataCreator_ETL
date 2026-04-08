using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using GlobalDataCreatorETL.UI.ViewModels;

namespace GlobalDataCreatorETL.UI.Views.Dialogs;

public partial class ExecutionResultDialog : Window
{
    private ExecutionResultDialogViewModel? _vm;
    private bool _detailsExpanded;

    public ExecutionResultDialog()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        _vm = DataContext as ExecutionResultDialogViewModel;
        if (_vm is null) return;

        ApplyTheme(_vm);
        PopulateContent(_vm);
    }

    // ── Theme ──────────────────────────────────────────────────────────────────

    private void ApplyTheme(ExecutionResultDialogViewModel vm)
    {
        switch (vm.Status)
        {
            case ExecutionDialogStatus.Success:
                IconCircle.Background      = new SolidColorBrush(Color.Parse("#1A22C55E"));
                IconCircle.BorderBrush     = new SolidColorBrush(Color.Parse("#4022C55E"));
                IconCircle.BorderThickness = new Thickness(1.5);
                HeaderIcon.Text            = "✔";
                HeaderIcon.Foreground      = new SolidColorBrush(Color.Parse("#22C55E"));
                AccentLine.Background      = new LinearGradientBrush
                {
                    StartPoint    = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint      = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops = { new GradientStop(Color.Parse("#16A34A"), 0), new GradientStop(Color.Parse("#22C55E"), 0.5), new GradientStop(Color.Parse("#0D1B2E"), 1) }
                };
                OkButton.Classes.Add("PrimaryButton");
                break;

            case ExecutionDialogStatus.NoData:
                IconCircle.Background      = new SolidColorBrush(Color.Parse("#1AF59E0B"));
                IconCircle.BorderBrush     = new SolidColorBrush(Color.Parse("#40F59E0B"));
                IconCircle.BorderThickness = new Thickness(1.5);
                HeaderIcon.Text            = "○";
                HeaderIcon.Foreground      = new SolidColorBrush(Color.Parse("#FCD34D"));
                AccentLine.Background      = new LinearGradientBrush
                {
                    StartPoint    = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint      = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops = { new GradientStop(Color.Parse("#D97706"), 0), new GradientStop(Color.Parse("#FCD34D"), 0.5), new GradientStop(Color.Parse("#0D1B2E"), 1) }
                };
                OkButton.Classes.Add("OutlineButton");
                break;

            default: // Error
                IconCircle.Background      = new SolidColorBrush(Color.Parse("#1ADC2626"));
                IconCircle.BorderBrush     = new SolidColorBrush(Color.Parse("#50DC2626"));
                IconCircle.BorderThickness = new Thickness(1.5);
                HeaderIcon.Text            = "✖";
                HeaderIcon.Foreground      = new SolidColorBrush(Color.Parse("#F87171"));
                AccentLine.Background      = new LinearGradientBrush
                {
                    StartPoint    = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint      = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops = { new GradientStop(Color.Parse("#DC2626"), 0), new GradientStop(Color.Parse("#F87171"), 0.5), new GradientStop(Color.Parse("#0D1B2E"), 1) }
                };
                OkButton.Classes.Add("DangerButton");
                break;
        }
    }

    // ── Content population ─────────────────────────────────────────────────────

    private void PopulateContent(ExecutionResultDialogViewModel vm)
    {
        TitleText.Text = vm.Title;
        SubtitleText.Text = vm.Status switch
        {
            ExecutionDialogStatus.Success => $"{vm.FilesGenerated} file(s) exported successfully",
            ExecutionDialogStatus.NoData  => "Execution completed — no files were generated.",
            _                             => "The operation could not complete. See details below."
        };

        // Reset all sections hidden first
        SuccessSection.IsVisible    = false;
        NoDataSection.IsVisible     = false;
        ErrorSection.IsVisible      = false;
        CopyDetailsButton.IsVisible = false;

        switch (vm.Status)
        {
            case ExecutionDialogStatus.Success:
                SuccessSection.IsVisible = true;
                FilesGeneratedRun.Text   = vm.FilesGenerated.ToString("N0");
                FileNamesText.Text       = vm.FileNamesDisplay;
                OutputDirText.Text       = vm.OutputDirectory;
                TotalRowsRun.Text        = vm.TotalRows.ToString("N0");
                DurationRun.Text         = vm.Duration;
                if (vm.IsBatch)
                {
                    SuccessfulRunsRow.IsVisible = true;
                    SuccessfulRunsRun.Text       = $"{vm.SuccessfulRuns} / {vm.TotalCombinations}";
                }
                if (vm.HasFailedRuns)
                {
                    FailedRunsRow.IsVisible = true;
                    FailedRunsRun.Text      = vm.FailedRuns.ToString("N0");
                }
                break;

            case ExecutionDialogStatus.NoData:
                NoDataSection.IsVisible  = true;
                NoDataDurationRun.Text   = vm.Duration;
                break;

            default: // Error
                ErrorSection.IsVisible  = true;
                ErrorMessageText.Text   = vm.ErrorMessage;
                if (vm.HasErrorDetail)
                {
                    DetailsSection.IsVisible    = true;
                    ErrorDetailText.Text        = vm.ErrorDetail;
                    CopyDetailsButton.IsVisible = true;
                }
                else
                {
                    DetailsSection.IsVisible = false;
                }
                break;
        }
    }

    // ── Interaction handlers ───────────────────────────────────────────────────

    private void OkButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void ToggleDetails_Click(object? sender, RoutedEventArgs e)
    {
        _detailsExpanded = !_detailsExpanded;
        DetailsBorder.IsVisible = _detailsExpanded;
        ToggleIcon.Text = _detailsExpanded ? "▼" : "▶";

        if (_vm is not null)
            _vm.ShowDetails = _detailsExpanded;
    }

    private async void CopyDetails_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(_vm.ClipboardText);
    }
}
