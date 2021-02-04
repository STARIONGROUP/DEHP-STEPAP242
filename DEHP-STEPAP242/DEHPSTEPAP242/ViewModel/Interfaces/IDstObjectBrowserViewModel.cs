

namespace DEHPSTEPAP242.ViewModel.Interfaces
{
    using System.Collections.Generic;

    using DEHPSTEPAP242.ViewModel.Rows;

    ///	<summary>
    /// Interface definition for <see cref="DstObjectBrowserViewModel"/> is the
    /// High Level Representation (aka HLR) of a STEP-AP242 file.
    /// </summary>
    public interface IDstObjectBrowserViewModel
    {
        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        bool IsBusy { get; set; }

        /// <summary>
        /// Self-referential data source content.
        /// 
        /// Using the following service columns:
        /// - Key Field --> Step3DPartTreeNode.ID
        /// - Parent Field --> Step3DPartTreeNode.ParentID
        /// </summary>
        List<Step3dRowViewModel> Step3DHLR { get; }

        /// <summary>
        /// Create the HLR tree from the Parts/Relations.
        /// </summary>
        /// <param name="parts">List of geometric parts</param>
        /// <param name="relations">List of part relations defining instances in the tree composition</param>
        void UpdateHLR();
    }
}
