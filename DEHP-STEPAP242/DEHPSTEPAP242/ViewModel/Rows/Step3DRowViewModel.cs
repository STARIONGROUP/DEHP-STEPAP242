
namespace DEHPSTEPAP242.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;
    
    using ReactiveUI;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    
    using STEP3DAdapter;

    /// <summary>
    /// The <see cref="Step3DRowViewModel"/> is the node in the HLR tree structure.
    /// 
    /// <seealso cref="DstObjectBrowserViewModel"/>
    /// <seealso cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/>
    /// </summary>
    public class Step3DRowViewModel : ReactiveObject
    {
        internal STEP3D_Part part;
        internal STEP3D_PartRelation relation;

        #region HLR Tree Indexes

        /// <summary>
        /// Auxiliary index for tree control.
        /// </summary>
        /// <remarks>
        /// It is an unique value in the <see cref="Builds.HighLevelRepresentationBuilder.HighLevelRepresentationBuilder"/> context.
        /// </remarks>
        public int ID { get; set; }

        /// <summary>
        /// Auxiliary parent index for tree control.
        /// </summary>
        public int ParentID { get; set; }

        #endregion

        #region Part Fields

        /// <summary>
        /// Gets the part instance name
        /// </summary>
        /// <remarks>
        /// The instance is the part name and the usage id <see cref="STEP3D_PartRelation.id"/>
        /// representing a unique string for the part.
        /// </remarks>
        public string InstanceName { get; }

        /// <summary>
        /// Get full path of compised part instance names
        /// </summary>
        public string InstancePath { get; private set; }

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
        /// Using as label the <see cref="STEP3D_PartRelation.id"/> instead 
        /// <see cref="STEP3D_PartRelation.name"/> because it was the only unique value 
        /// exported by the different CAD applications tested during developments.
        /// </remarks>
        public string RelationLabel { get => relation?.id; }

        /// <summary>
        /// Gets the Get STEP entity file Id of the relation (NAUO)
        /// </summary>
        public string RelationId { get => $"{relation?.stepId}"; }

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

        /// <summary>
        /// Gets or sets the mapping configurations
        /// </summary>
        public ReactiveList<IdCorrespondence> MappingConfigurations { get; set; } = new ReactiveList<IdCorrespondence>();

        /// <summary>
        /// Cleans all the "Selected" fields
        /// </summary>
        public void CleanSelections()
        {
            this.SelectedElementDefinition = null;
            this.SelectedParameter = null;
            this.SelectedParameterType = null; //TODO: remove unsed
            this.SelectedElementUsages.Clear();
            this.SelectedOption = null;
            this.SelectedActualFiniteState = null;
        }

        /// <summary>
        /// Gets this represented ElementName
        /// </summary>
        public string ElementName => this.Name;

        /// <summary>
        /// Gets this reprensented ParameterName
        /// </summary>
        public string ParameterName => $"{this.Name} 3d geometry";

        /// <summary>
        /// Enumeration of the possible mapping status of the current part
        /// </summary>
        public enum MappingStatusType
        {
            /// <summary>
            /// Noting refers to no <see cref="IdCorrespondence"/> information about mapping for the part
            /// </summary>
            Nothing,

            /// <summary>
            /// WithConfiguration refers to a <see cref="IdCorrespondence"/> entries not yet used for the mapping process
            /// </summary>
            Configured,

            /// <summary>
            /// Mapped refers to an already mapped part, independently of current <see cref="IdCorrespondence"/> defined
            /// </summary>
            Mapped,

            /// <summary>
            /// Transfered refers to an already transfered part, independently of current <see cref="IdCorrespondence"/> defined
            /// </summary>
            Transfered
        }

        /// <summary>
        /// Gets the mapping status code
        /// </summary>
        public MappingStatusType MappingStatus { get; private set; }

        /// <summary>
        /// Backing field for <see cref="MappingStatusMessage"/>
        /// </summary>
        private string mappingStatusMessage;

        /// <summary>
        /// Gets the <see cref="MappingStatus"/> string representation
        /// </summary>
        public string MappingStatusMessage
        {
            get => this.mappingStatusMessage;
            private set => this.RaiseAndSetIfChanged(ref this.mappingStatusMessage, value);
        }

        /// <summary>
        /// Sets the <see cref="MappingStatus"/> and updates the <see cref="MappingStatusMessage"/>
        /// </summary>
        /// <param name="mappingStatusType"></param>
        private void SetMappingStatus(MappingStatusType mappingStatusType)
        {
            this.MappingStatus = mappingStatusType;

            switch (this.MappingStatus)
            {
                case MappingStatusType.Nothing:
                    this.MappingStatusMessage = string.Empty;
                    break;
                case MappingStatusType.Configured:
                    this.MappingStatusMessage = "Configured";
                    break;
                case MappingStatusType.Mapped:
                    this.MappingStatusMessage = "Mapped";
                    break;
                case MappingStatusType.Transfered:
                    this.MappingStatusMessage = "Transfered";
                    break;
                default:
                    // Not expected
                    this.MappingStatusMessage = string.Empty;
                break;
            }
        }

        /// <summary>
        /// Update the <see cref="MappingStatus"/> according to current situation
        /// </summary>
        /// <remarks>
        /// The <see cref="MappingStatusType.Nothing"/> or <see cref="MappingStatusType.Configured"/> status can
        /// be changed between them at any time using the <see cref="MappingConfigurations"/> content.
        /// 
        /// Once the status is set to <see cref="MappingStatusType.Mapped"/> or <see cref="MappingStatusType.Transfered"/>
        /// the <see cref="MappingStatusType.Nothing"/> or <see cref="MappingStatusType.Configured"/> status cannot be set.
        /// 
        /// The <see cref="MappingStatusType.Mapped"/> status should remain untouchable until the transfer is executed
        /// or the current pending mappings are removed.
        /// 
        /// <seealso cref="ResetMappingStatus"/>
        /// </remarks>
        public void UpdateMappingStatus()
        {
            // On these no change is informed
            if (this.MappingStatus >= MappingStatusType.Mapped)
            {
                return;
            }

            // Only possible change
            if (this.MappingConfigurations.Count == 0)
            {
                this.SetMappingStatus(MappingStatusType.Nothing);
            }
            else
            {
                this.SetMappingStatus(MappingStatusType.Configured);
            }
        }

        /// <summary>
        /// Sets the <see cref="MappingStatusType.Mapped"/> status
        /// </summary>
        public void SetMappedStatus()
        {
            this.SetMappingStatus(MappingStatusType.Mapped);
        }

        /// <summary>
        /// Sets the <see cref="MappingStatusType.Transfered"/> status
        /// </summary>
        public void SetTransferedStatus()
        {
            this.SetMappingStatus(MappingStatusType.Transfered);
        }

        /// <summary>
        /// Sets the <see cref="MappingStatusType.Nothing"/> status
        /// independently of  <see cref="MappingConfigurations"/>
        /// </summary>
        public void ResetMappingStatus()
        {
            this.SetMappingStatus(MappingStatusType.Nothing);
        }

#endregion

#region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="part">Reference to <see cref="STEP3D_Part"/> entity in the <see cref="STEP3DFile"/></param>
        /// <param name="relation">Reference to <see cref="STEP3D_Part"/> entity in the <see cref="STEP3DFile"/></param>
        public Step3DRowViewModel(STEP3D_Part part, STEP3D_PartRelation relation, string parentPath = "")
        {
            this.part = part;
            this.relation = relation;

            this.InstanceName = string.IsNullOrWhiteSpace(this.RelationLabel) ? this.Name : $"{this.Name} ({this.RelationLabel})";
            this.InstancePath = string.IsNullOrWhiteSpace(parentPath) ? this.InstanceName : $"{parentPath}.{this.InstanceName}";

            this.ResetMappingStatus();

            //TODO: not working as expected
            //this.WhenAnyValue(x => x.MappingConfigurations).Subscribe(x => this.UpdateMappingStatus());
        }

#endregion
    }
}
