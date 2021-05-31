// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstTransferControlViewModel.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
// 
//    This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
// 
//    The DEHP STEP-AP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHP STEP-AP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.ViewModel
{
    using CDP4Dal;
    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPSTEPAP242.DstController;
    using ReactiveUI;
    using System;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

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
        /// The <see cref="IExchangeHistoryService"/>
        /// </summary>
        private readonly IExchangeHistoryService exchangeHistoryService;

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
        /// <param name="exchangeHistoryService">The <see cref="IExchangeHistoryService"/></param>
        public DstTransferControlViewModel(IDstController dstController, IStatusBarControlViewModel statusBar,
            IExchangeHistoryService exchangeHistoryService)
        {
            this.dstController = dstController;
            this.statusBar = statusBar;
            this.exchangeHistoryService = exchangeHistoryService;

            this.InitializeCommandsAndObservables();
        }

        /// <summary>
        /// Initializes the commands and observables
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
             CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
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
            this.dstController.CleanCurrentMapping();
            this.exchangeHistoryService.ClearPending();

            await Task.Delay(1);

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
            await this.exchangeHistoryService.Write();

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
