// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubDataSourceViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
// 
//    This file is part of DEHPSTEPAP242
// 
//    The DEHPSTEPAP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHPSTEPAP242 is distributed in the hope that it will be useful,
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
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;

    using CDP4Common.SiteDirectoryData;
    using CDP4Common.CommonData;
    using CDP4Common.Types;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPSTEPAP242.ViewModel.Interfaces;

    using ReactiveUI;
    using System.Collections.Generic;
    using DEHPSTEPAP242.Services.DstHubService;
    using CDP4Dal;
    using DEHPCommon.Events;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;

    /// <summary>
    /// View model that represents a data source panel which holds a tree like browser, a informational header and
    /// some control regarding the connection to the data source
    /// </summary>
    public sealed class HubDataSourceViewModel : DataSourceViewModel, IHubDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IObjectBrowserTreeSelectorService"/>
        /// </summary>
        private readonly IObjectBrowserTreeSelectorService treeSelectorService;

        /// <summary>
        /// The <see cref="IHubBrowserHeaderViewModel"/>
        /// </summary>
        public IHubBrowserHeaderViewModel HubBrowserHeader { get; set; }

        /// <summary>
        /// The <see cref="IObjectBrowserViewModel"/>
        /// </summary>
        public IHubObjectBrowserViewModel ObjectBrowser { get; set; }

        /// <summary>
        /// The <see cref="IPublicationBrowserViewModel"/>
        /// </summary>
        public IPublicationBrowserViewModel PublicationBrowser { get; set; }

        /// <summary>
        /// The <see cref="IHubFileStoreBrowserViewModel"/>
        /// </summary>
        public IHubFileStoreBrowserViewModel HubFileStoreBrowser { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="HubDataSourceViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="hubBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        /// <param name="objectBrowser">The <see cref="IHubObjectBrowserViewModel"/></param>
        /// <param name="publicationBrowser">The <see cref="IPublicationBrowserViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="treeSelectorService">The <see cref="IObjectBrowserTreeSelectorService"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService "/></param>
        /// <param name="hubFileBrowser">The <see cref="IHubFileStoreBrowserViewModel "/></param>
        public HubDataSourceViewModel(
            IHubController hubController,
            IHubBrowserHeaderViewModel hubBrowserHeader, 
            IHubObjectBrowserViewModel objectBrowser,
            IPublicationBrowserViewModel publicationBrowser,
            INavigationService navigationService,
            IObjectBrowserTreeSelectorService treeSelectorService,
            IDstHubService dstHubService,
            IHubFileStoreBrowserViewModel hubFileBrowser) : base(navigationService)
        {
            this.hubController = hubController;
            this.HubBrowserHeader = hubBrowserHeader;
            this.ObjectBrowser = objectBrowser;
            this.PublicationBrowser = publicationBrowser;
            this.treeSelectorService = treeSelectorService;
            this.dstHubService = dstHubService;
            this.HubFileStoreBrowser = hubFileBrowser;

            InitializeCommands();
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            this.ConnectCommand = ReactiveCommand.Create();
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());

            // Activate Refresh when an Iteration is open
            var canRefresh = this.WhenAny(
                vm => vm.hubController.OpenIteration,
                (iteration) => iteration.Value != null);

            this.RefreshCommand = ReactiveCommand.Create(canRefresh);
            this.RefreshCommand.Subscribe(_ => this.RefreshCommandExecute());
        }

        /// <summary>
        /// The connect text for the connect button
        /// </summary>
        private const string ConnectText = "Connect";

        /// <summary>
        /// The disconnect text for the connect button
        /// </summary>
        private const string DisconnectText = "Disconnect";

        /// <summary>
        /// Backing field for <see cref="ConnectButtonText"/>
        /// </summary>
        private string connectButtonText = ConnectText;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string ConnectButtonText
        {
            get => this.connectButtonText;
            set => this.RaiseAndSetIfChanged(ref this.connectButtonText, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for connecting to a data source
        /// </summary>
        public ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> to refresh the data source
        /// </summary>
        public ReactiveCommand<object> RefreshCommand { get; set; }

        /// <summary>
        /// Executes the <see cref="HubDataSourceViewModel.ConnectCommand"/>
        /// </summary>
        private void ConnectCommandExecute()
        {
            if (this.hubController.IsSessionOpen)
            {
                this.hubController.Close();
            }
            else
            {
                this.NavigationService.ShowDialog<Login>();
            }

            this.UpdateConnectButtonText(this.hubController.IsSessionOpen);

            if (hubController.IsSessionOpen)
            {
                this.dstHubService.CheckHubDependencies();
            }
        }

        /// <summary>
        /// Updates the <see cref="ConnectButtonText"/>
        /// </summary>
        /// <param name="isSessionOpen">Assert whether the the button text should be <see cref="ConnectText"/> or <see cref="DisconnectText"/></param>
        private void UpdateConnectButtonText(bool isSessionOpen)
        {
            this.ConnectButtonText = isSessionOpen ? DisconnectText : ConnectText;
        }

        /// <summary>
        /// Refreshes the <see cref="IHubController"/> cache
        /// </summary>
        private void RefreshCommandExecute()
        {
            this.hubController.Refresh();
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
        }

        /// <summary>
        /// Loads a STEP file stored in the server.
        /// </summary>
        protected override void LoadFileCommandExecute()
        {
            throw new NotImplementedException();
        }
    }
}
