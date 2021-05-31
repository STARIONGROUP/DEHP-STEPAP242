// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstObjectBrowserViewModel.cs" company="Open Engineering S.A.">
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

#define DEBUG_DST_OBJECT_BROWSER

namespace DEHPSTEPAP242.ViewModel
{
    using Autofac;
    using CDP4Common.CommonData;
    using CDP4Dal;
    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.Events;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Views.Dialogs;
    using NLog;
    using ReactiveUI;
    using STEP3DAdapter;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    /// <summary>
    /// The <see cref="DstObjectBrowserViewModel"/> is the view model 
    /// of the <see cref="DstObjectBrowser"/> and provides the 
    /// High Level Representation (aka HLR) of a STEP-AP242 file.
    /// 
    /// Information is provided as a Self-Referential Data Source.
    /// 
    /// To represent data in a tree structure, the data source should contain the following fields:
    /// - Key Field: This field should contain unique values used to identify nodes.
    /// - Parent Field: This field should contain values that indicate parent nodes.
    /// 
    /// <seealso cref="Step3DRowViewModel"/>
    /// </summary>
    public class DstObjectBrowserViewModel : ReactiveObject, IDstObjectBrowserViewModel, IHaveContextMenuViewModel
    {
        #region Private Interface References

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

#if DEBUG_DST_OBJECT_BROWSER
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
#endif

        #endregion Private Interface References

        #region IDstObjectBrowserViewModel interface

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
        /// Backing field for <see cref="Step3DHLR"/>
        /// </summary>
        private List<Step3DRowViewModel> step3DHLR = new List<Step3DRowViewModel>();

        /// <summary>
        /// Gets or sets the Step3D High Level Representation structure.
        /// </summary>
        public List<Step3DRowViewModel> Step3DHLR
        {
            get => this.step3DHLR;
            private set => this.RaiseAndSetIfChanged(ref this.step3DHLR, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedPart"/>
        /// </summary>
        private Step3DRowViewModel selectedPart;

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="Step3DRowViewModel"/>
        /// </summary>
        public Step3DRowViewModel SelectedPart
        {
            get => this.selectedPart;
            set => this.RaiseAndSetIfChanged(ref this.selectedPart, value);
        }

        /// <summary>
        /// Create the HLR tree from the Parts/Relations.
        /// 
        /// The HLR is a Self-Referential Data Source. In this approach
        /// each item in the list has a pair values describing the tree link.
        /// they are ID and ParentID.
        /// 
        /// The ID and ParentID are assigned in a way they are different 
        /// at each level, enabling that sub-trees can be repeated at
        /// different levels.
        /// 
        /// Any <see cref="STEP3D_Part"/> item without <see cref="STEP3D_PartRelation"/> 
        /// defining a specific instance, it will placed as child of the main root item.
        /// </summary>
        /// <param name="step3d">A <see cref="STEP3DFile"/> instance</param>
        public void UpdateHLR()
        {
            if (dstController.IsLoading)
            {
                IsBusy = true;
                return;
            }

            SelectedPart = null;

            var builder = AppContainer.Container.Resolve<IHighLevelRepresentationBuilder>();

            Step3DHLR = builder.CreateHLR(dstController.Step3DFile);

            IsBusy = false;

            if (this.CanMap())
            {
                this.OpenMappingConfigurationManager();
            }
        }

        #endregion

        #region IHaveContextMenuViewModel interface

        /// <summary>
        /// Gets the Context Menu for this browser
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new ReactiveList<ContextMenuItemViewModel>();

        /// <summary>
        /// Gets the command that allows to map the selected part
        /// </summary>
        private ReactiveCommand<object> MapCommand { get; set; }

        /// <summary>
        /// Gets the command that allows to change the mappping configuration
        /// </summary>
        private ReactiveCommand<object> OpenMappingConfigurationManagerCommand { get; set; }

        /// <summary>
        /// Populate the context menu for this browser
        /// </summary>
        public void PopulateContextMenu()
        {
            ContextMenu.Clear();

            if (this.SelectedPart is { })
            {
                ContextMenu.Add(new ContextMenuItemViewModel(
                    $"Map {SelectedPart.Description}", "",
                    MapCommand,
                    MenuItemKind.Export,
                    ClassKind.NotThing)
                );
            }

            ContextMenu.Add(new ContextMenuItemViewModel(
                    "Change Mapping Configuration", "",
                    OpenMappingConfigurationManagerCommand,
                    MenuItemKind.None,
                    ClassKind.NotThing)
                );
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstObjectBrowserViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public DstObjectBrowserViewModel(IDstController dstController, INavigationService navigationService, IHubController hubController)
        {
            this.dstController = dstController;
            this.navigationService = navigationService;
            this.hubController = hubController;

            // Update HLR when new file is available
            this.WhenAnyValue(vm => vm.dstController.IsLoading)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHLR());

            // Show automatically the mapping configuration after:
            // a) STEP file loaded (at UpdateHLR() method)
            // b) Iteration is open
            this.WhenAnyValue(vm => vm.hubController.OpenIteration)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (this.CanMap())
                    {
                        this.OpenMappingConfigurationManager();
                    }
                }
                );

            // Update mapping under request, triggered by MappingConfigurationDialogViewModel
            CDPMessageBus.Current.Listen<ExternalIdentifierMapChangedEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.AssignMappingsToAllParts());

            // Update mapping under request, triggered by Transfer Cancelled
            CDPMessageBus.Current.Listen<UpdateHighLevelRepresentationTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.AssignMappingsToAllParts(x.Reset));

            InitializeCommands();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if general mapping is possible
        /// </summary>
        /// <returns>True if possible</returns>
        private bool CanMap()
        {
            return (this.Step3DHLR.Count > 0) &&
                this.hubController.OpenIteration != null &&
                this.dstController.MappingDirection is MappingDirection.FromDstToHub &&
                !this.IsBusy;
        }

        /// <summary>
        /// Initializes the <see cref="ICommand"/> of this view model
        /// </summary>
        private void InitializeCommands()
        {
            var canSelectExternalIdMap = this.WhenAny(
                vm => vm.Step3DHLR,
                vm => vm.hubController.OpenIteration,
                vm => vm.dstController.MappingDirection,
                vm => vm.isBusy,
                (hlr, iteration, mappingDirection, busy) =>
                    hlr.Value.Count > 0 && iteration.Value != null &&
                    mappingDirection.Value is MappingDirection.FromDstToHub &&
                    !busy.Value
                );

            OpenMappingConfigurationManagerCommand = ReactiveCommand.Create(canSelectExternalIdMap);
            OpenMappingConfigurationManagerCommand.Subscribe(_ => this.OpenMappingConfigurationManager());

            var canMap = this.WhenAny(
                vm => vm.SelectedPart,
                vm => vm.hubController.OpenIteration,
                vm => vm.dstController.MappingDirection,
                vm => vm.isBusy,
                (part, iteration, mappingDirection, busy) =>
                    part.Value != null && iteration.Value != null &&
                    mappingDirection.Value is MappingDirection.FromDstToHub &&
                    !busy.Value
                );

            MapCommand = ReactiveCommand.Create(canMap);
            MapCommand.Subscribe(_ => this.MapCommandExecute());
        }

        /// <summary>
        /// Executes the <see cref="MapCommand"/>
        /// </summary>
        /// <remarks>
        /// The mapping is performed only on the <see cref="SelectedPart"/> through
        /// the <see cref="MappingConfigurationDialog"/>
        /// </remarks>
        private void MapCommandExecute()
        {
            var viewModel = AppContainer.Container.Resolve<IMappingConfigurationDialogViewModel>();

            this.AssignMapping();

            viewModel.SetPart(this.SelectedPart);

            this.navigationService.ShowDialog<MappingConfigurationDialog, IMappingConfigurationDialogViewModel>(viewModel);
        }

        /// <summary>
        /// Opens the <see cref="MappingConfigurationManagerDialog"/>
        /// </summary>
        private void OpenMappingConfigurationManager()
        {
            this.navigationService.ShowDialog<MappingConfigurationManagerDialog>();
        }

        /// <summary>
        /// Assings a mapping configuration associated to the selected part
        /// </summary>
        /// <remarks>This method could be not required. Check in the future</remarks>
        private void AssignMapping()
        {
            if (this.dstController.ExternalIdentifierMap is null || this.SelectedPart is null)
            {
                return;
            }

            // Note: a change in the Mapping Configuration will not affect 
            // current Mapped parts... do not remove that status
            if (this.SelectedPart.MappingStatus != Step3DRowViewModel.MappingStatusType.Mapped)
            {
                this.SelectedPart.ResetMappingStatus();
            }

            this.SelectedPart.MappingConfigurations.Clear();
            this.SelectedPart.MappingConfigurations.AddRange(
                this.dstController.ExternalIdentifierMap.Correspondence.Where(
                    x => x.ExternalId == this.SelectedPart.ElementName ||
                         x.ExternalId == this.SelectedPart.ParameterName));

#if DEBUG_DST_OBJECT_BROWSER
            this.logger.Debug($"MappingConfigurations assigned");
            this.ShowMappingConfigurations(this.selectedPart);
#endif

            this.SelectedPart.UpdateMappingStatus();
        }

        /// <summary>
        /// Assings a mapping configuration associated to the selected part ignoring mapped ones
        /// </summary>
        private void AssignMappingsToAllParts(bool reset = false)
        {
            foreach (var part in this.Step3DHLR)
            {
                if (reset)
                {
                    part.ResetMappingStatus();
                }
                else
                {
                    // Note: a change in the Mapping Configuration will not affect current Mapped parts,
                    //       they remains until transfer action is performed.
                    if (part.MappingStatus != Step3DRowViewModel.MappingStatusType.Mapped)
                    {
                        part.ResetMappingStatus();
                    }
                }

                part.MappingConfigurations.Clear();

                if (this.dstController.ExternalIdentifierMap is { })
                {
                    part.MappingConfigurations.AddRange(
                        this.dstController.ExternalIdentifierMap.Correspondence.Where(
                            x => x.ExternalId == part.ElementName ||
                                 x.ExternalId == part.ParameterName));

                    part.UpdateMappingStatus();
                }
            }
        }

#if DEBUG_DST_OBJECT_BROWSER
        /// <summary>
        /// Shows the mapping configuration
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/></param>
        private void ShowMappingConfigurations(Step3DRowViewModel part)
        {
            this.logger.Debug($"MappingConfigurations for: {part.Description}");
            this.logger.Debug($"  Status: {part.MappingStatusMessage}");
            this.dstController.ShowCorrespondences(part.MappingConfigurations);
        }
#endif

        #endregion
    }
}
