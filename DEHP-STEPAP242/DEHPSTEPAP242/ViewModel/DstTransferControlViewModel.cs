
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
        /// Initializes a new <see cref="DstTransferControlViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public DstTransferControlViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            var canTransfert = CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
                .Select(x => !x.Reset).ObserveOn(RxApp.MainThreadScheduler);
            
            this.TransferCommand = ReactiveCommand.CreateAsyncTask(canTransfert, async _ => await this.TransferCommandExecute());

            var canCancel = this.WhenAnyValue(x => x.AreThereAnyTransferInProgress);
            this.CancelCommand = ReactiveCommand.CreateAsyncTask(canCancel, async _ => await this.CancelTransfer());
        }

        /// <summary>
        /// Cancels the transfer in progress
        /// </summary>
        /// <returns>A <see cref="Task"/><returns>
        private Task CancelTransfer()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the transfert command
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task TransferCommandExecute()
        {
            this.AreThereAnyTransferInProgress = true;
            await this.dstController.Transfer();
            this.AreThereAnyTransferInProgress = false;
        }
    }
}
