// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSourceViewModel.cs" company="RHEA System S.A.">
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

    using DEHPCommon.Services.NavigationService;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DataSourceViewModel"/> is the base view model for view model that represents a data source like <see cref="DstDataSourceViewModel"/>
    /// </summary>
    public abstract class DataSourceViewModel : ReactiveObject
    {
        /// <summary>
        /// The Load text for the load button
        /// </summary>
        private const string LoadText = "Load STEP-AP242 file";

        private string loadButtonText = LoadText;

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
        /// Gets the <see cref="INavigationService"/>
        /// </summary>
        protected readonly INavigationService NavigationService;

        /// <summary>
        /// Initializes a new <see cref="DataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        protected DataSourceViewModel(INavigationService navigationService)
        {
            this.NavigationService = navigationService;
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string LoadButtonText
        {
            get => this.loadButtonText;
            set => this.RaiseAndSetIfChanged(ref this.loadButtonText, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for loadind a STEP file source
        /// </summary>
        public ReactiveCommand<object> LoadFileCommand { get; set; }

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
        /// Initializes the <see cref="ReactiveCommand{T}"/>
        /// </summary>
        protected virtual void InitializeCommands()
        {
            this.ConnectCommand = ReactiveCommand.Create();
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());

            this.LoadFileCommand = ReactiveCommand.Create();
            this.LoadFileCommand.Subscribe(_ => this.LoadFileCommandExecute());
        }

        /// <summary>
        /// Executes the <see cref="LoadFileCommand"/>
        /// </summary>
        protected abstract void LoadFileCommandExecute();

        /// <summary>
        /// Executes the <see cref="ConnectCommand"/>
        /// </summary>
        protected abstract void ConnectCommandExecute();

        /// <summary>
        /// Updates the <see cref="ConnectButtonText"/>
        /// </summary>
        /// <param name="isSessionOpen">Assert whether the the button text should be <see cref="ConnectText"/> or <see cref="DisconnectText"/></param>
        protected void UpdateConnectButtonText(bool isSessionOpen)
        {
            this.ConnectButtonText = isSessionOpen ? DisconnectText : ConnectText;
        }

    }
}
