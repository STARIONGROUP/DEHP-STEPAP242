// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubFileStoreBrowserViewModel.cs" company="Open Engineering S.A.">
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
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.FileDialogService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPSTEPAP242.Events;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.Services.FileStoreService;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Wrapper class to display <see cref="FileRevision"/> of a STEP <see cref="File"/>
    /// </summary>
    public class HubFile
    {
        /// <summary>
        /// Gets the <see cref="FileRevision.Path"/>
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the <see cref="FileRevision.RevisionNumber"/>
        /// </summary>
        public int RevisionNumber { get; private set; }

        /// <summary>
        /// Gets the <see cref="FileRevision.CreatedOn"/>
        /// </summary>
        public DateTime CreatedOn { get; private set; }

        /// <summary>
        /// Gets the <see cref="Person"/> full name creator of this <see cref="FileRevision"/>
        /// </summary>
        public string CreatorFullName { get; private set; }

        /// <summary>
        /// The referenced <see cref="FileRevision"/>
        /// </summary>
        internal FileRevision FileRev { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> of this representation</param>
        public HubFile(FileRevision fileRevision)
        {
            FileRev = fileRevision;

            FilePath = FileRev.Path;
            RevisionNumber = FileRev.RevisionNumber;
            CreatedOn = FileRev.CreatedOn;
            CreatorFullName = $"{FileRev.Creator.Person.GivenName} {FileRev.Creator.Person.Surname.ToUpper()}";
        }
    }

    /// <summary>
    /// ViewModel for all the STEP files in the current <see cref="Iteration"/>
    /// and <see cref="EngineeringModel"/>.
    /// 
    /// Only last revisions are shown.
    /// </summary>
    public class HubFileStoreBrowserViewModel : ReactiveObject, IHubFileStoreBrowserViewModel
    {
        #region Private members

        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstHubService"/> instance
        /// </summary>
        private readonly IDstHubService dstHubService;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The <see cref="IFileStoreService"/> instance
        /// </summary>
        private readonly IFileStoreService fileStoreService;

        /// <summary>
        /// The <see cref="IOpenSaveFileDialogService"/>
        /// </summary>
        private readonly IOpenSaveFileDialogService fileDialogService;

        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool? isBusy;

        /// <summary>
        /// Gets or sets a value indicating whether the browser is busy
        /// </summary>
        public bool? IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        #endregion

        #region IHubFileBrowserViewModel interface

        /// <summary>
        /// Gets the collection of STEP file names in the current iteration and active domain
        /// </summary>
        public ReactiveList<HubFile> HubFiles { get; private set; }

        /// <summary>
        /// Backing field for <see cref="CurrentHubFile"/>
        /// </summary>
        private HubFile currentHubFile;

        /// <summary>
        /// Sets and gets selected <see cref="HubFile"/> from <see cref="HubFiles"/> list
        /// </summary>
        public HubFile CurrentHubFile
        {
            get => currentHubFile;
            set => this.RaiseAndSetIfChanged(ref this.currentHubFile, value);
        }

        /// <summary>
        /// Uploads one STEP-AP242 file to the <see cref="DomainFileStore"/> of the active domain
        /// </summary>
        public ReactiveCommand<object> UploadFileCommand { get; private set; }

        /// <summary>
        /// Downloads one STEP-AP242 file from the <see cref="DomainFileStore"/> of active domain into user choosen location
        ///
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<Unit> DownloadFileAsCommand { get; private set; }

        /// <summary>
        /// Downloads one STEP-AP242 file from the <see cref="DomainFileStore"/> of active domain into the local storage
        ///
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<Unit> DownloadFileCommand { get; private set; }

        /// <summary>
        /// Loads one STEP-AP242 file from the local storage
        /// 
        /// If files does not exist, it call <see cref="DownloadFileCommand"/> first.
        /// 
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<Unit> LoadFileCommand { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hubController"></param>
        /// <param name="statusBarControlView"></param>
        /// <param name="fileStoreService"></param>
        /// <param name="dstHubService"></param>
        /// <param name="fileDialogService"></param>
        public HubFileStoreBrowserViewModel(IHubController hubController, IStatusBarControlViewModel statusBarControlView,
            IFileStoreService fileStoreService, IDstHubService dstHubService,
            IOpenSaveFileDialogService fileDialogService)
        {
            this.hubController = hubController;
            this.statusBar = statusBarControlView;
            this.fileStoreService = fileStoreService;
            this.dstHubService = dstHubService;
            this.fileDialogService = fileDialogService;

            HubFiles = new ReactiveList<HubFile>();

            InitializeCommandsAndObservables();
        }

        #endregion

        #region Private/Protected methods

        /// <summary>
        /// Initializes the commands
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
            // Change on connection
            this.WhenAnyValue(x => x.hubController.OpenIteration).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateFileList());

            // Refresh/Transfer emits UpdateObjectBrowserTreeEvent (cache changed)
            CDPMessageBus.Current.Listen<UpdateObjectBrowserTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateFileList());

            // Ask for download
            CDPMessageBus.Current.Listen<DownloadFileRevisionEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x => await this.DownloadFileRevisionIdAs(x.TargetId));

            // Commands on selected FileRevision
            var fileSelected = this.WhenAny(
                vm => vm.CurrentHubFile,
                (x) => x.Value != null);

            this.LoadFileCommand = ReactiveCommand.CreateAsyncTask(fileSelected, async _ => await this.LoadFileCommandExecute());

            this.DownloadFileCommand = ReactiveCommand.CreateAsyncTask(fileSelected, async _ => await this.DownloadFileCommandExecute());

            this.DownloadFileAsCommand = ReactiveCommand.CreateAsyncTask(fileSelected, async _ => await this.DownloadFileAsCommandExecute());
        }

        /// <summary>
        /// Fills the list of STEP files
        /// 
        /// Files correspond to the current <see cref="Iteration"/> and current <see cref="DomainOfExpertise"/>
        /// and <see cref="EngineeringModel"/>.
        /// </summary>
        private void UpdateFileList()
        {
            if (!this.hubController.IsSessionOpen || this.hubController.OpenIteration is null)
            {
                CurrentHubFile = null;
                HubFiles.Clear();
                return;
            }

            this.IsBusy = true;

            var revisions = dstHubService.GetFileRevisions();

            List<HubFile> hubfiles = new List<HubFile>();

            foreach (var rev in revisions)
            {
                var item = new HubFile(rev);
                hubfiles.Add(item);
            }

            CurrentHubFile = null;
            HubFiles.Clear();
            HubFiles.AddRange(hubfiles);

            this.IsBusy = false;
        }

        /// <summary>
        /// Gets the <see cref="FileRevision"/> of the <see cref="CurrentHubFile"/>
        /// </summary>
        /// <returns>The <see cref="FileRevision"/></returns>
        private FileRevision CurrentFileRevision()
        {
            var frev = HubFiles.FirstOrDefault(x => x.FilePath == CurrentHubFile?.FilePath);

            if (frev is null)
            {
                return null;
            }

            return frev.FileRev;
        }

        /// <summary>
        /// Shows the <see cref="IOpenSaveFileDialogService.GetSaveFileDialog()"/> for a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> to be downloaded</param>
        /// <returns>The destination path, or null if cancelled by the user</returns>
        private string GetSaveFileDestination(FileRevision fileRevision)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(fileRevision.Path);
            var extension = System.IO.Path.GetExtension(fileRevision.Path);
            var filter = $"{extension.Replace(".", "").ToUpper()} files|*{extension}|All files (*.*)|*.*";

            var destinationPath = this.fileDialogService.GetSaveFileDialog(fileName, extension, filter, string.Empty, 1);
            return destinationPath;
        }

        /// <summary>
        /// Downloads the <see cref="FileRevision"/> into a file
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> to be downloaded</param>
        /// <param name="destinationPath">Full name path to file</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task DownloadFileRevision(FileRevision fileRevision, string destinationPath)
        {
            if (fileRevision is null)
            {
                return;
            }

            IsBusy = true;
            Application.Current.Dispatcher.Invoke(() => this.statusBar.Append("Downloading file from Hub..."));

            using (var fstream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
            {
                await hubController.Download(fileRevision, fstream);
            }

            Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Downloaded as: {destinationPath}"));
            IsBusy = false;
        }

        /// <summary>
        /// Downloads the <see cref="FileRevision"/> into destination choosen by the user
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> to be downloaded</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task DownloadFileRevisionAs(FileRevision fileRevision)
        {
            if (fileRevision is null)
            {
                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"The FileRevision is null", StatusBarMessageSeverity.Error));
                return;
            }

            var destinationPath = this.GetSaveFileDestination(fileRevision);
            if (destinationPath is null)
            {
                return;
            }

            await this.DownloadFileRevision(fileRevision, destinationPath);
        }

        /// <summary>
        /// Downloads the <see cref="FileRevision"/> from its <see cref="System.Guid"/> into destination choosen by the user
        /// </summary>
        /// <param name="guid"><see cref="System.Guid"/> value</param>
        /// <returns>A <see cref="Task"/></returns>
        /// <remarks>
        /// The <paramref name="guid"/> is validated as <see cref="System.Guid"/> pointing to a <see cref="FileRevision"/>
        /// in the current <see cref="DomainOfExpertise"/>
        /// </remarks>
        private async Task DownloadFileRevisionIdAs(string guid)
        {
            var fileRevision = this.dstHubService.FindFileRevision(guid);
            if (fileRevision is null)
            {
                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"The Guid {guid} did not correspond to a valid FileRevision", StatusBarMessageSeverity.Warning));
                return;
            }

            await this.DownloadFileRevisionAs(fileRevision);
        }

        /// <summary>
        /// Executes the <see cref="DownloadFileCommand"/> asynchronously.
        /// 
        /// File is downloaded from the Hub into destination choosen by the user.
        /// </summary>
        private async Task DownloadFileAsCommandExecute()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append("No current file selected to perform the download"));
                return;
            }

            await this.DownloadFileRevisionAs(fileRevision);
        }

        /// <summary>
        /// Executes the <see cref="DownloadFileCommand"/> asynchronously.
        /// 
        /// File is downloaded from the Hub and stored locally.
        /// <seealso cref="FileStoreService"/>.
        /// </summary>
        private async Task DownloadFileCommandExecute()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                Application.Current.Dispatcher.Invoke(() => statusBar.Append("No current file selected to perform the download"));
                return;
            }

            IsBusy = true;
            Application.Current.Dispatcher.Invoke(() => statusBar.Append("Downloading file from Hub..."));

            using (var fstream = fileStoreService.AddFileStream(fileRevision))
            {
                await hubController.Download(fileRevision, fstream);
            }

            Application.Current.Dispatcher.Invoke(() => statusBar.Append("Download successful"));
            IsBusy = false;
        }

        /// <summary>
        /// Executes the <see cref="LoadFileCommand"/> on <see cref="CurrentHubFile"/> asynchronously.
        /// </summary>
        /// <remarks>
        /// If file does not exists in the local cache <see cref="FileStoreService"/>, it is first downloaded from the Hub.
        /// 
        /// File is loaded by the current default application from the local storage <see cref="FileStoreService"/>.
        /// </remarks>
        private async Task LoadFileCommandExecute()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                Application.Current.Dispatcher.Invoke(() => statusBar.Append("No current file selected to perform the load"));
                return;
            }

            if (!fileStoreService.Exists(fileRevision))
            {
                await DownloadFileCommandExecute();
            }

            var destinationPath = fileStoreService.GetPath(fileRevision);

            IsBusy = true;
            Application.Current.Dispatcher.Invoke(() => statusBar.Append($"Loading from Hub: {destinationPath}"));

            bool openOK = this.OpenWithDefaultProgram(destinationPath);

            if (openOK)
            {
                Application.Current.Dispatcher.Invoke(() => statusBar.Append("Load successful"));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => statusBar.Append("Load failed", StatusBarMessageSeverity.Error));
            }

            IsBusy = false;
        }

        /// <summary>
        /// Opens a file using the default application defined in the system
        /// </summary>
        /// <param name="path">Full path to the file to be opened</param>
        /// <returns>True if the execution was performed</returns>
        private bool OpenWithDefaultProgram(string path)
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            return fileopener.Start();
        }

        #endregion
    }
}
