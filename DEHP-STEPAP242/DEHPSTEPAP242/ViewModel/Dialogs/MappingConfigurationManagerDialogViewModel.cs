
namespace DEHPSTEPAP242.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reactive;

    using ReactiveUI;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;

    using DEHPSTEPAP242.ViewModel.Dialogs.Interfaces;
    using DEHPSTEPAP242.Settings;
    using CDP4Common.EngineeringModelData;
    using DEHPSTEPAP242.DstController;

    /// <summary>
    /// The view-model for the Mapping Configuration Manager dialog that allows users
    /// to select the DST to HUB mappings.
    /// </summary>
    public class MappingConfigurationManagerDialogViewModel : ReactiveObject, IMappingConfigurationManagerDialogViewModel, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/> instance
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IUserPreferenceService{AppSettings}"/> instance
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;

        /// <summary>
        /// The file name without extension of the current STEP file
        /// </summary>
        private string fileName;

        /// <summary>
        /// Backing field for <see cref="NewExternalIdentifierMapName"/>
        /// </summary>
        private string newExternalIdentifierMapName;

        /// <summary>
        /// Gets or sets the current path to a STEP file.
        /// </summary>
        public string NewExternalIdentifierMapName
        {
            get => newExternalIdentifierMapName;
            set => this.RaiseAndSetIfChanged(ref this.newExternalIdentifierMapName, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedExternalIdentifierMap"/>
        /// </summary>
        private ExternalIdentifierMap selectedExternalIdentifierMap;

        /// <summary>
        /// Gets or sets the selected <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap SelectedExternalIdentifierMap
        {
            get => this.selectedExternalIdentifierMap;
            set => this.RaiseAndSetIfChanged(ref this.selectedExternalIdentifierMap, value);
        }

        /// <summary>
        /// Backing field for <see cref="CreateNewMappingConfigurationChecked"/>
        /// </summary>
        private bool createNewMappingConfigurationChecked;

        /// <summary>
        /// Gets or sets the checked checkbox assert that selects that a new mapping configuration will be created
        /// </summary>
        public bool CreateNewMappingConfigurationChecked
        {
            get => this.createNewMappingConfigurationChecked;
            set => this.RaiseAndSetIfChanged(ref this.createNewMappingConfigurationChecked, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ReactiveList{T}"/> of available <see cref="ExternalIdentifierMap"/>
        /// for this DST.
        /// </summary>
        public ReactiveList<ExternalIdentifierMap> AvailableExternalIdentifierMap { get; set; }

        /// <summary>
        /// Gets the continue command
        /// </summary>
        public ReactiveCommand<object> ApplyCommand { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        public MappingConfigurationManagerDialogViewModel(IDstController dstController, IHubController hubController, IUserPreferenceService<AppSettings> userPreferenceService)
        {
            this.dstController = dstController;
            this.hubController = hubController;
            this.userPreferenceService = userPreferenceService;

            this.fileName = System.IO.Path.GetFileNameWithoutExtension(this.dstController.Step3DFile?.FileName).ToLower();

            InitializeUI();
            InitializeCommands();
        }

        /// <summary>
        /// Initializes the User Interface components
        /// </summary>
        private void InitializeUI()
        {
            this.AvailableExternalIdentifierMap = new ReactiveList<ExternalIdentifierMap>(
                hubController.AvailableExternalIdentifierMap(this.dstController.ThisToolName));

            // Select the ExternalIdentifierMap used previously for current STEP file name
            var usedMappingName = string.Empty;

            this.userPreferenceService.Read();

            if (this.userPreferenceService.UserPreferenceSettings.MappingUsedByFiles.TryGetValue(this.fileName, out usedMappingName))
            {
                this.SelectedExternalIdentifierMap = this.AvailableExternalIdentifierMap.FirstOrDefault(
                    x => x.Name == usedMappingName);
                
                // In case the Configuration name is not found, propose the creation
                if (this.SelectedExternalIdentifierMap is null)
                {
                    this.CreateNewMappingConfigurationChecked = true;
                    this.NewExternalIdentifierMapName = usedMappingName;
                }
            }
            else if (this.AvailableExternalIdentifierMap.Count == 0)
            {
                // Create new configuration is the only possibility here,
                // propose the file name as initial name
                this.CreateNewMappingConfigurationChecked = true;
                this.NewExternalIdentifierMapName = $"{System.IO.Path.GetFileNameWithoutExtension(this.dstController.Step3DFile?.FileName)} Configuration";
            }
        }

        /// <summary>
        /// Initializes Commands
        /// </summary>
        private void InitializeCommands()
        {
            var canApply = this.WhenAnyValue(
                vm => vm.CreateNewMappingConfigurationChecked,
                vm => vm.NewExternalIdentifierMapName,
                vm => vm.SelectedExternalIdentifierMap,
                (newChecker, newName, selectedMap) => 
                    (newChecker && !string.IsNullOrWhiteSpace(newName)) || 
                    (!newChecker && selectedMap != null)
                );

            this.ApplyCommand = ReactiveCommand.Create(canApply);
            this.ApplyCommand.Subscribe(_ => this.ApplyCommandExecute());
        }

        /// <summary>
        /// Applies the selection of <see cref="ExternalIdentifierMap"/> to the current STEP file
        /// </summary>
        private void ApplyCommandExecute()
        {
            this.ProcessExternalIdentifierMap();
            this.SaveMappingAssociation();
            this.CloseWindowBehavior?.Close();
        }

        /// <summary>
        /// Creates a new <see cref="ExternalIdentifierMap"/> and or sets the <see cref="IDstController.ExternalIdentifierMap"/>
        /// </summary>
        private void ProcessExternalIdentifierMap()
        {
            if (this.CreateNewMappingConfigurationChecked)
            {
                this.dstController.ExternalIdentifierMap = this.dstController.CreateExternalIdentifierMap(this.NewExternalIdentifierMapName);
            }
            else
            {
                this.dstController.ExternalIdentifierMap = this.SelectedExternalIdentifierMap.Clone(false);
            }
        }

        /// <summary>
        /// Save the association between <see cref="fileName"/> and the current <see cref="ExternalIdentifierMap"/>
        /// </summary>
        private void SaveMappingAssociation()
        {
            this.userPreferenceService.UserPreferenceSettings.MappingUsedByFiles[this.fileName] = this.dstController.ExternalIdentifierMap.Name;
            this.userPreferenceService.Save();
        }
    }
}
