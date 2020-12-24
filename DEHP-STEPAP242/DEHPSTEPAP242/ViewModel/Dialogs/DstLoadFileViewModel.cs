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
    using System.IO;

    using Microsoft.Win32;
    using ReactiveUI;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Settings;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;


    /// <summary>
    /// The view-model for the Login that allows users to open a STEP-AP242 file.
    /// </summary>
    public class DstLoadFileViewModel : ReactiveObject, IDstLoadFileViewModel, ICloseWindowViewModel
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
        /// The <see cref="IUserPreferenceService{AppSettings}"/> instance
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        //public int WindowWidth { get; set; }
        //public int WindowHeight { get; set; }

        private string filePath;

        /// <summary>
        /// Gets or sets the current path to a STEP file.
        /// </summary>
        public string FilePath 
        { 
            get => filePath;
            set => this.RaiseAndSetIfChanged(ref this.filePath, value);
        }

        private bool loadingFile;

        /// <summary>
        /// Gets or sets the current loading task status.
        /// </summary>
        public bool IsLoadingFile
        {
            get => loadingFile;
            private set => this.RaiseAndSetIfChanged(ref this.loadingFile, value);
        }

        /// <summary>
        /// List of recent opened STEP files.
        /// </summary>
        public ReactiveList<string> RecentFiles { get; private set; } = new ReactiveList<string> { ChangeTrackingEnabled = true };

        /// <summary>
        /// Load STEP-AP242 file from the local machine.
        /// 
        /// <seealso cref="FilePath"/>
        /// </summary>
        public ReactiveCommand<object> SelectFileCommand { get; private set; }

        /// <summary>
        /// Loads the current <see cref="FilePath"/> and closes the window.
        /// </summary>
        public ReactiveCommand<object> LoadFileCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DstLoadFileViewModel"/> class.
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlView">The <see cref="IStatusBarControlViewModel"/></param>
        public DstLoadFileViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlView, IUserPreferenceService<AppSettings> userPreferenceService)
        {
            this.dstController = dstController;
            this.statusBarControlView = statusBarControlView;
            this.userPreferenceService = userPreferenceService;

            PopulateRecentFiles();
            InitializeCommands();

            if (RecentFiles.IsEmpty == false)
            {
                // Initialize using the last opened file
                FilePath = RecentFiles[0];
            }

            //WindowHeight = 500;
            //WindowWidth = 500;
        }

        /// <summary>
        /// Instantiates the commands.
        /// </summary>
        private void InitializeCommands()
        {
            SelectFileCommand = ReactiveCommand.Create();
            SelectFileCommand.Subscribe(_ => SelectFileCommandExecute());

            // Load File button is activated when the FilePath points to a existing file
            var canLoadFile = this.WhenAnyValue(
                vm => vm.FilePath,
                vm => vm.IsLoadingFile,
                (fn, loading) => File.Exists(fn) && !loading);

            LoadFileCommand = ReactiveCommand.Create(canLoadFile);
            LoadFileCommand.Subscribe(_ => LoadFileCommandExecuteAsync());
        }

        /// <summary>
        /// Executes the <see cref="SelectFileCommand"/>
        /// </summary>
        protected void SelectFileCommandExecute()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "STEP-AP242|*.step;*.stp|All types|*.*",
                InitialDirectory = Path.GetDirectoryName(FilePath)
            };

            if (dlg.ShowDialog() == true)
            {
                FilePath = dlg.FileName;
            }
        }

        /// <summary>
        /// Executes the <see cref="LoadFileCommand"/>
        /// </summary>
        protected void LoadFileCommandExecute()
        {
            IsLoadingFile = true;
            statusBarControlView.Append("Loading file...");

            dstController.Load(FilePath);

            IsLoadingFile = false;

            if (dstController.IsFileOpen)
            {
                statusBarControlView.Append("Load successful");

                AddToRecentFiles(FilePath);
                SaveRecentFiles();

                CloseWindowBehavior?.Close();
            }
            else
            {
                statusBarControlView.Append($"Load failed: {dstController.Step3DFile.ErrorMessage}", StatusBarMessageSeverity.Error);
            }
        }

        protected async void LoadFileCommandExecuteAsync()
        {
            IsLoadingFile = true; 
            statusBarControlView.Append("Loading file...");
            
            await dstController.LoadAsync(FilePath);

            IsLoadingFile = false;

            if (dstController.IsFileOpen)
            {
                statusBarControlView.Append("Load successful");

                AddToRecentFiles(FilePath);
                SaveRecentFiles();

                CloseWindowBehavior?.Close();
            }
            else
            {
                statusBarControlView.Append($"Load failed: {dstController.Step3DFile.ErrorMessage}", StatusBarMessageSeverity.Error);
            }
        }

        /// <summary>
        /// Loads the saved recent files into the <see cref="RecentFiles"/>
        /// </summary>
        private void PopulateRecentFiles()
        {
            userPreferenceService.Read();
            RecentFiles.Clear();
            RecentFiles.AddRange(this.userPreferenceService.UserPreferenceSettings.RecentFiles);
        }

        /// <summary>
        /// Adds a file name to the <see cref="RecentFiles"/> list.
        /// 
        /// It behaves as a stack: the last used in the first in the list (more recent).
        /// 
        /// Duplicated entries are silently ignored.
        /// </summary>
        /// <param name="filename">Full path to a file</param>
        private void AddToRecentFiles(string filename)
        {
            // First files are the latest used
            if (RecentFiles.Contains(filename))
            {
                RecentFiles.Remove(filename);
            }

            RecentFiles.Insert(0, filename);
        }

        /// <summary>
        /// Updates and saves the recent files into the <see cref="RecentFiles"/>
        /// </summary>
        private void SaveRecentFiles()
        {
            userPreferenceService.UserPreferenceSettings.RecentFiles.Clear();
            userPreferenceService.UserPreferenceSettings.RecentFiles.AddRange(RecentFiles);

            userPreferenceService.Save();
        }


        /// <summary>
        /// Executes login command
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        //private async Task ExecuteLogin()
        //{
        //    this.statusBarControlView.Append("Loggin in...");
        //
        //    try
        //    {
        //        var credentials = this.RequiresAuthentication ? new UserIdentity(this.UserName, this.Password) : null;
        //        await this.dstController.Connect(this.Uri, true, credentials);
        //        this.LoginSuccessfull = this.dstController.IsSessionOpen;
        //
        //        if (this.LoginSuccessfull)
        //        {
        //            this.statusBarControlView.Append("Loggin successful");
        //            await Task.Delay(1000);
        //            this.CloseWindowBehavior?.Close();
        //        }
        //        else
        //        {
        //            this.statusBarControlView.Append($"Loggin failed", StatusBarMessageSeverity.Info);
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        this.statusBarControlView.Append($"Loggin failed: {exception.Message}", StatusBarMessageSeverity.Error);
        //    }
        //}
    }
}
