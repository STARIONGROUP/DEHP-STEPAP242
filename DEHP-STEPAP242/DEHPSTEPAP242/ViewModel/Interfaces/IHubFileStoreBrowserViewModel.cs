namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// Definition of methods and properties of <see cref="HubFileStoreBrowserViewModel"/>
    /// </summary>
    public interface IHubFileStoreBrowserViewModel
    {
        /// <summary>
        /// Gets the collection of STEP file names in the current iteration and active domain
        /// </summary>
        public ReactiveList<HubFile> HubFiles { get; }

        /// <summary>
        /// Sets and gets selected <see cref="HubFile"/> from <see cref="HubFiles"/> list
        /// </summary>
        public HubFile CurrentHubFile { get; set; }

        /// <summary>
        /// Uploads one STEP-AP242 file to the <see cref="DomainFileStore"/> of the active domain
        /// </summary>
        ReactiveCommand<object> UploadFileCommand { get; }

        /// <summary>
        /// Downloads one STEP-AP242 file from the <see cref="DomainFileStore"/> of active domain into the local storage
        /// </summary>
        ReactiveCommand<object> DownloadFileCommand { get; }

        /// <summary>
        /// Loads one STEP-AP242 file from the local storage
        /// </summary>
        ReactiveCommand<object> LoadFileCommand { get; }
    }
}
