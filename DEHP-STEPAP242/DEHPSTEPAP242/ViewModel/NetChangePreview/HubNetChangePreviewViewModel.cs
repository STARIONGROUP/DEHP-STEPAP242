// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubNetChangePrewiewModel.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPAP242.ViewModel.NetChangePreview
{
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Dal;
    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview;
    using DEHPCommon.UserInterfaces.ViewModels.Rows;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    /// <summary>
    /// The <see cref="HubNetChangePreviewViewModel"/> is the view model
    /// for the Net Change Preview of the 10-25 data source from
    /// mappings of STEP-AP242 parts.
    /// </summary>
    public class HubNetChangePreviewViewModel : NetChangePreviewViewModel, IHubNetChangePreviewViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The command for the context menu that allows to deselect all selectable <see cref="ElementBase"/> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer"/>
        /// </summary>
        public ReactiveCommand<object> DeselectAllCommand { get; set; }

        /// <summary>
        /// The command for the context menu that allows to select all selectable <see cref="ElementBase"/> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer"/>
        /// </summary>
        public ReactiveCommand<object> SelectAllCommand { get; set; }

#if UPDATE_SELECTION
        // <summary>
        /// The collection of <see cref="VariableRowViewModel"/> that represents the latest selection
        /// </summary>
        private readonly List<Step3DRowViewModel> previousSelection = new List<Step3DRowViewModel>();
#endif

        /// <summary>
        /// Gets or sets a value indicating that the tree in the case that
        /// <see cref="DstController.MapResult"/> is not empty and the tree is not showing all changes
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel" /> class.
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubController">The <see cref="T:DEHPCommon.HubController.Interfaces.IHubController" /></param>
        /// <param name="objectBrowserTreeSelectorService">The <see cref="T:DEHPCommon.Services.ObjectBrowserTreeSelectorService.IObjectBrowserTreeSelectorService" /></param>
        public HubNetChangePreviewViewModel(IDstController dstController, IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService)
            : base(hubController, objectBrowserTreeSelectorService)
        {
            this.dstController = dstController;
            

            this.SelectedThings.BeforeItemsAdded.Subscribe(this.WhenItemSelectedChanges);
            this.SelectedThings.BeforeItemsRemoved.Subscribe(this.WhenItemSelectedChanges);

            this.SelectAllCommand = ReactiveCommand.Create();
            this.SelectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer());

            this.DeselectAllCommand = ReactiveCommand.Create();
            this.DeselectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer(false));

#if UPDATE_SELECTION
            //CDPMessageBus.Current.Listen<UpdateHubPreviewBasedOnSelectionEvent>()
            //    .ObserveOn(RxApp.MainThreadScheduler)
            //    .Subscribe(x => this.UpdateTreeBasedOnSelectionHandler(x.Selection.ToList()));

            CDPMessageBus.Current.Listen<SessionEvent>(this.HubController.Session)
                .Where(x => x.Status == SessionStatus.EndUpdate)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    this.UpdateTreeBasedOnSelectionHandler(this.previousSelection);
                });
#endif
        }

#if UPDATE_SELECTION
        /// <summary>
        /// Updates the tree and filter changed things based on a selection
        /// </summary>
        /// <param name="selection">The collection of selected <see cref="Step3DRowViewModel"/> </param>
        private void UpdateTreeBasedOnSelectionHandler(IReadOnlyCollection<Step3DRowViewModel> selection)
        {
            if (this.dstController.MapResult.Any())
            {
                this.IsBusy = true;

                if (!selection.Any() && this.IsDirty)
                {
                    this.previousSelection.Clear();
                    this.ComputeValuesWrapper();
                }
                else if (selection.Any())
                {
                    this.previousSelection.Clear();
                    this.previousSelection.AddRange(selection);
                    this.UpdateTreeBasedOnSelection(selection);
                }

                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Updates the trees with the selection
        /// </summary>
        /// <param name="selection">The collection of selected <see cref="Step3DRowViewModel"/> </param>
        private void UpdateTreeBasedOnSelection(IEnumerable<Step3DRowViewModel> selection)
        {
            this.RestoreThings();

            var parametersToRemove = this.dstController.ParameterNodeIds
                .Where(p => !selection.Any(v => p.Value.Equals(v.Reference.NodeId.Identifier)))
                .Select(x => x.Key)
                .ToList();

            foreach (var variable in selection)
            {
                var parameters = this.dstController.ParameterNodeIds
                    .Where(v => v.Value.Equals(variable.Reference.NodeId.Identifier))
                    .Select(x => x.Key);

                foreach (var parameterOrOverrideBase in parameters)
                {
                    var parameterRows = this.GetRows(parameterOrOverrideBase).ToList();

                    if (!parameterRows.Any())
                    {
                        var oldElement = this.GetOldElement(parameterOrOverrideBase.Container);

                        (oldElement as ElementDefinition)?.Parameter.RemoveAll(p =>
                            parametersToRemove.Any(r => p.ParameterType.Iid == r.ParameterType.Iid));

                        var elementRow = this.VerifyElementIsInTheTree(parameterOrOverrideBase);

                        this.UpdateRow(parameterOrOverrideBase, (ElementDefinition)oldElement, elementRow);

                        this.IsDirty = true;
                    }

                    foreach (var parameterRow in parameterRows)
                    {
                        var oldElement = this.GetOldElement(parameterRow.ContainerViewModel.Thing);

                        if (parameterRow.ContainerViewModel is ElementDefinitionRowViewModel elementDefinitionRow)
                        {
                            this.UpdateRow(parameterOrOverrideBase, (ElementDefinition)oldElement, elementDefinitionRow);
                        }
                        else if (parameterRow.ContainerViewModel is ElementUsageRowViewModel elementUsageRow)
                        {
                            this.UpdateRow(parameterOrOverrideBase, (ElementUsage)oldElement, elementUsageRow);
                        }

                        this.IsDirty = true;
                    }
                }
            }
        }
#endif

        private void WhenItemSelectedChanges(object row)
        {
            if (row is RowViewModelBase<ElementDefinition> elementDefinitionRow)
            {
                this.SelectChainOfContainerViewModel(elementDefinitionRow, !elementDefinitionRow.IsSelectedForTransfer);
            }

            if (row is RowViewModelBase<ElementUsage> elementUsageRow)
            {
                this.SelectChainOfContainerViewModel(elementUsageRow, !elementUsageRow.IsSelectedForTransfer);

                var definitionRowViewModel = this.Things.OfType<ElementDefinitionsBrowserViewModel>()
                    .SelectMany(r => r.ContainedRows.OfType<ElementDefinitionRowViewModel>())
                    .FirstOrDefault(r => r.Thing.ShortName == elementUsageRow.Thing.ElementDefinition.ShortName);

                if (definitionRowViewModel is { })
                {
                    definitionRowViewModel.IsSelectedForTransfer = !elementUsageRow.IsSelectedForTransfer;
                    this.AddOrRemoveToSelectedThingsToTransfer(definitionRowViewModel, !elementUsageRow.IsSelectedForTransfer);
                }
            }
        }

        /// <summary>
        /// Adds or removes the <paramref name="row.Thing"/> and it's chain of container to the <see cref="IDstController.SelectedThingsToTransfer"/>
        /// </summary>
        /// <typeparam name="TElement">The type of <see cref="ElementBase"/> the <paramref name="row"/> represents</typeparam>
        /// <param name="row">The <see cref="IRowViewModelBase{T}"/> to select or deselect</param>
        /// <param name="isSelected">A value indicating whether the <paramref name="row"/> should be added or removed</param>
        private void SelectChainOfContainerViewModel<TElement>(IRowViewModelBase<TElement> row, bool isSelected = true) where TElement : ElementBase
        {
            this.AddOrRemoveToSelectedThingsToTransfer(row, isSelected);

            if (row.ContainerViewModel is RowViewModelBase<ElementDefinition> container)
            {
                this.SelectChainOfContainerViewModel(container);
            }
        }

        /// <summary>
        /// Adds or removes the <paramref name="row.Thing"/> to the <see cref="IDstController.SelectedThingsToTransfer"/>
        /// </summary>
        /// <typeparam name="TElement">The type of <see cref="ElementBase"/> the <paramref name="row"/> represents</typeparam>
        /// <param name="row">The <see cref="IRowViewModelBase{T}"/> to select or deselect</param>
        /// <param name="isSelected">A value indicating whether the <paramref name="row"/> should be added or removed</param>
        private void AddOrRemoveToSelectedThingsToTransfer<TElement>(IViewModelBase<TElement> row, bool isSelected = true) where TElement : ElementBase
        {
            this.AddOrRemoveToSelectedThingsToTransfer(row.Thing, isSelected);
        }

        /// <summary>
        /// Adds or removes the <paramref name="element"/>  to the <see cref="IDstController.SelectedThingsToTransfer"/>
        /// </summary>
        /// <typeparam name="TElement">The type of <paramref name="element"/></typeparam>
        /// <param name="element">The <typeparamref name="TElement"/> element to add or remove</param>
        /// <param name="isSelected">A value indicating whether the <paramref name="element"/> should be added or removed</param>
        private void AddOrRemoveToSelectedThingsToTransfer<TElement>(TElement element, bool isSelected = true) where TElement : ElementBase
        {
            if (isSelected)
            {
                this.dstController.SelectedThingsToTransfer.Add(element);
            }
            else
            {
                this.dstController.SelectedThingsToTransfer.RemoveAll(
                    this.dstController.SelectedThingsToTransfer
                        .Where(x => x.ShortName == element.ShortName && x.Iid == element.Iid).ToList());
            }

            CDPMessageBus.Current.SendMessage(new SelectEvent(element, !isSelected));
        }

        /// <summary>
        /// Updates the tree
        /// </summary>
        /// <param name="shouldReset">A value indicating whether the tree should remove the element in preview</param>
        public override void UpdateTree(bool shouldReset)
        {
            if (shouldReset)
            {
                this.Reload();
            }
            else
            {
                this.ComputeValuesWrapper();
            }
        }

        /// <summary>
        /// Gets the <see cref="ElementBase"/> at the previous state that correspond to the <paramref name="container"/>
        /// </summary>
        /// <param name="container">The <see cref="Thing"/> container</param>
        /// <returns>A <see cref="ElementBase"/></returns>
        private ElementBase GetOldElement(Thing container)
        {
            return this.ThingsAtPreviousState
                .FirstOrDefault(t => t.Iid == container.Iid
                                  || container.Iid == Guid.Empty
                                  && container is ElementDefinition element
                                  && t is ElementDefinition previousStateElement
                                  && element.Name == previousStateElement.Name
                                  && element.ShortName == previousStateElement.ShortName)
                as ElementBase;
        }

        /// <summary>
        /// Updates the <paramref name="elementRow"/> children with the <paramref name="parameterOrOverrideBase"/>
        /// </summary>
        /// <typeparam name="TThing">The precise type of <see cref="ElementBase"/></typeparam>
        /// <typeparam name="TRow">The type of <paramref name="elementRow"/></typeparam>
        /// <param name="parameterOrOverrideBase">The <see cref="ParameterOrOverrideBase"/></param>
        /// <param name="oldElement">The old <see cref="ElementBase"/> to be updated</param>
        /// <param name="elementRow">The row to perform on the update</param>
        private void UpdateRow<TThing, TRow>(ParameterOrOverrideBase parameterOrOverrideBase, TThing oldElement, TRow elementRow)
            where TRow : ElementBaseRowViewModel<TThing> where TThing : ElementBase
        {
            var updatedElement = (TThing)oldElement?.Clone(true);

            this.AddOrReplaceParameter(updatedElement, parameterOrOverrideBase);

            CDPMessageBus.Current.SendMessage(new HighlightEvent(elementRow.Thing), elementRow.Thing);

            elementRow.UpdateThing(updatedElement);

            elementRow.UpdateChildren();
        }

        /// <summary>
        /// Restores the tree to the point before <see cref="ComputeValues"/> was executed the first time
        /// </summary>
        private void RestoreThings()
        {
            var isExpanded = this.Things.First().IsExpanded;

            this.UpdateTree(true);

            this.Things.First().IsExpanded = isExpanded;
        }

        /// <summary>
        /// Adds or replace the <paramref name="parameterOrOverride"/> on the <paramref name="updatedElement"/>
        /// </summary>
        /// <param name="updatedElement">The <see cref="ElementBase"/></param>
        /// <param name="parameterOrOverride">The <see cref="ParameterOrOverrideBase"/></param>
        private void AddOrReplaceParameter(ElementBase updatedElement, ParameterOrOverrideBase parameterOrOverride)
        {
            if (updatedElement is ElementDefinition elementDefinition)
            {
                if (elementDefinition.Parameter.FirstOrDefault(p => p.ParameterType.Iid == parameterOrOverride.ParameterType.Iid)
                    is { } parameter)
                {
                    elementDefinition.Parameter.Remove(parameter);
                }

                elementDefinition.Parameter.Add((Parameter)parameterOrOverride);
            }
            else if (updatedElement is ElementUsage elementUsage)
            {
                if (parameterOrOverride is Parameter elementParameter)
                {
                    if (elementUsage.ElementDefinition.Parameter.FirstOrDefault(p => p.ParameterType.Iid == parameterOrOverride.ParameterType.Iid)
                        is { } parameter)
                    {
                        elementUsage.ElementDefinition.Parameter.Remove(parameter);
                    }

                    elementUsage.ElementDefinition.Parameter.Add(elementParameter);
                }
                else if (parameterOrOverride is ParameterOverride parameterOverride)
                {
                    if (elementUsage.ParameterOverride.FirstOrDefault(p => p.ParameterType.Iid == parameterOrOverride.ParameterType.Iid)
                        is { } parameter)
                    {
                        elementUsage.ParameterOverride.Remove(parameter);
                    }

                    elementUsage.ParameterOverride.Add(parameterOverride);
                }
            }
        }

        /// <summary>
        /// Gets all the rows of type <see cref="ParameterOrOverrideBase}"/> that are related to <paramref name="parameter"/>
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterBase"/></param>
        /// <returns>A collection of <see cref="IRowViewModelBase{T}"/> of type <see cref="ParameterOrOverrideBase"/></returns>
        private IEnumerable<IRowViewModelBase<ParameterOrOverrideBase>> GetRows(ParameterBase parameter)
        {
            var result = new List<IRowViewModelBase<ParameterOrOverrideBase>>();

            foreach (var elementDefinitionRow in this.Things.OfType<ElementDefinitionsBrowserViewModel>()
                .SelectMany(x => x.ContainedRows.OfType<ElementDefinitionRowViewModel>()))
            {
                result.AddRange(elementDefinitionRow.ContainedRows
                    .OfType<ElementUsageRowViewModel>()
                    .SelectMany(x => x.ContainedRows
                        .OfType<IRowViewModelBase<ParameterOrOverrideBase>>())
                    .Where(parameterRow => VerifyRowContainsTheParameter(parameter, parameterRow)));

                result.AddRange(elementDefinitionRow.ContainedRows
                    .OfType<IRowViewModelBase<ParameterOrOverrideBase>>()
                    .Where(parameterRow => VerifyRowContainsTheParameter(parameter, parameterRow)));
            }

            return result;
        }

        // <summary>
        /// Calls the <see cref="ComputeValues"/> with some household
        /// </summary>
        private void ComputeValuesWrapper()
        {
            this.IsBusy = true;
            this.dstController.SelectedThingsToTransfer.Clear();
            var isExpanded = this.Things.First().IsExpanded;
            this.ComputeValues();
            this.SelectDeselectAllForTransfer();
            this.Things.First().IsExpanded = isExpanded;
            this.IsDirty = false;
            this.IsBusy = false;
        }

        /// <summary>
        /// Verify that the <paramref name="parameterRow"/> contains the <paramref name="parameter"/>
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterBase"/></param>
        /// <param name="parameterRow">The <see cref="IRowViewModelBase{T}"/></param>
        /// <returns>An assert</returns>
        private static bool VerifyRowContainsTheParameter(ParameterBase parameter, IRowViewModelBase<ParameterOrOverrideBase> parameterRow)
        {
            var containerIsTheRightOne = (parameterRow.ContainerViewModel.Thing.Iid == parameter.Container.Iid ||
                                          (parameterRow.ContainerViewModel.Thing is ElementUsage elementUsage
                                           && (elementUsage.ElementDefinition.Iid == parameter.Container.Iid
                                           || elementUsage.Iid == parameter.Container.Iid)));

            var parameterIsTheRightOne = (parameterRow.Thing.Iid == parameter.Iid ||
                                          (parameter.Iid == Guid.Empty
                                           && parameter.ParameterType.Iid == parameterRow.Thing.ParameterType.Iid));

            return containerIsTheRightOne && parameterIsTheRightOne;
        }

        /// <summary>
        

        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        public override void ComputeValues()
        {
            foreach (var parameterOverride in this.dstController.MapResult.OfType<ElementUsage>()
                .SelectMany(x => x.ParameterOverride))
            {
                var parameterRows = this.GetRows(parameterOverride).ToList();

                foreach (var parameterRow in parameterRows)
                {
                    if (parameterRow.ContainerViewModel is ElementUsageRowViewModel elementUsageRow)
                    {
                        //var t = elementUsageRow.ContainerViewModel.Thing;
                        //var eu = (ElementUsage)t;
                        //
                        //this.UpdateRow(parameterOverride, (ElementUsage)elementUsageRow.ContainerViewModel.Thing, elementUsageRow);

                        this.UpdateRow(parameterOverride, elementUsageRow.Thing, elementUsageRow);
                    }

                    this.ThingsAtPreviousState.Add(parameterRow.ContainerViewModel.Thing.Clone(true));
                }
            }

            foreach (var parameter in this.dstController.MapResult.OfType<ElementDefinition>()
                .SelectMany(x => x.Parameter))
            {
                var elementRow = this.VerifyElementIsInTheTree(parameter);

                if (parameter.Iid == Guid.Empty)
                {
                    this.UpdateRow(parameter, (ElementDefinition)parameter.Container, elementRow);
                    this.AddToThingsAtPreviousState(elementRow.Thing);
                    continue;
                }

                var parameterRows = this.GetRows(parameter).ToList();

                foreach (var parameterRow in parameterRows)
                {
                    if (parameterRow.ContainerViewModel is ElementDefinitionRowViewModel elementDefinitionRow)
                    {
                        this.UpdateRow(parameter, (ElementDefinition)parameterRow.ContainerViewModel.Thing, elementDefinitionRow);
                    }
                    else if (parameterRow.ContainerViewModel is ElementUsageRowViewModel elementUsageRow)
                    {
                        this.UpdateRow(parameter, (ElementUsage)parameterRow.ContainerViewModel.Thing, elementUsageRow);
                    }

                    this.AddToThingsAtPreviousState(parameterRow.ContainerViewModel.Thing);
                }
            }
        }

        /// <summary>
        /// Adds to <see cref="NetChangePreviewViewModel.ThingsAtPreviousState"/>
        /// </summary>
        /// <param name="element">The element to add</param>
        private void AddToThingsAtPreviousState(Thing element)
        {
            var existing = this.GetOldElement(element);

            if (existing == null)
            {
                this.ThingsAtPreviousState.Add(element.Clone(true));
            }
        }

        /// <summary>
        /// Verifies that the <see cref="ElementDefinition"/> container of the <paramref name="parameterOrOverrideBase"/>
        /// exists in the tree. If not it creates it
        /// </summary>
        /// <param name="parameterOrOverrideBase">The <see cref="Thing"/> parameterOrOverrideBase</param>
        /// <returns>A <see cref="ElementDefinitionRowViewModel"/></returns>
        private ElementDefinitionRowViewModel VerifyElementIsInTheTree(Thing parameterOrOverrideBase)
        {
            var iterationRow =
                this.Things.OfType<ElementDefinitionsBrowserViewModel>().FirstOrDefault();

            var elementDefinitionRow = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                .FirstOrDefault(e => e.Thing.Iid == parameterOrOverrideBase.Container.Iid
                                     && e.Thing.Name == ((INamedThing)parameterOrOverrideBase.Container).Name);

            if (elementDefinitionRow is null)
            {
                elementDefinitionRow = new ElementDefinitionRowViewModel((ElementDefinition)parameterOrOverrideBase.Container,
                    this.HubController.CurrentDomainOfExpertise, this.HubController.Session, iterationRow);

                iterationRow.ContainedRows.Add(elementDefinitionRow);
            }

            return elementDefinitionRow;
        }

        /// <summary>
        /// Add a context menu to be able to select or unselect all the mapped entities show in the impact view
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Select all for transfer", "", this.SelectAllCommand, MenuItemKind.Copy, ClassKind.NotThing));

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Deselect all for transfer", "", this.DeselectAllCommand, MenuItemKind.Delete, ClassKind.NotThing));
        }

        /// <summary>
        /// Executes the <see cref="SelectAllCommand"/> and the <see cref="DeselectAllCommand"/>
        /// </summary>
        /// <param name="areSelected">A value indicating whether the elements are to be selected</param>
        public void SelectDeselectAllForTransfer(bool areSelected = true)
        {
            foreach (var element in this.dstController.MapResult)
            {
                this.AddOrRemoveToSelectedThingsToTransfer(element, areSelected);
            }
        }
    }
}