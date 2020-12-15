// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstDataSourceViewModel.cs" company="RHEA System S.A.">
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
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.Views.Dialogs;
	using Microsoft.Win32;
	using System.Diagnostics;

	/// <summary>
	/// The <see cref="DstDataSourceViewModel"/> is the view model for the panel that will display controls and data relative to EcosimPro
	/// </summary>
	public sealed class DstDataSourceViewModel : DataSourceViewModel, IDstDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;
        
        /// <summary>
        /// Gets the <see cref="IDstBrowserHeaderViewModel"/>
        /// </summary>
        public IDstBrowserHeaderViewModel DstBrowserHeader { get; }

        /// <summary>
        /// Initializes a new <see cref="DstDataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="dstBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        public DstDataSourceViewModel(INavigationService navigationService, IDstController dstController, IDstBrowserHeaderViewModel dstBrowserHeader) : base(navigationService)
        {
            this.dstController = dstController;
            this.DstBrowserHeader = dstBrowserHeader;
            this.InitializeCommands();

            //this.ConnectCommand.Subscribe(_ => this.LoadFileCommandExecute());
        }

        /// <summary>
        /// Executes the <see cref="DataSourceViewModel.ConnectCommand"/>
        /// </summary>
        protected override void ConnectCommandExecute()
        {
            if (this.dstController.IsSessionOpen)
            {
                this.dstController.CloseSession();
            }
            else
            {
                this.NavigationService.ShowDialog<DstLogin>();
            }

            this.UpdateConnectButtonText(this.dstController.IsSessionOpen);
        }

        protected override void LoadFileCommandExecute()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
			{
                //txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
                Debug.WriteLine($"Load file: {openFileDialog.FileName}");

                this.dstController.Load(openFileDialog.FileName);

                Debug.WriteLine($"dstController.Load finished");
            }


            /*
            if (this.dstController.IsSessionOpen)
            {
                this.dstController.CloseSession();
            }
            else
            {
                //this.NavigationService.ShowDialog<DstLogin>();
                this.NavigationService.ShowDialog<DstLoadStepFile>();
            }

            this.UpdateConnectButtonText(this.dstController.IsSessionOpen);
            */
        }
    }
}
