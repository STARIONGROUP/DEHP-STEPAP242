
namespace DEHPSTEPAP242.ViewModel.Dialogs.Interfaces
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.UserInterfaces.Behaviors;

    using DEHPSTEPAP242.ViewModel.Rows;

    //using DevExpress.Xpf.Editors;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="MappingConfigurationDialogViewModel"/>
    /// </summary>
    public interface IMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        bool IsBusy { get; set; }

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="Step3dRowViewModel"/>
        /// </summary>
        Step3dRowViewModel SelectedThing { get; set; }

        /// <summary>
        /// Gets the collection of the available <see cref="Option"/> from the connected Hub Model
        /// </summary>
        ReactiveList<Option> AvailableOptions { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ElementDefinition"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ElementDefinition> AvailableElementDefinitions { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ElementUsage"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ElementUsage> AvailableElementUsages { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ParameterType"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ParameterType> AvailableParameterTypes { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="Parameter"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<Parameter> AvailableParameters { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; }

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        ReactiveList<Step3dRowViewModel> Variables { get; }

        /// <summary>
        /// Gets the <see cref="ICommand"/> to continue
        /// </summary>
        ReactiveCommand<object> ContinueCommand { get; set; }
    }
}
