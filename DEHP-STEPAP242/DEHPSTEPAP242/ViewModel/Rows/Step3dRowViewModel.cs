
namespace DEHPSTEPAP242.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;
    
    using ReactiveUI;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    
    using STEP3DAdapter;

    /// <summary>
    /// The <see cref="Step3dRowViewModel"/> is the node in the HLR tree structure.
    /// 
    /// <seealso cref="DstObjectBrowserViewModel"/>
    /// <seealso cref="Builds.HighLevelRepresentationBuilder.HLRBuilder"/>
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
        /// Compose a reduced description of the <see cref="STEP3D_Part"/>
        /// </summary>
        public string Description
        {
            get => $"{part.type}#{part.stepId} '{part.name}'";
        }

        /// <summary>
        /// Gets a label of association
        /// </summary>
        /// <remarks>
        /// Using as label the <see cref="STEP3D_PartRelation.id"/> because 
        /// it was the only unique value exported by the different 
        /// CAD applications tested.
        /// </remarks>
        public string RelationLabel { get => relation?.id; }

        /// <summary>
        /// Gets the Get STEP entity file Id of the relation (NAUO)
        /// </summary>
        /// </remarks>
        public string RelationId { get => relation?.id; }

        #endregion

        #region Mapping parameters

        /// <summary>
        /// Backing field for <see cref="SelectedOption"/>
        /// </summary>
        private Option selectedOption;

        /// <summary>
        /// Gets or sets the selected <see cref="Option"/>
        /// </summary>
        public Option SelectedOption
        {
            get => this.selectedOption;
            set => this.RaiseAndSetIfChanged(ref this.selectedOption, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedParameter"/>
        /// </summary>
        private Parameter selectedParameter;

        /// <summary>
        /// Gets or sets the selected <see cref="Parameter"/>
        /// </summary>
        public Parameter SelectedParameter
        {
            get => this.selectedParameter;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameter, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedParameterType"/>
        /// </summary>
        private ParameterType selectedParameterType;

        /// <summary>
        /// Gets or sets the selected <see cref="Parameter"/>
        /// </summary>
        public ParameterType SelectedParameterType
        {
            get => this.selectedParameterType;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameterType, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedElementDefinition"/>
        /// </summary>
        private ElementDefinition selectedElementDefinition;

        /// <summary>
        /// Gets or sets the selected <see cref="ElementDefinition"/>
        /// </summary>
        public ElementDefinition SelectedElementDefinition
        {
            get => this.selectedElementDefinition;
            set => this.RaiseAndSetIfChanged(ref this.selectedElementDefinition, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedActualFiniteState"/>
        /// </summary>
        private ActualFiniteState selectedActualFiniteState;

        /// <summary>
        /// Gets or sets the selected <see cref="ActualFiniteState"/>
        /// </summary>
        public ActualFiniteState SelectedActualFiniteState
        {
            get => this.selectedActualFiniteState;
            set => this.RaiseAndSetIfChanged(ref this.selectedActualFiniteState, value);
        }

        /// <summary>
        /// Gets or sets the collection of selected <see cref="ElementUsage"/>s
        /// </summary>
        public ReactiveList<ElementUsage> SelectedElementUsages { get; set; } = new ReactiveList<ElementUsage>();

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">Reference to <see cref="STEP3D_Part"/> entity in the <see cref="STEP3DFile"/></param>
        /// <param name="relation">Reference to <see cref="STEP3D_Part"/> entity in the <see cref="STEP3DFile"/></param>
        public Step3dRowViewModel(STEP3D_Part part, STEP3D_PartRelation relation)
        {
            this.part = part;
            this.relation = relation;
        }

        #endregion
    }
}
