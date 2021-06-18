// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HUbDataSourceViewModel" company="Open Engineering S.A.">
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
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using DEHPCommon.UserInterfaces.Views;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using ReactiveUI;
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows;


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
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Gets the <see cref="IHubSessionControlViewModel"/>
        /// </summary>
        public IHubSessionControlViewModel SessionControl { get; }
               

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
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="sessionControl">The <see cref="IHubSessionControlViewModel"/></param>
        /// <param name="browserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        /// <param name="objectBrowser">The <see cref="IHubObjectBrowserViewModel"/></param>
        /// <param name="publicationBrowser">The <see cref="IPublicationBrowserViewModel"/></param>
        /// <param name="hubFileBrowser">The <see cref="IHubFileStoreBrowserViewModel "/></param>
        /// <param name="treeSelectorService">The <see cref="IObjectBrowserTreeSelectorService"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService "/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        public HubDataSourceViewModel(
            IHubController hubController,
            IDstController dstController,
            IHubSessionControlViewModel sessionControl,
            IHubBrowserHeaderViewModel browserHeader,
            IHubObjectBrowserViewModel objectBrowser,
            IPublicationBrowserViewModel publicationBrowser,
            IHubFileStoreBrowserViewModel hubFileBrowser,
            
            IDstHubService dstHubService,
            INavigationService navigationService) : base(navigationService)
        {
            this.hubController = hubController;
            this.dstController = dstController;
            this.SessionControl = sessionControl;
            this.HubBrowserHeader = browserHeader;
            this.ObjectBrowser = objectBrowser;
            this.PublicationBrowser = publicationBrowser;
            this.HubFileStoreBrowser = hubFileBrowser;            
            this.dstHubService = dstHubService;

            InitializeCommands();
        }

        /// <summary>
        /// Initializes the commands
        /// </summary>
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            this.ConnectCommand = ReactiveCommand.Create();
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());

            var isConnected = this.WhenAny(
                x => x.hubController.OpenIteration,
                x => x.hubController.IsSessionOpen,
                (i, o) => i.Value != null && o.Value)
                .ObserveOn(RxApp.MainThreadScheduler);

            isConnected.Subscribe(this.UpdateConnectButtonText);

            isConnected.Subscribe(_ =>
            {
                if (this.hubController.OpenIteration is { })
                {
                    this.dstHubService.CheckHubDependencies();
                }
            });
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
        /// Executes the <see cref="HubDataSourceViewModel.ConnectCommand"/>
        /// </summary>
        private void ConnectCommandExecute()
        {
            if (this.hubController.IsSessionOpen)
            {
                if (this.dstController.MapResult.Any())
                {
                    var result = MessageBox.Show(
                        "You have pending transfers.\nBy continuing, these transfers will be lost.",
                        "Disconnect confirmation",
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (result != MessageBoxResult.OK)
                    {
                        return;
                    }
                }

                this.hubController.Close();
            }
            else
            {
                this.NavigationService.ShowDialog<Login>();
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
        /// Loads a STEP file stored in the server.
        /// </summary>
        protected override void LoadFileCommandExecute()
        {
            throw new NotImplementedException();
        }
    }
}
