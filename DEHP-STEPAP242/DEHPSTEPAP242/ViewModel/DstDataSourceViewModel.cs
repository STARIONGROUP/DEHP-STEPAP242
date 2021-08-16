// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstDataSourceViewModel.cs" company="Open Engineering S.A.">
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
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.Views.Dialogs;
    using ReactiveUI;
    using System;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// The <see cref="DstDataSourceViewModel"/> is the view model for the panel that will display controls and data relative to EcosimPro
    /// </summary>
    public sealed class DstDataSourceViewModel : DataSourceViewModel, IDstDataSourceViewModel
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;

        #endregion

        #region IDstDataSourceViewModel interface
        /// <summary>
        /// Gets the <see cref="IDstBrowserHeaderViewModel"/>
        /// </summary>
        public IDstBrowserHeaderViewModel DstBrowserHeader { get; }

        /// <summary>
        /// Gets the <see cref="IDstObjectBrowserViewModel"/>
        /// </summary>
        public IDstObjectBrowserViewModel DstObjectBrowser { get; }

        /// <summary>
        /// Backing field for <see cref="IsFileInHub"/>
        /// </summary>
        private bool isFileInHub;

        /// <summary>
        /// Gets or sets a value indicating whether the TransfertCommand" is executing
        /// </summary>
        public bool IsFileInHub
        {
            get => this.isFileInHub;
            private set => this.RaiseAndSetIfChanged(ref this.isFileInHub, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstDataSourceViewModel"/>
        /// </summary>
        /// <param name="
        /// ">The <see cref="INavigationService"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="dstBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public DstDataSourceViewModel(INavigationService navigationService,
            IDstController dstController, IDstBrowserHeaderViewModel dstBrowserHeader,
            IDstObjectBrowserViewModel dstObjectBrowser,
            IHubController hubController,
            IDstHubService dstHubService) : base(navigationService)
        {
            this.dstController = dstController;
            this.hubController = hubController;
            this.DstBrowserHeader = dstBrowserHeader;
            this.DstObjectBrowser = dstObjectBrowser;
            this.dstHubService = dstHubService;

            this.WhenAnyValue(
                vm => vm.dstController.Step3DFile,
                vm => vm.hubController.OpenIteration
               ).Subscribe(_ => this.UpdateFileInHubStatus());

            this.InitializeCommands();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Load a new STEP AP242 file.
        /// </summary>
        protected override void LoadFileCommandExecute()
        {
            if (this.dstController.MapResult.Any())
            {
                var result = MessageBox.Show(
                    "You have pending transfers.\nBy continuing, these transfers will be lost.",
                    "Load file confirmation",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }

            this.NavigationService.ShowDialog<DstLoadFile>();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if a current STEP AP242 file is stored in the Hub
        /// </summary>
        private void UpdateFileInHubStatus()
        {
            this.IsFileInHub = this.dstHubService.FindFile(this.dstController.Step3DFile?.FileName) != null;
        }

        #endregion
    }
}
