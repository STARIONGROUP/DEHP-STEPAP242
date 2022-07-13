// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstUserSettingsVieModel.cs" company="Open Engineering S.A.">
//     Copyright (c) 2021 Open Engineering S.A.
//
//     Author: Ivan Fontaine
//
//     Part of the code was based on the work performed by RHEA as result of the collaboration in
//     the context of "Digital Engineering Hub Pathfinder" by Sam Gerené, Alex Vorobiev, Alexander
//     van Delft and Nathanael Smiechowski.
//
//     This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
//
//     The DEHP STEP-AP242 is free software; you can redistribute it and/or modify it under the
//     terms of the GNU Lesser General Public License as published by the Free Software Foundation;
//     either version 3 of the License, or (at your option) any later version.
//
//     The DEHP STEP-AP242 is distributed in the hope that it will be useful, but WITHOUT ANY
//     WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
//     PURPOSE. See the GNU Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public License along with this
//     program; if not, write to the Free Software Foundation, Inc., 51 Franklin Street, Fifth
//     Floor, Boston, MA 02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using DEHPCommon.UserPreferenceHandler.UserPreferenceService;
using DEHPSTEPAP242.Settings;
using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
using DEHPSTEPAP242.Views.Dialogs;
using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    public class DstUserSettingsViewModel : ReactiveObject, IDstUserSettingsViewModel
    {
        /// <summary>
        /// The <see cref="IUserPreferenceService{AppSettings}"/> instance
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;

        /// Backing field for <see cref="FileStoreDirectoryName"/>
        private string fileStoreDirectoryName = "";

        /// <summary>
        /// The property used to store the subdirectory name for the filestore.
        /// </summary>
        public string FileStoreDirectoryName
        {
            get => this.fileStoreDirectoryName;
            set => this.RaiseAndSetIfChanged(ref this.fileStoreDirectoryName, value);
        }

        /// Backing field for <see cref="FileStoreCleanOnInt"/>
        private bool fileStoreCleanOnInit = false;

        /// <summary>
        /// Used to store the flag that controls the cleaning of the filestore sub directory on startup.
        /// </summary>
        public bool FileStoreCleanOnInit
        {
            get => this.fileStoreCleanOnInit;
            set => this.RaiseAndSetIfChanged(ref this.fileStoreCleanOnInit, value);
        }

        /// Backing field for <see cref="PathToStepViewer"/>
        private string pathToStepViewer = "";

        /// <summary>
        ///  Used to store the path to the user specified step viewer application.
        /// </summary>
        public string PathToStepViewer
        {
            get => this.pathToStepViewer;
            set => this.RaiseAndSetIfChanged(ref this.pathToStepViewer, value);
        }

        /// <summary>
        /// The Command that opens the file browse dialog box.
        /// </summary>

        public ReactiveCommand<object> SelectFileCommand { get; private set; }

        /**
         * <summary>
         * The Constructor.
         * </summary>
         */

        public DstUserSettingsViewModel(IUserPreferenceService<AppSettings> userPreferenceService)
        {
            this.userPreferenceService = userPreferenceService;

            // SPA: These two lines should be activated with DEHPCommon 10.0.281?
            //SelectFileCommand = ReactiveCommand.Create();
            //SelectFileCommand.Subscribe(_ => SelectFileCommandExecute());
        }

        /**
         * <summary>
         * Use to load the current data from the user preference file.
         * </summary>
        */

        public void ReadData()
        {
            AppSettings preferences = userPreferenceService.UserPreferenceSettings;
            this.FileStoreCleanOnInit = false;
            this.PathToStepViewer = "";
            this.FileStoreDirectoryName = "";
            this.userPreferenceService.Read();
            this.FileStoreCleanOnInit = preferences.FileStoreCleanOnInit;
            this.PathToStepViewer = preferences.PathToStepViewer;
            this.FileStoreDirectoryName = preferences.FileStoreDirectoryName;
        }

        /**
         * <summary>
         * Use to save the current data to the user preference file.
         * </summary>
        */

        public void WriteData()
        {
            AppSettings preferences = userPreferenceService.UserPreferenceSettings;
            preferences.FileStoreCleanOnInit = this.FileStoreCleanOnInit;
            preferences.PathToStepViewer = this.PathToStepViewer;
            preferences.FileStoreDirectoryName = this.FileStoreDirectoryName;
            this.userPreferenceService.Save();
        }

        /**
         * <summary>
         * This method is used for  the basic user settings dialogbox workflow.
         * (i.e. load the data from the user settings file, open the dialog box and update the file content if necessary)
         * </summary>
         */

        public void HandleUserSettings()
        {
            ReadData();

            DstUserSettings dlg = new()
            {
                DataContext = this
            };

            if (dlg.ShowDialog() == true)
            {
                WriteData();
            }
        }

        /**<summary>
         * This is the method used to allow the user to select the step viewer executable.
         * </summary>
         */

        protected void SelectFileCommandExecute()
        {
            OpenFileDialog dlg = new OpenFileDialog()
            {
                Filter = "Executables|*.exe|All types|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                PathToStepViewer = dlg.FileName;
            }
        }
    }
}