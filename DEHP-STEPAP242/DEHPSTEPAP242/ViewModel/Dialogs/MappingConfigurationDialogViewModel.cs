
namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Input;

    using ReactiveUI;

    using DevExpress.Mvvm.Native;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPCommon.Enumerators;


    /// <summary>
    /// The <see cref="MappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping to the hub source
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
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

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
        private Step3DRowViewModel selectedThing;

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        public Step3DRowViewModel SelectedThing
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
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        public ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; } = new ReactiveList<ActualFiniteState>();

        /// <summary>
        /// Gets the <see cref="ICommand"/> to continue
        /// </summary>
        public ReactiveCommand<object> ContinueCommand { get; set; }

        /// <summary>
        /// Sets the target <see cref="Step3DRowViewModel"/> to map
        /// </summary>
        /// <param name="part"></param>
        public void SetPart(Step3DRowViewModel part)
        {
            this.SelectedThing = part;
            this.UpdatePropertiesBasedOnMappingConfiguration();
        }

        /// <summary>
        /// Updates the mapping based on the available 10-25 elements
        /// </summary>
        /// <remarks>
        /// The mapping configuration could not be compatible with the current
        /// things existing on the Hub. Informs the used about this situation.
        /// </remarks>
        private void UpdatePropertiesBasedOnMappingConfiguration()
        {
            var part = this.SelectedThing;
            var warnings = new List<string>();

            part.CleanSelections();

            // First: check ED before processing other things
            foreach (var idCorrespondence in part.MappingConfigurations)
            {
                if (this.hubController.GetThingById(idCorrespondence.InternalThing, this.hubController.OpenIteration, out Thing thing))
                {
                    if (thing is ElementDefinition ed)
                    {
                        this.UpdateSelectionFromMappedConfiguredThing(part, ed, warnings);
                        break;
                    }
                }
            }

            // Second: process the rest
            foreach (var idCorrespondence in part.MappingConfigurations)
            {
                if (this.hubController.GetThingById(idCorrespondence.InternalThing, this.hubController.OpenIteration, out Thing thing))
                {
                    switch (thing)
                    {
                        case ElementDefinition elementDefinition:
                            // Ignore already processed thing
                            break;

                        case ElementUsage elementUsage:
                            this.UpdateSelectionFromMappedConfiguredThing(part, elementUsage, warnings);
                            break;

                        case Parameter parameter:
                            this.UpdateSelectionFromMappedConfiguredThing(part, parameter, warnings);
                            break;

                        case ParameterOverride parameterOverride:
                            this.UpdateSelectionFromMappedConfiguredThing(part, parameterOverride, warnings);
                            break;

                        case Option option:
                            this.UpdateSelectionFromMappedConfiguredThing(part, option, warnings);
                            break;

                        case ActualFiniteState state:
                            this.UpdateSelectionFromMappedConfiguredThing(part, state, warnings);
                            break;

                        default:
                            warnings.Add($"The mapped Thing \"{thing.Iid}\" [{thing}] is not managed");
                            break;
                    };
#if ACTION_SWITCH
                    Action action = thing switch
                    {
                        ElementUsage elementUsage => (() =>
                        {
                            var eu = this.AvailableElementUsages.FirstOrDefault(x => x.Iid == elementUsage.Iid);

                            if (eu is null)
                            {
                                warnings.Add($"The mapped ElementUsage \"{elementUsage.Name}\" [{elementUsage.ShortName}] is not more available");
                            }
                            else
                            {
                                part.SelectedElementUsages.Add(eu);
                            }
                        }),

                        Parameter parameter => (() =>
                        {
                            if (part.SelectedParameter is null)
                            {
                                warnings.Add($"The mapped Parameter \"{parameter.ParameterType.Name}\" [{parameter.ParameterType.ShortName}] is not more available");
                            }
                            else if (part.SelectedParameter.Iid != parameter.Iid)
                            {
                                warnings.Add($"The mapped Parameter \"{parameter.ParameterType.Name}\" [{parameter.ParameterType.ShortName}] is not more available");
                            }
                        }),

                        Option option => (() =>
                        {
                            part.SelectedOption = option;
                        }),

                        ActualFiniteState state => (() => 
                        {
                            if (this.AvailableActualFiniteStates.FirstOrDefault(x => x.Iid == state.Iid) is null)
                            {
                                warnings.Add($"The mapped ActualFiniteState \"{state.Name}\" [{state.ShortName}] is not more available");
                            }
                            else
                            {
                                part.SelectedActualFiniteState = state;
                            }
                        }),

                        _ => null
                    };

                    action?.Invoke();
#endif
                }
            }

            if (warnings.Count > 0 /*&& part.MappingStatus == Step3DRowViewModel.MappingStatusType.Configured*/)
            {
                var text = string.Join(Environment.NewLine + Environment.NewLine, warnings);
                MessageBox.Show(text, "Mapping Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ElementDefinition ed, List<string> warnings)
        {
            part.SelectedElementDefinition = this.AvailableElementDefinitions.FirstOrDefault(x => x.Iid == ed.Iid);

            if (part.SelectedElementDefinition is null)
            {
                warnings.Add($"The mapped ElementDefinition \"{ed.Name}\" [{ed.ModelCode()}] is not more available");
            }
            else
            {
                // Update what can be selected before reading information from the mapping
                this.UpdateAvailableElementUsages();
                this.UpdateSelectedParameter();
                this.UpdateAvailableActualFiniteStates();
            }
        }

        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ElementUsage elementUsage, List<string> warnings)
        {
            if (part.SelectedElementDefinition is null)
            {
                warnings.Add($"Ignoring mapped ElementUsage \"{elementUsage.Name}\" [{elementUsage.ModelCode()}] is not available because ElementDefinition was not defined");
                return;
            }

            var eu = this.AvailableElementUsages.FirstOrDefault(x => x.Iid == elementUsage.Iid);

            if (eu is null)
            {
                warnings.Add($"The mapped ElementUsage \"{elementUsage.Name}\" [{elementUsage.ModelCode()}] is not more available");
            }
            else
            {
                part.SelectedElementUsages.Add(eu);
            }
        }

        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, Parameter parameter, List<string> warnings)
        {
            if (part.SelectedElementDefinition is null)
            {
                warnings.Add($"Ignoring mapped Parameter \"{parameter.ParameterType.Name}\" [{parameter.ModelCode()}] is not available because ElementDefinition was not defined");
                return;
            }

            if (part.SelectedParameter is null)
            {
                warnings.Add($"The mapped Parameter \"{parameter.ParameterType.Name}\" [{parameter.ModelCode()}] is not more available");
            }
            else if (part.SelectedParameter.Iid != parameter.Iid)
            {
                warnings.Add($"The mapped Parameter \"{parameter.ParameterType.Name}\" [{parameter.ModelCode()}] is not more available");
            }
        }

        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ParameterOverride parameterOverride, List<string> warnings)
        {
            if (part.SelectedElementDefinition is null)
            {
                warnings.Add($"Ignoring mapped ParameterOverride \"{parameterOverride.ParameterType.Name}\" [{parameterOverride.ModelCode()}] is not available because ElementDefinition was not defined");
                return;
            }

            this.UpdateSelectionFromMappedConfiguredThing(part, parameterOverride.Parameter, warnings);
        }

        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, Option option, List<string> warnings)
        {
            part.SelectedOption = option;
        }

        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ActualFiniteState state, List<string> warnings)
        {
            if (this.AvailableActualFiniteStates.FirstOrDefault(x => x.Iid == state.Iid) is null)
            {
                warnings.Add($"The mapped ActualFiniteState \"{state.Name}\" [{state.ShortName}] is not more available");
            }
            else
            {
                part.SelectedActualFiniteState = state;
            }
        }

#endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public MappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController, 
            IDstHubService dstHubService, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.dstController = dstController;
            this.dstHubService = dstHubService;
            this.statusBar = statusBar;

            this.InitializeAvailableProperties();
            this.InitializesCommandsAndObservableSubscriptions();
        }

#endregion

#region Private Methods

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void InitializeAvailableProperties()
        {
            this.UpdateAvailableOptions();
            this.UpdateAvailableElementDefinitions();
            //this.UpdateAvailableElementUsages(); // calculated when ED is selected
            this.UpdateAvailableActualFiniteStates();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        /// <remarks>
        /// The strategy is to let the "Continue" button always enabled and
        /// prevent the action in case the conditions are not satified. The reason
        /// is showed to the user using a message dialog <seealso cref="CheckMappingDefinition"/>.
        /// </remarks>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            this.ContinueCommand = ReactiveCommand.Create();
            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand());

            // UI triggers
            this.WhenAnyValue(x => x.SelectedThing.SelectedOption)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateAvailableFields(() =>
                {
                    this.UpdateAvailableElementUsages();
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateAvailableFields(() =>
                {
                    this.UpdateSelectedParameter();
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
        /// Updates the <see cref="AvailableOptions"/> collection
        /// </summary>
        private void UpdateAvailableOptions()
        {
            //this.AvailableOptions.AddRange(this.hubController.OpenIteration.Option.Where(x => this.AvailableOptions.All(o => o.Iid != x.Iid)));
            this.AvailableOptions.AddRange(this.hubController.OpenIteration.Option);
        }

        /// <summary>
        /// Updates the list of compatible element definitions
        /// </summary>
        private void UpdateAvailableElementDefinitions()
        {
            this.AvailableElementDefinitions.AddRange(
                this.hubController.OpenIteration.Element.Where(this.AreTheseOwnedByTheDomain<ElementDefinition>())
                .Select(e => e.Clone(true))
                );
        }

        /// <summary>
        /// Updates the <see cref="AvailableElementUsages"/>
        /// </summary>
        private void UpdateAvailableElementUsages()
        {
            this.AvailableElementUsages.Clear();

            if (this.SelectedThing?.SelectedElementDefinition is { })
            {
                this.AvailableElementUsages.AddRange(this.GetElementUsagesFor(this.SelectedThing.SelectedElementDefinition));
            }
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
        /// Updates the target <see cref="Step3DRowViewModel.SelectedParameter"/>s for the <see cref="Step3DRowViewModel.SelectedElementDefinition"/>
        /// </summary>
        private void UpdateSelectedParameter()
        {
            if (this.SelectedThing.SelectedElementDefinition is { })
            {
                this.SelectedThing.SelectedParameter = this.SelectedThing.SelectedElementDefinition
                    .Parameter.Where(this.AreTheseOwnedByTheDomain<Parameter>())
                    .FirstOrDefault(x => this.dstHubService.IsSTEPParameterType(x.ParameterType));
            }
            else
            {
                this.SelectedThing.SelectedParameter = null;
            }
        }

        /// <summary>
        /// Gets the <see cref="ElementUsage"/> of <see cref="ElementDefinition"/> available for the current selected <see cref="Option"/>
        /// </summary>
        /// <param name="ed">The <see cref="ElementDefinition"/></param>
        /// <returns>A <see cref="List{ElementUsage}"/></returns>
        private List<ElementUsage> GetElementUsagesFor(ElementDefinition ed)
        {
            var usages = new List<ElementUsage>();

            var option = this.SelectedThing?.SelectedOption;
            
            if (option is null)
            {
                // All ElementUsages can be selected
                usages.AddRange(this.AvailableElementDefinitions.SelectMany(d => d.ContainedElement)
                    .Where(u => u.ElementDefinition.Iid == ed.Iid).Select(x => x.Clone(true))
                    );
            }
            else
            {
                // Filter ElementUsages when an Option is selected
                usages.AddRange(this.AvailableElementDefinitions.SelectMany(d => d.ContainedElement)
                    .Where(u => u.ElementDefinition.Iid == ed.Iid && 
                        !u.ExcludeOption.Contains(option)).Select(x => x.Clone(true))
                    );
            }

            //usages.RemoveAll(u => u.ExcludeOption.Contains(option));

            //foreach (var element in this.AvailableElementDefinitions)
            //{
            //    foreach (var containedEU in element.ContainedElement)
            //    {
            //        if (containedEU.ElementDefinition.Iid == ed.Iid)
            //        {
            //            // taken from cloned ED containedEU.Clone(true)
            //            usages.Add(containedEU);
            //        }
            //    }
            //}

            return usages;
        }

        /// <summary>
        /// Verify that the <see cref="IOwnedThing"/> is owned by the current domain of expertise
        /// </summary>
        /// <typeparam name="T">The <see cref="IOwnedThing"/> type</typeparam>
        /// <returns>A <see cref="Func{T,T}"/> input parameter is <see cref="IOwnedThing"/> and outputs an assert whether the verification return true </returns>
        private Func<T, bool> AreTheseOwnedByTheDomain<T>() where T : IOwnedThing
            => x => x.Owner.Iid == this.hubController.CurrentDomainOfExpertise.Iid;

        /// <summary>
        /// Executes the <see cref="ContinueCommand"/> to create the proper mapping
        /// </summary>
        private void ExecuteContinueCommand()
        {
            try
            {
                if (!this.CheckMappingDefinition())
                {
                    return;
                }

                this.IsBusy = true;

                this.statusBar.Append($"Mapping in progress of {SelectedThing.Description}...");

                this.dstController.Map(this.SelectedThing);

                this.statusBar.Append($"{SelectedThing.Description} mapped");

                this.CloseWindowBehavior?.Close();
            }
            catch (Exception e)
            {
                this.statusBar.Append($"Mapping of {SelectedThing.Description} failed", StatusBarMessageSeverity.Error);
                MessageBox.Show($"{e.Message}");
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Checks that the values for the mapping are corrects.
        /// </summary>
        /// <returns>True if the mapping definition is correct</returns>
        private bool CheckMappingDefinition()
        {
            if (this.SelectedThing.SelectedElementDefinition is null)
            {
                // New ElementDefinition and Parameter will be created

                if (this.SelectedThing.SelectedOption is { })
                {
                    MessageBox.Show($"A new ElementDefinition named \"{this.SelectedThing.ElementName}\" will be created,\nthe selected option \"{this.SelectedThing.SelectedOption.Name}\" will not be used in the mapping",
                        "Mapping information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"A new ElementDefinition named \"{this.SelectedThing.ElementName}\" will be created",
                        "Mapping information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return true;
            }

            if (this.SelectedThing.SelectedParameter is null)
            {
                // 3D Geometric Parameter does not exist in current ElementDefinition, it will be added

                if (this.SelectedThing.SelectedOption is { })
                {
                    MessageBox.Show($"The target parameter \"{this.dstHubService.FindSTEPParameterType()?.Name}\" does not exist at \"{this.SelectedThing.SelectedElementDefinition.Name}\",\nthe selected option \"{this.SelectedThing.SelectedOption.Name}\" will not be used in the mapping",
                        "Mapping information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return true;
            }

            if (this.SelectedThing.SelectedParameter.IsOptionDependent &&
                this.SelectedThing.SelectedOption is null)
            {
                MessageBox.Show($"The existing target parameter \"{this.SelectedThing.SelectedParameter.ModelCode()}\" is Option dependent,\nplease select one Option to apply the mapping",
                    "Mapping incomplete", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return false;
            }

            if (this.SelectedThing.SelectedParameter.StateDependence is { } &&
                this.SelectedThing.SelectedActualFiniteState is null)
            {
                MessageBox.Show($"The existing target parameter \"{this.SelectedThing.SelectedParameter.ModelCode()}\" has a State Dependence,\nplease select one State Dependence to apply the mapping",
                    "Mapping incomplete", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return false;
            }

            return true;
        }

#endregion
    }
}
