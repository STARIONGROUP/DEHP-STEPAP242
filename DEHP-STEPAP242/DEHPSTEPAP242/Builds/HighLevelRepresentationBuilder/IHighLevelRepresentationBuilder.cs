
namespace DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder
{
    using System.Collections.Generic;

    using DEHPSTEPAP242.ViewModel.Rows;

    using STEP3DAdapter;

    /// <summary>
    /// Helper class to create the High Level Representation (HLR) View Model for STEP AP242 file
    /// </summary>
    public interface IHighLevelRepresentationBuilder
    {
        /// <summary>
        /// Creates the High Level Representation (HLR) View Model for STEP AP242 file
        /// </summary>
        List<Step3DRowViewModel> CreateHLR(STEP3DFile step3d);
    }
}
