
#define USE_CANMAP_OBSERVABLE

namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;
    using ReactiveUI;

    using CDP4Common.CommonData;
    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Views.Dialogs;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.Builds.HighLevelRepresentationBuilder;

    using STEP3DAdapter;
    using System.Reactive.Linq;
    using System.Windows;
    using DEHPSTEPAP242.ViewModel.Dialogs;
    using CDP4Dal;
    using DEHPSTEPAP242.Events;

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
                    hlr.Value.Count>0 && iteration.Value != null &&
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
        /// the <see cref="IMappingConfigurationDialogViewModel"/>
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

            this.SelectedPart.UpdateMappingStatus();
        }

        /// <summary>
        /// Assings a mapping configuration associated to the selected part
        /// </summary>
        private void AssignMappingsToAllParts()
        {
            foreach (var part in this.Step3DHLR)
            {
                // Note: a change in the Mapping Configuration will not affect current Mapped parts,
                //       they remains until transfer action is performed.
                if (part.MappingStatus != Step3DRowViewModel.MappingStatusType.Mapped)
                {
                    part.ResetMappingStatus();
                }

                part.MappingConfigurations.Clear();
                part.MappingConfigurations.AddRange(
                    this.dstController.ExternalIdentifierMap.Correspondence.Where(
                        x => x.ExternalId == part.ElementName ||
                             x.ExternalId == part.ParameterName));

                part.UpdateMappingStatus();
            }
        }

        #endregion
    }
}
