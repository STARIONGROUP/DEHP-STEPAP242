
namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using ReactiveUI;

    using CDP4Dal;

    using DEHPCommon.Events;
    using DEHPCommon.UserInterfaces.ViewModels;

    using DEHPSTEPAP242.DstController;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.Enumerators;
    using DEHPSTEPAP242.Events;


    /// <summary>
    /// Transfer Control ViewModel for STEP AP242 DST
    /// <inheritdoc cref="TransferControlViewModel"/>
    /// </summary>
    public class DstTransferControlViewModel : TransferControlViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// Backing field for <see cref="AreThereAnyTransferInProgress"/>
        /// </summary>
        private bool areThereAnyTransferInProgress;

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="TransferControlViewModel.TransferCommand"/> is executing
        /// </summary>
        public bool AreThereAnyTransferInProgress
        {
            get => this.areThereAnyTransferInProgress;
            set => this.RaiseAndSetIfChanged(ref this.areThereAnyTransferInProgress, value);
        }

        /// <summary>
        /// Backing field for <see cref="CanTransfer"/>
        /// </summary>
        private bool canTransfer;

        /// <summary>
        /// Gets or sets a value indicating whether there is any awaiting transfer
        /// </summary>
        public bool CanTransfer
        {
            get => this.canTransfer;
            set => this.RaiseAndSetIfChanged(ref this.canTransfer, value);
        }

        /// <summary>
        /// Initializes a new <see cref="DstTransferControlViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstTransferControlViewModel(IDstController dstController, IStatusBarControlViewModel statusBar)
        {
            this.dstController = dstController;
            this.statusBar = statusBar;

            this.InitializeCommandsAndObservables();
        }

        /// <summary>
        /// Initializes the commands and observables
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
            var canTransfert = CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
                .Select(x => !x.Reset).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.UpdateCanTransfer);

            this.dstController.MapResult.CountChanged.Subscribe(x => this.UpdateCanTransfer(x > 0));

            this.WhenAnyValue(vm => vm.dstController.IsLoading)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateCanTransfer(false));

            this.TransferCommand = ReactiveCommand.CreateAsyncTask(
                this.WhenAnyValue(x => x.CanTransfer),
                async _ => await this.TransferCommandExecute(),
                RxApp.MainThreadScheduler);

            this.TransferCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e => this.statusBar.Append($"{e.Message}", StatusBarMessageSeverity.Error));

            var canCancel = this.WhenAnyValue(x => x.AreThereAnyTransferInProgress);

            this.CancelCommand = ReactiveCommand.CreateAsyncTask(canCancel,
                async _ => await this.CancelTransfer(),
                RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Updates the <see cref="CanTransfer"/>
        /// </summary>
        private void UpdateCanTransfer(bool value)
        {
            this.CanTransfer = value;
        }

        /// <summary>
        /// Cancels the transfer in progress
        /// </summary>
        /// <returns>A <see cref="Task"/><returns>
        private async Task CancelTransfer()
        {
            await Task.Delay(1);

            this.dstController.CleanCurrentMapping();

            this.AreThereAnyTransferInProgress = false;
            this.IsIndeterminate = false;
        }

        /// <summary>
        /// Executes the transfert command
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task TransferCommandExecute()
        {
            this.AreThereAnyTransferInProgress = true;
            this.IsIndeterminate = true;
            this.statusBar.Append($"Transfers in progress");

            await this.dstController.Transfer();

            if (this.dstController.TransferTime > 1000)
            {
                this.statusBar.Append($"Transfers completed in {this.dstController.TransferTime / 1000.0} seconds");
            }
            else
            {
                this.statusBar.Append($"Transfers completed in {this.dstController.TransferTime} ms");
            }

            this.IsIndeterminate = false;
            this.AreThereAnyTransferInProgress = false;
        }
    }
}
