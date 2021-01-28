
namespace DEHPSTEPAP242.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;
    using ReactiveUI;
    using STEP3DAdapter;

    /// <summary>
    /// The <see cref="Step3dRowViewModel"/> is the node in the HLR tree structure.
    /// 
    /// <seealso cref="DstObjectBrowserViewModel"/>
    /// </summary>
    public class Step3dRowViewModel : ReactiveObject
    {
        internal STEP3D_Part part;
        internal STEP3D_PartRelation relation;

        #region HLR Tree Indexes

        /// <summary>
        /// Auxiliary index for tree control.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Auxiliary parent index for tree control.
        /// </summary>
        public int ParentID { get; set; }

        #endregion

        #region Part Fields

        /// <summary>
        /// Get Part name.
        /// </summary>
        public string Name { get => part.name; }

        /// <summary>
        /// Get short entity type.
        /// </summary>
        public string Type { get => part.type; }

        /// <summary>
        /// Get STEP entity type.
        /// </summary>
        public string RepresentationType { get => part.representation_type; }

        /// <summary>
        /// Get STEP entity file Id.
        /// </summary>
        public int StepId { get => part.stepId; }

        /// <summary>
        /// Compose a reduced description of the Part.
        /// </summary>
        public string Description
        {
            get => $"{part.type}#{part.stepId} '{part.name}'";
        }

        // TODO: add information about the Relation

        public string RelationLabel { get => relation?.id; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">Reference to STEP 3D part entity from the controller</param>
        public Step3dRowViewModel(STEP3D_Part part, STEP3D_PartRelation relation)
        {
            this.part = part;
            this.relation = relation;
        }

        #endregion
    }
}
