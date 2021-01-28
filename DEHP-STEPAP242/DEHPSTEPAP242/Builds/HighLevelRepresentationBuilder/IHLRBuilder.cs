
namespace DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder
{
    using DEHPSTEPAP242.ViewModel.Rows;
    using ReactiveUI;
    using STEP3DAdapter;
    using System.Collections.Generic;

    /// <summary>
    /// Helper class to create the High Level Representation (HLR) View Model for STEP AP242 file
    /// </summary>
    public interface IHLRBuilder
    {
        /// <summary>
        /// Creates the High Level Representation (HLR) View Model for STEP AP242 file
        /// </summary>
        List<Step3dRowViewModel> CreateHLR(STEP3DFile step3d);
    }
}