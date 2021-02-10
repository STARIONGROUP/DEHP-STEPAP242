
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
    using CDP4Common.CommonData;


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

        #region IMappingConfigurationDialogViewModel interface

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
            set
            {
                // Parts are stored in parameters of a specifci type (created by Dst)
                //value.SelectedParameterType = this.AvailableParameterTypes.FirstOrDefault();

                this.RaiseAndSetIfChanged(ref this.selectedThing, value);
            }
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
        /// Gets the <see cref="ICommand"/> to continue
        /// </summary>
        public ReactiveCommand<object> ContinueCommand { get; set; }

        /// <summary>
        /// Updates the mapping based on the available 10-25 elements
        /// </summary>
        public void UpdatePropertiesBasedOnMappingConfiguration()
        {
            //this.dstHubService.CheckHubDependencies
            //this.SelectedThing.SelectedParameter = this.dstHubService.
            //this.IsBusy = true;
            //
            //var part = this.SelectedThing;
            //
            //foreach (var idCorrespondence in part.MappingConfigurations)
            //{
            //    if (this.hubController.GetThingById(idCorrespondence.InternalThing, this.hubController.OpenIteration, out Thing thing))
            //    {
            //        Action action = thing switch
            //        {
            //            ElementDefinition elementDefinition => (() => part.SelectedElementDefinition = elementDefinition),
            //            ElementUsage elementUsage => (() => part.SelectedElementUsages.Add(elementUsage)),
            //            Parameter parameter => (() => part.SelectedParameter = parameter),
            //            Option option => (() => part.SelectedOption = option),
            //            ActualFiniteState state => (() => part.SelectedActualFiniteState = state),
            //            _ => null
            //        };
            //
            //        action?.Invoke();
            //
            //        if (action is null && this.hubController.GetThingById(idCorrespondence.InternalThing, out CompoundParameterType parameterType))
            //        {
            //            part.SelectedParameterType = parameterType;
            //        }
            //    }
            //}
            //
            //this.IsBusy = false;
        }

        #endregion

        #region Constructor

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

        #endregion

        #region Private Methods

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

            var canContinue = this.WhenAny(
                vm => vm.SelectedThing,
                vm => vm.SelectedThing.SelectedElementDefinition,
                (part, ed) =>
                    part.Value != null && ed.Value != null
                );

            this.ContinueCommand = ReactiveCommand.Create(canContinue);
            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand());

            this.WhenAnyValue(x => x.SelectedThing.SelectedOption)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.WhenAnyValue(x => x.SelectedThing.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateAvailableFields(() =>
                {
                    this.UpdateAvailableParameters();
                    this.UpdateAvailableElementUsages();
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateAvailableFields(this.UpdateAvailableActualFiniteStates));
        }

        /// <summary>
        /// Executes the specified action to update the view Hub fields surrounded by a <see cref="IsBusy"/> state change
        /// </summary>
        /// <param name="updateAction">The <see cref="Action"/> to execute</param>
        private void UpdateAvailableFields(Action updateAction)
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
            

            try
            {
                // Update the selected parameter
                this.SelectedThing.SelectedParameter = this.SelectedThing.SelectedElementDefinition.Parameter.FirstOrDefault(x => this.dstHubService.IsSTEPParameterType(x.ParameterType));
                this.SelectedThing.SelectedParameterType = this.SelectedThing.SelectedParameter?.ParameterType;

                if (this.SelectedThing.SelectedElementUsages is { })
                {
                    MessageBox.Show("Element Usages were disabled until implementation is finished", "Work in progress");
                    this.SelectedThing.SelectedElementUsages?.Clear();
                }

                this.IsBusy = true;

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
            this.UpdateAvailableElementDefinitions();
            this.UpdateAvailableParameters();
            this.UpdateAvailableElementUsages();
            this.UpdateAvailableActualFiniteStates();

            this.IsBusy = false;
        }

        /// <summary>
        /// Updates the list of compatible parameters
        /// </summary>
        /// <remarks>
        /// This DST targets the information against one special parameter type
        /// created by the DST <seealso cref="DstHubService.CheckParameterTypes"/>
        /// </remarks>
        private void UpdateAvailableParameterTypes()
        {
            this.AvailableParameterTypes.Clear();

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
            
            // Called in constructor, this reference is not yet selected
            //this.SelectedThing.SelectedOption = this.AvailableOptions.Last();
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
        /// Updates the list of compatible element definitions
        /// </summary>
        private void UpdateAvailableElementDefinitions()
        {
            this.AvailableElementDefinitions.Clear();
            this.AvailableElementDefinitions.AddRange(this.hubController.OpenIteration.Element.Where(this.AreTheseOwnedByTheDomain<ElementDefinition>()));
        }

        /// <summary>
        /// Updates the <see cref="AvailableElementUsages"/>
        /// </summary>
        private void UpdateAvailableElementUsages()
        {
            this.AvailableElementUsages.Clear();

            if (this.SelectedThing?.SelectedElementDefinition != null)
            {
                var ed = this.SelectedThing.SelectedElementDefinition;
                Debug.WriteLine($"ED {ed.Name} {ed.Iid}");

                // NOTE: ElementUsages where not cloned when setting the value in the SelecteThing.SelectedElementDefinition
                var hubED = this.hubController.OpenIteration.Element.FirstOrDefault(x => x.Iid == ed.Iid);
                
                // Note: both the owner of the DOE and ED are the owners of the EU
                this.AvailableElementUsages.AddRange(
                    hubED.ReferencingElementUsages()
                        .Where(x => x.ExcludeOption.Contains(this.selectedThing.SelectedOption) == false)
                        .Select(x => x.Clone(true))
                    );

                /*
                Debug.WriteLine($"Element {ed.Name} usages check:");
                foreach (var item in ed.ContainedElement)
                {
                    Debug.WriteLine($"  ContainedElement {item.Name}");
                }

                Debug.WriteLine("----");

                foreach (var item in ed.ReferencedElement)
                {
                    Debug.WriteLine($"  ReferencedElement {item.Name}");
                }

                Debug.WriteLine("+++++");

                foreach (var item in ed.ReferencingElementUsages())
                {
                    Debug.WriteLine($"  Referencing {item.Name}");
                }

                Debug.WriteLine("========================================================");

                Debug.WriteLine($"Element {hubED.Name} usages check:");
                foreach (var item in hubED.ContainedElement)
                {
                    Debug.WriteLine($"  ContainedElement {item.Name}");
                }

                Debug.WriteLine("----");

                foreach (var item in hubED.ReferencedElement)
                {
                    Debug.WriteLine($"  ReferencedElement {item.Name}");
                }

                Debug.WriteLine("+++++");

                foreach (var item in hubED.ReferencingElementUsages())
                {
                    Debug.WriteLine($"  Referencing {item.Name}");
                }
                */
            }
        }

        /// <summary>
        /// Verify that the <see cref="IOwnedThing"/> is owned by the current domain of expertise
        /// </summary>
        /// <typeparam name="T">The <see cref="IOwnedThing"/> type</typeparam>
        /// <returns>A <see cref="Func{T,T}"/> input parameter is <see cref="IOwnedThing"/> and outputs an assert whether the verification return true </returns>
        private Func<T, bool> AreTheseOwnedByTheDomain<T>() where T : IOwnedThing 
            => x => x.Owner.Iid == this.hubController.CurrentDomainOfExpertise.Iid;

        #endregion
    }
}
