// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstLogin.xaml.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.Views.Dialogs
{
    using System.Windows;

    using DEHPSTEPAP242.ViewModel.Dialogs;

    /// <summary>
    /// Interaction logic for DstLoadFile.xaml
    /// </summary>
    public partial class DstLoadFile : Window
    {
        /// <summary>
        /// Initializes a new <see cref="DstLoadFile"/>
        /// </summary>
        public DstLoadFile()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Close evente validation.
        /// 
        /// It is not possible to close the window during the Load operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check that there is not a Loading task in progress
            var vm = (DstLoadFileViewModel) DataContext;

            if (vm.IsLoadingFile)
            {
                MessageBox.Show("Loading file in progress, plase wait.", "Loading File");
                e.Cancel = true;
            }
        }
    }
}
