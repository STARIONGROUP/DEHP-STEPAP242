
namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    
    using ReactiveUI;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.Services.FileStoreService;
    using DEHPSTEPAP242.Services.DstHubService;

    /// <summary>
    /// Wrapper class to display <see cref="FileRevision"/>
    /// </summary>
    public class HubFile
    {
        public string FileName { get; private set; }
        public int RevisionNumber { get; private set; }
        public DateTime ModifiedOn { get; private set; }

        //		public string CreatorName { get; private set; }
        //		public string CreatorSurname { get; private set; }
        public string CreatorFullName { get; private set; }

        internal FileRevision FileRev;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/> of this representation</param>
        public HubFile(FileRevision fileRevision)
        {
            FileRev = fileRevision;

            FileName = FileRev.Path;
            RevisionNumber = FileRev.RevisionNumber;
            ModifiedOn = FileRev.ModifiedOn;
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
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControlView;

        private readonly IFileStoreService fileStoreService;
        
        private readonly IDstHubService dstHubService;

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
        public ReactiveList<HubFile> HubFiles
        {
            get; private set;
        }

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
        /// Downloads one STEP-AP242 file from the <see cref="DomainFileStore"/> of active domain into the local storage
        ///
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<object> DownloadFileCommand { get; private set; }

        /// <summary>
        /// Loads one STEP-AP242 file from the local storage
        /// 
        /// If files does not exist, it call <see cref="DownloadFileCommand"/> first.
        /// 
        /// Uses the <see cref="CurrentHubFile"/> value.
        /// </summary>
        public ReactiveCommand<object> LoadFileCommand { get; private set; }

        #endregion

        #region Constructor

        public HubFileStoreBrowserViewModel(IHubController hubController, IStatusBarControlViewModel statusBarControlView, IFileStoreService fileStoreService, IDstHubService dstHubService)
        {
            this.hubController = hubController;
            this.statusBarControlView = statusBarControlView;
            this.fileStoreService = fileStoreService;
            this.dstHubService = dstHubService;

            HubFiles = new ReactiveList<HubFile>();

            this.WhenAnyValue(x => x.hubController.OpenIteration).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    this.IsBusy = true;

                    if (this.hubController.IsSessionOpen && this.hubController.OpenIteration != null)
                    {
                        //this.ToolTip = $"{this.hubController.Session.DataSourceUri}\n{this.hubController.Session.ActivePerson.Name}";
                        this.UpdateFileList();
                    }
                    else
                    {
                        HubFiles.Clear();
                    }

                    this.IsBusy = false;
                });

            InitializeCommands();
        }

        #endregion

        #region Private/Protected methods

        /// <summary>
        /// Initializes the commands
        /// </summary>
        private void InitializeCommands()
        {
            /* null comparison is not accepted by WhenAnyValue() */
            //
            //var fileSelected = this.WhenAnyValue(
            //	vm => vm.CurrentHubFile,
            //	(x) => x != null);

            //var fileSelected = this.WhenAnyValue(
            //	vm => vm.CurrentHubFile,
            //	(x) => { return (x != null); });

            var fileSelected = this.WhenAnyValue(
                vm => vm.CurrentHubFile,
                (x) => IsValidCurrentHubFile(x));
            
            LoadFileCommand = ReactiveCommand.Create(fileSelected);
            LoadFileCommand.Subscribe(_ => LoadFileCommandExecuteAsync());

            DownloadFileCommand = ReactiveCommand.Create(fileSelected);
            DownloadFileCommand.Subscribe(_ => DownloadFileCommandExecuteAsync());
        }

        /// <summary>
        /// Fills the list of STEP files
        /// 
        /// Files correspond to the current <see cref="Iteration"/> and current <see cref="DomainOfExpertise"/>
        /// and <see cref="EngineeringModel"/>.
        /// </summary>
        private void UpdateFileList()
        {
            Debug.WriteLine("UpdateFileList:");

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

            foreach(var i in HubFiles)
            {
                Debug.WriteLine($">>> HF {i.FileName}");
            }
        }

        /// <summary>
        /// Helper method to check if object is null
        /// 
        /// WhenAnyValue() does not accept "x != null" in the body method.
        /// </summary>
        /// <param name="x"></param>
        /// <returns>True if is not null</returns>
        private bool IsValidCurrentHubFile(HubFile x)
        {
            return !(x == null);
        }

        /// <summary>
        /// Gets the <see cref="FileRevision"/> of the <see cref="CurrentHubFile"/>
        /// </summary>
        /// <returns>The <see cref="FileRevision"/></returns>
        private FileRevision CurrentFileRevision()
        {
            var frev = HubFiles.FirstOrDefault(x => x.FileName == CurrentHubFile?.FileName);

            if (frev is null)
            {
                return null;
            }

            return frev.FileRev;
        }

        /// <summary>
        /// Executes the <see cref="DownloadFileCommand"/> asynchronously.
        /// 
        /// File is downloaded from the Hub and stored locally.
        /// <seealso cref="FileStoreService"/>.
        /// </summary>
        protected async Task DownloadFileCommandExecuteAsync()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                statusBarControlView.Append("No current file selected to perform the download");
                return;
            }

            IsBusy = true;
            statusBarControlView.Append("Downloading file from Hub...");

            using (var fstream = fileStoreService.AddFileStream(fileRevision))
            {
                await hubController.Download(fileRevision, fstream);
            }

            statusBarControlView.Append("Download successful");

            IsBusy = false;
        }

        /// <summary>
        /// Executes the <see cref="LoadFileCommand"/> asynchronously.
        /// 
        /// File is loaded from the local storage <see cref="FileStoreService"/>.
        /// If file does not exists, it is first downloaded.
        /// </summary>
        protected async Task LoadFileCommandExecuteAsync()
        {
            var fileRevision = CurrentFileRevision();
            if (fileRevision is null)
            {
                statusBarControlView.Append("No current file selected to perform the load");
                return;
            }
            
            if (fileStoreService.Exists(fileRevision) == false)
            {
                await DownloadFileCommandExecuteAsync();
            }

            IsBusy = true;

            var destinationPath = fileStoreService.GetPath(fileRevision);

            statusBarControlView.Append($"Loading from Hub: {destinationPath}");

            // TODO: load STEP file into a View

            statusBarControlView.Append("Load successful");

            IsBusy = false;
        }

        #endregion
    }
}
