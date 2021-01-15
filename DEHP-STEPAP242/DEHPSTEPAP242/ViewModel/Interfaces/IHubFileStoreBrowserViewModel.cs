using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Gets the collection of STEP file names in the current iteration
        /// </summary>
        public ReactiveList<HubFile> HubFiles { get; }

        /// <summary>
        /// Download STEP-AP242 file from the Hub into a local storage
        /// </summary>
        ReactiveCommand<object> DownloadFileCommand { get; }

        /// <summary>
        /// Load STEP-AP242 file from the Hub
        /// </summary>
        ReactiveCommand<object> LoadFileCommand { get; }
    }
}
