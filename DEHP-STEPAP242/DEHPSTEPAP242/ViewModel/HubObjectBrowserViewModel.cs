
namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;

    using ReactiveUI;
    using NLog;

    using CDP4Dal;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.Events;


    /// <summary>
    /// The <see cref="HubObjectBrowserViewModel"/> is a specialization 
    /// of <see cref="ObjectBrowserViewModel"/> to add context menue features.
    /// 
    /// </summary>
    public class HubObjectBrowserViewModel : ObjectBrowserViewModel, IHubObjectBrowserViewModel
    {
        /// <summary>
        /// The current class logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;

        /// <summary>
        /// Last selected <see cref="System.Guid"/> from the "sources" parameter
        /// </summary>
        private string fileRevisionId;

        /// <summary>
        /// The <see cref="ReactiveCommand{T}"/> for download the <see cref="FileRevision"/> 
        /// corresponding to the <see cref="fileRevisionId"/> selected
        /// </summary>
        private ReactiveCommand<object> DownloadGuidCommand;

        public HubObjectBrowserViewModel(IDstController dstController, IDstHubService dstHubService,
            IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService) : base(hubController, objectBrowserTreeSelectorService)
        {
            this.dstController = dstController;
            this.dstHubService = dstHubService;

            this.InitializesCommandsAndObservableSubscriptions();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            this.MapCommand = ReactiveCommand.Create();
            this.MapCommand.Subscribe(_ => Logger.Debug("No Mapping from Hub to Dst"));

            this.DownloadGuidCommand = ReactiveCommand.Create();
            this.DownloadGuidCommand.Subscribe(_ => CDPMessageBus.Current.SendMessage(new DownloadFileRevisionEvent(this.fileRevisionId)));
        }

        /// <summary>
        /// Show information about STEP 3D Geometry association
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.fileRevisionId = string.Empty;
            this.ContextMenu.Clear();

            if (this.SelectedThing == null)
            {
                return;
            }

            // Working on the last one is the most user friendly decission
            switch (this.SelectedThings.LastOrDefault())
            {
                case ParameterRowViewModel parameter:
                {
                    this.ProcessParameterRowViewModel(parameter);
                }
                break;

                case ParameterComponentValueRowViewModel component:
                {
                    this.ProcessParameterComponentValueRowViewModel(component);
                }
                break;

                case ElementDefinitionRowViewModel elementDefinition:
                {
                    this.ProcessElementDefinitionRowViewModel(elementDefinition);
                }
                break;

                default:
                    //TODO: add processing for ElementUsages
                return;
            }
        }

        /// <summary>
        /// Creates context menu for a <see cref="ParameterOverride"/> if it is an STEP 3D parameter.
        /// 
        /// <seealso cref="IDstHubService.IsSTEPParameterType"/>
        /// </summary>
        /// <param name="parameter"><see cref="ParameterOverride"/> </param>
        private void ProcessParameterContextMenu(ParameterOrOverrideBase parameter)
        {
            if (!this.dstHubService.IsSTEPParameterType(parameter.ParameterType))
            {
                return;
            }

            try
            {
                IValueSet valueSet = parameter.ValueSets.LastOrDefault();
                var valuearray = valueSet.Computed;

                CompoundParameterType compound = (CompoundParameterType)parameter.ParameterType;

                var name_component = compound.Component.FirstOrDefault(x => x.ShortName == "name");
                var source_component = compound.Component.FirstOrDefault(x => x.ShortName == "source");

                var part_name = valuearray[name_component.Index];
                var part_filereference = valuearray[source_component.Index];

                this.fileRevisionId = part_filereference;

                this.ContextMenu.Add(new ContextMenuItemViewModel(
                    $"Download Associated STEP 3D file to \"{part_name}\" {part_filereference}", "", 
                    this.DownloadGuidCommand,
                    MenuItemKind.Export, ClassKind.NotThing));
            }
            catch (Exception e)
            {
                Logger.Warn(e, "Ignoring context menue creation for ParameterOrOverride");
            }
        }

        /// <summary>
        /// Creates the context menue if applicable.
        /// </summary>
        /// <param name="row"><see cref="ParameterRowViewModel"/></param>
        private void ProcessParameterRowViewModel(ParameterRowViewModel row)
        {
            this.ProcessParameterContextMenu(row.Thing);
        }

        /// <summary>
        /// Creates the context menue if applicable.
        /// </summary>
        /// <param name="row"><see cref="ParameterComponentValueRowViewModel"/></param>
        private void ProcessParameterComponentValueRowViewModel(ParameterComponentValueRowViewModel row)
        {
            if (row.ContainerViewModel is ParameterRowViewModel parameter)
            {
                this.ProcessParameterRowViewModel(parameter);
            }
        }

        /// <summary>
        /// Creates the context menue if applicable.
        /// </summary>
        /// <param name="row"><see cref="ElementDefinitionRowViewModel"/></param>
        private void ProcessElementDefinitionRowViewModel(ElementDefinitionRowViewModel row)
        {
            foreach (var irow in row.ContainedRows)
            {
                // Find if there is a child row with the STEP geometrical information
                if (irow is ParameterRowViewModel parameter)
                {
                    if (this.dstHubService.IsSTEPParameterType(parameter.Thing.ParameterType))
                    {
                        this.ProcessParameterContextMenu(parameter.Thing);
                        break;
                    }
                }
            }
        }
    }
}
