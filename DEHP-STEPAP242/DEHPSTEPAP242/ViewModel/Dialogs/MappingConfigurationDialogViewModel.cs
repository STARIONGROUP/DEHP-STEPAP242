
namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Input;

    using ReactiveUI;

    using DevExpress.Mvvm.Native;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Services.DstHubService;
    using System.Diagnostics;


    /// <summary>
    /// The <see cref="MappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping
    /// </summary>
    public class MappingConfigurationDialogViewModel : ReactiveObject, IMappingConfigurationDialogViewModel, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedThing"/>
        /// </summary>
        private Step3dRowViewModel selectedThing;

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        public Step3dRowViewModel SelectedThing
        {
            get => this.selectedThing;
            set => this.RaiseAndSetIfChanged(ref this.selectedThing, value);
        }

        /// <summary>
        /// Gets the collection of the available <see cref="Option"/> from the connected Hub Model
        /// </summary>
        public ReactiveList<Option> AvailableOptions { get; } = new ReactiveList<Option>();

        /// <summary>
        /// Gets the collection of the available <see cref="ElementDefinition"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ElementDefinition> AvailableElementDefinitions { get; } = new ReactiveList<ElementDefinition>();

        /// <summary>
        /// Gets the collection of the available <see cref="ElementUsage"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ElementUsage> AvailableElementUsages { get; } = new ReactiveList<ElementUsage>();

        /// <summary>
        /// Gets the collection of the available <see cref="ParameterType"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ParameterType> AvailableParameterTypes { get; } = new ReactiveList<ParameterType>();

        /// <summary>
        /// Gets the collection of the available <see cref="Parameter"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<Parameter> AvailableParameters { get; } = new ReactiveList<Parameter>();
        
        /// <summary>
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        public ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; } = new ReactiveList<ActualFiniteState>();

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        public ReactiveList<Step3dRowViewModel> Variables { get; } = new ReactiveList<Step3dRowViewModel>();

        /// <summary>
        /// Gets the <see cref="ICommand"/> to continue
        /// </summary>
        public ReactiveCommand<object> ContinueCommand { get; set; }

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService"/></param>
        public MappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController, IDstHubService dstHubService)
        {
            this.hubController = hubController;
            this.dstController = dstController;
            this.dstHubService = dstHubService;
            this.UpdateProperties();
            this.InitializesCommandsAndObservableSubscriptions();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            //var canContinue = this.WhenAnyValue(x => x.SelectedThing.SelectedValues.CountChanged)
            //    .SelectMany(x => x.Select(c => c > 0)).ObserveOn(RxApp.MainThreadScheduler);

            //var canContinue = this.WhenAnyValue(x => /*x.SelectedThing != null*/ true);

            //this.Variables.ForEach(x => canContinue.Merge(
            //    x.WhenAny(v => v.SelectedValues, v => v.Value.Any())
            //        .ObserveOn(RxApp.MainThreadScheduler)));

            this.ContinueCommand = ReactiveCommand.Create(/*canContinue*/);
            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand());

            this.WhenAnyValue(x => x.SelectedThing.SelectedOption)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.WhenAnyValue(x => x.SelectedThing.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(() =>
                {
                    this.UpdateAvailableParameters();
                    this.UpdateAvailableElementUsages();
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.UpdateAvailableActualFiniteStates));
        }

        /// <summary>
        /// Executes the specified action to update the view Hub fields surrounded by a <see cref="IsBusy"/> state change
        /// </summary>
        /// <param name="updateAction">The <see cref="Action"/> to execute</param>
        private void UpdateHubFields(Action updateAction)
        {
            this.IsBusy = true;
            updateAction.Invoke();
            this.IsBusy = false;
        }

        /// <summary>
        /// Executes the <see cref="ContinueCommand"/> to create the proper mapping
        /// </summary>
        private void ExecuteContinueCommand()
        {
            this.IsBusy = true;

            try
            {
                this.dstController.Map(this.SelectedThing);
                this.CloseWindowBehavior?.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.IsBusy = true;
            this.UpdateAvailableOptions();
            this.AvailableElementDefinitions.Clear();
            this.AvailableParameterTypes.Clear();
            this.AvailableElementDefinitions.AddRange(this.hubController.OpenIteration.Element.Where(this.AreTheseOwnedByTheDomain<ElementDefinition>()));

            // EXAMPLE CODE quering from chained Rdls
            //var paramList = this.hubController.GetSiteDirectory().AvailableReferenceDataLibraries()
            //            .SelectMany(x => x.QueryParameterTypesFromChainOfRdls())
            //            .Where(x => this.dstHubService.IsSTEPParameterType(x));
            //
            //foreach (var item in paramList)
            //{
            //    Debug.WriteLine($"step param: {item.Name}/{item.ShortName}");
            //}

            // Parameter Types are stored in a specific RDL (see this service to change the target)
            var rdl = this.dstHubService.GetReferenceDataLibrary();

            this.AvailableParameterTypes.AddRange(rdl.ParameterType.Where(
                x => this.dstHubService.IsSTEPParameterType(x))
                .OrderBy(x => x.Name));

            this.UpdateAvailableParameters();
            this.UpdateAvailableElementUsages();
            this.UpdateAvailableActualFiniteStates();

            this.IsBusy = false;
        }

        /// <summary>
        /// Updates the <see cref="AvailableActualFiniteStates"/>
        /// </summary>
        private void UpdateAvailableActualFiniteStates()
        {
            this.AvailableActualFiniteStates.Clear();

            if (this.SelectedThing?.SelectedParameter is { } parameter && parameter.StateDependence is { } stateDependence)
            {
                this.AvailableActualFiniteStates.AddRange(stateDependence.ActualState);
                this.SelectedThing.SelectedActualFiniteState = this.AvailableActualFiniteStates.FirstOrDefault();
            }
        }

        /// <summary>
        /// Updates the <see cref="AvailableOptions"/> collection
        /// </summary>
        private void UpdateAvailableOptions()
        {
            this.AvailableOptions.AddRange(this.hubController.OpenIteration.Option.Where(x => this.AvailableOptions.All(o => o.Iid != x.Iid)));
            this.Variables.ForEach(x => x.SelectedOption = this.AvailableOptions.Last());
        }

        /// <summary>
        /// Updates the available <see cref="Parameter"/>s for the <see cref="VariableRowViewModel.SelectedElementDefinition"/>
        /// </summary>
        private void UpdateAvailableParameters()
        {
            this.AvailableParameters.Clear();

            if (this.selectedThing?.SelectedElementDefinition != null)
            {
                this.AvailableParameters.AddRange(
                    this.SelectedThing.SelectedElementDefinition.Parameter.Where(this.AreTheseOwnedByTheDomain<Parameter>())
                    .Where(x => this.AvailableParameterTypes.Contains(x.ParameterType))
                // ALTERNATIVE, checking from service instead already constructed list AvailableParameterTypes
                //.Where(x => this.dstHubService.IsSTEPParameterType(x.ParameterType))
                );
            }
        }

        /// <summary>
        /// Updates the <see cref="AvailableElementUsages"/>
        /// </summary>
        private void UpdateAvailableElementUsages()
        {
            this.AvailableElementUsages.Clear();

            if (this.selectedThing?.SelectedElementDefinition != null)
            {
                var ed = this.selectedThing.SelectedElementDefinition;

                // Note: both the owner of the DOE and ED are the owners of the EU
                this.AvailableElementUsages.AddRange(ed.ReferencingElementUsages().Where(x => x.ExcludeOption.Contains(this.selectedThing.SelectedOption) == false));
            }
        }

        /// <summary>
        /// Verify that the <see cref="IOwnedThing"/> is owned by the current domain of expertise
        /// </summary>
        /// <typeparam name="T">The <see cref="IOwnedThing"/> type</typeparam>
        /// <returns>A <see cref="Func{T,T}"/> input parameter is <see cref="IOwnedThing"/> and outputs an assert whether the verification return true </returns>
        private Func<T, bool> AreTheseOwnedByTheDomain<T>() where T : IOwnedThing 
            => x => x.Owner.Iid == this.hubController.CurrentDomainOfExpertise.Iid;
    }
}
