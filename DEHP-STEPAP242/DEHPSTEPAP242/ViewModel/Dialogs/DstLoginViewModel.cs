// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstLoginViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using System;
    using System.Reactive;
    using System.Threading.Tasks;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The view-model for the Login that allows users to connect to a OPC UA datasource
    /// </summary>
    public class DstLoginViewModel : ReactiveObject, IDstLoginViewModel, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControlView;

        /// <summary>
        /// Backing field for the <see cref="UserName"/> property
        /// </summary>
        private string username;

        /// <summary>
        /// Gets or sets server username value
        /// </summary>
        public string UserName
        {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Password"/> property
        /// </summary>
        private string password;

        /// <summary>
        /// Gets or sets server password value
        /// </summary>
        public string Password
        {
            get => this.password;
            set => this.RaiseAndSetIfChanged(ref this.password, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Uri"/> property
        /// </summary>
        private string uri;

        /// <summary>
        /// Gets or sets server uri
        /// </summary>
        public string Uri
        {
            get => this.uri;
            set => this.RaiseAndSetIfChanged(ref this.uri, value);
        }

        /// <summary>
        /// Backing field for the <see cref="LoginSuccessfull"/> property
        /// </summary>
        private bool loginSuccessfull;

        /// <summary>
        /// Gets or sets login succesfully flag
        /// </summary>
        public bool LoginSuccessfull
        {
            get => this.loginSuccessfull;
            private set => this.RaiseAndSetIfChanged(ref this.loginSuccessfull, value);
        }

        /// <summary>
        /// Backing field for <see cref="RequiresAuthentication"/>
        /// </summary>
        private bool requiresAuthentication;

        /// <summary>
        /// Gets or sets an assert whether the specified <see cref="Uri"/> endpoint requires authentication
        /// </summary>
        public bool RequiresAuthentication
        {
            get => this.requiresAuthentication;
            set => this.RaiseAndSetIfChanged(ref this.requiresAuthentication, value);
        }
        
        /// <summary>
        /// Gets the server login command
        /// </summary>
        public ReactiveCommand<Unit> LoginCommand { get; private set; }
        
        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DstLoginViewModel"/> class.
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlView">The <see cref="IStatusBarControlViewModel"/></param>
        public DstLoginViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlView)
        {
            this.dstController = dstController;
            this.statusBarControlView = statusBarControlView;

            var canLogin = this.WhenAnyValue(
                vm => vm.UserName,
                vm => vm.Password,
                vm => vm.RequiresAuthentication,
                vm => vm.Uri,
                (username, password, requiresAuthentication, uri) =>
                    (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(username) || !requiresAuthentication) && !string.IsNullOrEmpty(uri));
            
            this.LoginCommand = ReactiveCommand.CreateAsyncTask(canLogin, async _ => await this.ExecuteLogin());
        }

        /// <summary>
        /// Executes login command
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        private async Task ExecuteLogin()
        {
            this.statusBarControlView.Append("Loggin in...");

            try
            {
                var credentials = this.RequiresAuthentication ? new UserIdentity(this.UserName, this.Password) : null;
                await this.dstController.Connect(this.Uri, true, credentials);
                this.LoginSuccessfull = this.dstController.IsSessionOpen;

                if (this.LoginSuccessfull)
                {
                    this.statusBarControlView.Append("Loggin successful");
                    await Task.Delay(1000);
                    this.CloseWindowBehavior?.Close();
                }
                else
                {
                    this.statusBarControlView.Append($"Loggin failed", StatusBarMessageSeverity.Info);
                }
            }
            catch (Exception exception)
            {
                this.statusBarControlView.Append($"Loggin failed: {exception.Message}", StatusBarMessageSeverity.Error);
            }
        }
    }
}
