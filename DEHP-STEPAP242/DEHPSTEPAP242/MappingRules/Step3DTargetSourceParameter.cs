
namespace DEHPSTEPAP242.MappingRules
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHPSTEPAP242.ViewModel.Rows;

    /// <summary>
    /// Helper class which keeps a reference to the <see cref="ValueArray{string}"/> 
    /// that needs to me updated with the new <see cref="FileRevision"/> of the source
    /// STEP 3D file in the Hub.
    /// </summary>
    public class Step3DTargetSourceParameter
    {
        /// <summary>
        /// The <see cref="Step3DRowViewModel"/> originating the change
        /// </summary>
        public readonly Step3DRowViewModel part;

        /// <summary>
        /// The <see cref="ValueArray{string}"/> of the <see cref="IValueSet"/> of interest
        /// </summary>
        private readonly ValueArray<string> values;

        /// <summary>
        /// The index in the <see cref="ValueArray{string}"/> for the <see cref="ParameterTypeComponent"/> corresponding to the "source" field
        /// </summary>
        private readonly int componentIndex;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/></param>
        /// <param name="values">The <see cref="ValueArray{string}"/> of the target <see cref="IValueSet"/></param>
        /// <param name="componentIndex">The index corresponding to the source field</param>
        public Step3DTargetSourceParameter(Step3DRowViewModel part, ValueArray<string> values, int componentIndex)
        {
            this.part = part;
            this.values = values;
            this.componentIndex = componentIndex;
        }

        /// <summary>
        /// Updates the <see cref="ValueArray{string}"/> associated to the source parameter
        /// </summary>
        /// <param name="fileRevision"></param>
        public void UpdateSource(FileRevision fileRevision)
        {
            this.values[componentIndex] = fileRevision.Iid.ToString();
        }
    }
}
