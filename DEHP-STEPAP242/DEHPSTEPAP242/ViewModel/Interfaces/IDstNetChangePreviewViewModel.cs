
namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    using DEHPSTEPAP242.ViewModel.NetChangePreview;

    /// <summary>
    /// Interface definition for the <see cref="DstNetChangePreviewViewModel"/>
    /// </summary>
    public interface IDstNetChangePreviewViewModel
    {
        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        void ComputeValues();
    }
}
