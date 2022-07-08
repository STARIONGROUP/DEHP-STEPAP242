// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UndeterminateProgressBar.cs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
//
//    Author: Ivan Fontaine
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
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows;

namespace DEHPSTEPAP242.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for UndeterminateProgressBar.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class UndeterminateProgressBar : Window
    { 
        /** <summary>
         * The constructor
         * </summary>
         */
        public UndeterminateProgressBar()
        {            
            InitializeComponent();
        }
        /** <summary>
         * The background worker use to change the progress value to animate the progressbar
         * </summary>
         */
        private void Worker_Work(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(100);
            }
        }
        /**<summary>
         * Method used to handle the progress changed event.
         * </summary>
         */
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbStatus.Value = e.ProgressPercentage;
        }
        /**<summary>
         * Here we create the background worker that will keep the progressbar animated
         * </summary>
         */
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_Work;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerAsync();
            this.Topmost = false;
            this.Activate();
        }
    }
}