// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
//
//    Part of the code was based on the work performed by RHEA as result
//    of the collaboration in the context of "Digital Engineering Hub Pathfinder"
//    by Sam Gerené, Alex Vorobiev, Alexander van Delft and Nathanael Smiechowski.
// 
//    This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
// 
//    The DEHP STEP-AP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHP STEP-AP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Input;

    using DevExpress.Mvvm.Native;
    using ReactiveUI;
    using Autofac;
    using NLog;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.Services.NavigationService;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Views.Dialogs;
    using DEHPSTEPAP242.Services.DstHubService;


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
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

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

            this.logger.Debug($"Update Selections for: {part.Description}");

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
                    this.logger.Debug($"Process Correspondance Thing = {thing} --> {thing.Iid}");

                    switch (thing)
                    {
                        case ElementDefinition elementDefinition:
                            // Ignore already processed ED thing
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
                }
            }

            if (warnings.Count > 0)
            {
                var text = string.Join(Environment.NewLine + Environment.NewLine, warnings);
                MessageBox.Show(text, "Mapping Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.logger.Debug($"Update Selections finished");
        }

        /// <summary>
        /// Updates selection based on the current mapping to 10-25 elements
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> to be updated</param>
        /// <param name="ed">The <see cref="ElementDefinition"/> to me selected</param>
        /// <param name="warnings">The container filled with warnings (if there are ones)</param>
        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ElementDefinition ed, List<string> warnings)
        {
            part.SelectedElementDefinition = this.AvailableElementDefinitions.FirstOrDefault(x => x.Iid == ed.Iid);

            if (part.SelectedElementDefinition is null)
            {
                var msg = $"The mapped ElementDefinition \"{ed.Name}\" [{ed.ModelCode()}] is not more available";
                warnings.Add(msg);

                this.logger.Warn(msg);
            }
            else
            {
                this.logger.Debug($"Select ElementDefinition \"{ed.Name}\" [{ed.ModelCode()}]");

                // Update what can be selected before reading information from the mapping
                this.UpdateAvailableElementUsages();
                this.UpdateSelectedParameter();
                this.UpdateAvailableActualFiniteStates();
            }
        }

        /// <summary>
        /// Updates selection based on the current mapping to 10-25 elements
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> to be updated</param>
        /// <param name="ed">The <see cref="ElementUsage"/> to me selected</param>
        /// <param name="warnings">The container filled with warnings (if there are ones)</param>
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
                this.logger.Debug($"Add Selected ElementUsage \"{eu.Name}\" [{eu.ModelCode()}]");
                part.SelectedElementUsages.Add(eu);
            }
        }

        /// <summary>
        /// Updates selection based on the current mapping to 10-25 elements
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> to be updated</param>
        /// <param name="ed">The <see cref="Parameter"/> to me selected</param>
        /// <param name="warnings">The container filled with warnings (if there are ones)</param>
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

        /// <summary>
        /// Updates selection based on the current mapping to 10-25 elements
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> to be updated</param>
        /// <param name="ed">The <see cref="ParameterOverride"/> to me selected</param>
        /// <param name="warnings">The container filled with warnings (if there are ones)</param>
        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ParameterOverride parameterOverride, List<string> warnings)
        {
            if (part.SelectedElementDefinition is null)
            {
                warnings.Add($"Ignoring mapped ParameterOverride \"{parameterOverride.ParameterType.Name}\" [{parameterOverride.ModelCode()}] is not available because ElementDefinition was not defined");
                return;
            }

            this.UpdateSelectionFromMappedConfiguredThing(part, parameterOverride.Parameter, warnings);
        }

        /// <summary>
        /// Updates selection based on the current mapping to 10-25 elements
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> to be updated</param>
        /// <param name="ed">The <see cref="Option"/> to me selected</param>
        /// <param name="warnings">The container filled with warnings (if there are ones)</param>
        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, Option option, List<string> warnings)
        {
            var theOption = this.AvailableOptions.FirstOrDefault(x => x.Iid == option.Iid);

            if (theOption is null)
            {
                warnings.Add($"The mapped Option \"{option.Name}\" [{option.ShortName}] is not more available");
            }
            else
            {
                part.SelectedOption = theOption;
            }
        }

        /// <summary>
        /// Updates selection based on the current mapping to 10-25 elements
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> to be updated</param>
        /// <param name="ed">The <see cref="ActualFiniteState"/> to me selected</param>
        /// <param name="warnings">The container filled with warnings (if there are ones)</param>
        private void UpdateSelectionFromMappedConfiguredThing(Step3DRowViewModel part, ActualFiniteState state, List<string> warnings)
        {
            var theState = this.AvailableActualFiniteStates.FirstOrDefault(x => x.Iid == state.Iid);

            if (theState is null)
            {
                warnings.Add($"The mapped ActualFiniteState \"{state.Name}\" [{state.ShortName}] is not more available");
            }
            else
            {
                part.SelectedActualFiniteState = theState;
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

            this.CheckExternalMappingPersistance();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize view model properties which does not depend on selection
        /// </summary>
        private void InitializeAvailableProperties()
        {
            // Note: the available ElementUsages are selected in function of ED/Option selection

            this.UpdateAvailableOptions();
            this.UpdateAvailableElementDefinitions();
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

        private void CheckExternalMappingPersistance()
        {
            if (this.dstController.ExternalIdentifierMap is null)
            {
                MessageBox.Show("There is no Mapping Configuration selected!\n\nThe mapping values will not be remembered,\nyou will need to select the parameters each time.",
                    "Mapping Configuration", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
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
            this.logger.Debug("Clean previous elements");
            this.AvailableElementUsages.Clear();

            if (this.SelectedThing?.SelectedElementDefinition is { })
            {
                this.logger.Debug($"Current ElementDefinition: {this.SelectedThing.SelectedElementDefinition.Name} [{this.SelectedThing.SelectedElementDefinition.ModelCode()}]");

                var selectedElementUsages = this.SelectedThing.SelectedElementUsages.ToList();

                foreach (var eu in selectedElementUsages)
                {
                    this.logger.Debug($"Selected ElementDefinition: \"{eu.Name}\" [{eu.ModelCode()}]");
                }

                var elementsUsages = this.GetElementUsagesFor(this.SelectedThing.SelectedElementDefinition);

                foreach (var eu in elementsUsages)
                {
                    this.logger.Debug($"Available ElementUsage from selected ElementeDefinition: \"{eu.Name}\" [{eu.ModelCode()}]");
                }

                this.AvailableElementUsages.AddRange(elementsUsages);

                foreach (var eu in this.SelectedThing.SelectedElementUsages)
                {
                    this.logger.Debug($"BEFORE Selected ElementUsage: \"{eu.Name}\" [{eu.ModelCode()}]");
                }

                // The UI filters any existing ElementUsage not compatible with its source (AvailableElementUsages)
                this.SelectedThing.SelectedElementUsages.Clear();
                this.SelectedThing.SelectedElementUsages.AddRange(
                    this.AvailableElementUsages.Where(x => selectedElementUsages.Any(s => s.Iid == x.Iid))
                    );

                foreach (var eu in this.SelectedThing.SelectedElementUsages)
                {
                    this.logger.Debug($"AFTER Selected ElementUsage: \"{eu.Name}\" [{eu.ModelCode()}]");
                }
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

                var currentStateDependence = this.SelectedThing.SelectedActualFiniteState;
                this.SelectedThing.SelectedActualFiniteState = this.AvailableActualFiniteStates.FirstOrDefault(x => x.Iid == currentStateDependence?.Iid);
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

                if (this.SelectedThing.SelectedElementDefinition is null)
                {
                    // When ElementDefinition is not selected, a new one will created
                    // to store the selected geometry.
                    if (!this.SelectNewElementDefinitionName())
                    {
                        return;
                    }
                }

                this.IsBusy = true;

                this.statusBar.Append($"Mapping in progress of {SelectedThing.Description}...");

                this.dstController.Map(this.SelectedThing);

                this.statusBar.Append($"{SelectedThing.Description} mapped");

                this.CloseWindowBehavior?.Close();
            }
            catch (Exception exception)
            {
                this.statusBar.Append($"Mapping of {SelectedThing.Description} failed", StatusBarMessageSeverity.Error);
                this.logger.Error(exception);
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
                MessageBox.Show($"The existing target parameter \"{this.SelectedThing.SelectedParameter.ModelCode()}\"\nis Option dependent,\nplease select one Option to apply the mapping",
                    "Mapping incomplete", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return false;
            }

            if (this.SelectedThing.SelectedParameter.StateDependence is { } &&
                this.SelectedThing.SelectedActualFiniteState is null)
            {
                MessageBox.Show($"The existing target parameter \"{this.SelectedThing.SelectedParameter.ModelCode()}\"\nhas a State Dependence,\nplease select one State Dependence to apply the mapping",
                    "Mapping incomplete", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the name for the new <see cref="ElementDefinition"/> to be created
        /// </summary>
        /// <returns>True if a new valid name was selected</returns>
        private bool SelectNewElementDefinitionName()
        {
            string elementName = string.IsNullOrWhiteSpace(this.selectedThing.NewElementDefinitionName) ? this.SelectedThing.ElementName : this.selectedThing.NewElementDefinitionName;

            var vm = new InputDialogViewModel("Mapping information",
                "A new ElementDefinition will be created\n\nDefine the name to be used:",
                elementName, "<New name>");

            var dlg = new InputDialog()
            {
                DataContext = vm
            };

            bool? result = dlg.ShowDialog();

            if (result.GetValueOrDefault())
            {
                // Check that the Name is valid according to the rules for Name
                // a) does not end with space
                // b) Start with letter
                // c) Does not contain parenthesis
                // d) Does not exists an ED in the system or pending of transerf

                var name = vm.Text.Trim();

                if (string.IsNullOrWhiteSpace(vm.Text))
                {
                    this.statusBar.Append($"The name for a new ElementDefinition cannot be empty", StatusBarMessageSeverity.Warning);
                    return false;
                }

                if (Regex.IsMatch(name, "[\\(\\)]"))
                {
                    this.statusBar.Append($"The '{name}' cannot be used as name for a new ElementDefinition, parenthesis are not valid", StatusBarMessageSeverity.Warning);
                    return false;
                }

                if (!Regex.IsMatch(name, "^[a-zA-Z]"))
                {
                    this.statusBar.Append($"The '{name}' cannot be used as name for a new ElementDefinition, it should start with a letter", StatusBarMessageSeverity.Warning);
                    return false;
                }

                if (this.hubController.OpenIteration.Element.Any(x => x.Name == name))
                {
                    this.statusBar.Append($"The '{name}' cannot be used as name for a new ElementDefinition, it already exists in the curent model", StatusBarMessageSeverity.Warning);
                    return false;
                }

                if (this.dstController.MapResult.Any(x => x is ElementDefinition && x.Name == name))
                {
                    this.statusBar.Append($"The '{name}' cannot be used as name for a new ElementDefinition, it already exists in pending mapping creation", StatusBarMessageSeverity.Warning);
                    return false;
                }

                this.selectedThing.NewElementDefinitionName = name;
                return true;
            }

            return false;
        }

        #endregion
    }
}
