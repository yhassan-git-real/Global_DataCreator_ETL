using ReactiveUI;
using System.Reactive.Linq;
using System.Windows.Input;

namespace GlobalDataCreatorETL.UI.ViewModels;

public sealed partial class MainViewModel
{
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> StartCommand { get; private set; } = null!;
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CancelCommand { get; private set; } = null!;
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ResetCommand { get; private set; } = null!;
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> BrowseFolderCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        var canStart   = this.WhenAnyValue(x => x.IsBusy).Select(busy => !busy);
        var canCancel  = this.WhenAnyValue(x => x.IsBusy);

        StartCommand  = ReactiveCommand.CreateFromTask(ExecuteStartAsync, canStart,
                            outputScheduler: RxApp.MainThreadScheduler);
        CancelCommand = ReactiveCommand.Create(ExecuteCancel, canCancel,
                            outputScheduler: RxApp.MainThreadScheduler);
        ResetCommand  = ReactiveCommand.Create(ExecuteReset,
                            outputScheduler: RxApp.MainThreadScheduler);
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(ExecuteBrowseFolderAsync,
                            outputScheduler: RxApp.MainThreadScheduler);
    }
}
