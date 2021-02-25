// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
// 
//    This file is part of DEHPSTEPAP242
// 
//    The DEHPSTEPAP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHPSTEPAP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPSTEPAP242.DstController
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    
    using ReactiveUI;
    using NLog;

    using CDP4Dal;
    using CDP4Dal.Operations;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.CommonData;
    using CDP4Common.Types;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Events;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.ViewModel.Rows;
    using DEHPSTEPAP242.Services.DstHubService;
    using DEHPSTEPAP242.MappingRules;

    using STEP3DAdapter;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Views;
    using System.Runtime.ExceptionServices;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data 
    /// from and to STEP AP242 file.
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IMappingEngine"/>
        /// </summary>
        private readonly IMappingEngine mappingEngine;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// The <see cref="IDstHubService"/>
        /// </summary>
        private readonly IDstHubService dstHubService;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region IDstController interface

        /// <summary>
        /// Gets this running tool name
        /// </summary>
        public string ThisToolName => this.GetType().Assembly.GetName().Name;

        /// <summary>
        /// Backing field for <see cref="Step3DFile"/>
        /// </summary>
        private STEP3DFile step3dFile;

        /// <summary>
        /// Gets or sets the <see cref="STEP3DFile"/> instance.
        /// 
        /// <seealso cref="Load"/>
        /// <seealso cref="LoadAsync"/>
        /// </summary>
        public STEP3DFile Step3DFile
        { 
            get => step3dFile;
            private set => step3dFile = value;
        }

        /// <summary>
        /// Returns the status of the last load action.
        /// </summary>
        public bool IsFileOpen => step3dFile?.HasFailed == false;

        /// <summary>
        /// The <see cref="IsLoading"/> that indicates the loading status flag.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// Gets or sets the status flag for the load action.
        /// </summary>
        public bool IsLoading
        {
            get => isLoading;
            private set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }

        /// <summary>
        /// Loads a STEP-AP242 file.
        /// <param name="filename">Full path to a STEP-AP242 file</param>
        public void Load(string filename)
        {
            IsLoading = true;

            var step = new STEP3DFile(filename);

            if (step.HasFailed)
            {
                IsLoading = false;

                // In case of error the current Step3DFile is not updated (keep previous)
                logger.Error($"Error loading STEP file: { step.ErrorMessage }");

                throw new InvalidOperationException($"Error loading STEP file: { step.ErrorMessage }");
            }

            // Update the new instance only when a load success
            Step3DFile = step;

            IsLoading = false;
        }

        /// <summary>
        /// Loads a STEP-AP242 file asynchronously.
        /// </summary>
        /// <param name="filename">Full path to a STEP-AP242 file</param>
        public async Task LoadAsync(string filename)
        {
            await Task.Run( () => Load(filename) );
        }

        /// <summary>
        /// Backing field for the <see cref="MappingDirection"/>
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/>
        /// </summary>
        public MappingDirection MappingDirection
        {
            get => this.mappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.mappingDirection, value);
        }

        /// <summary>
        /// Gets the collection of <see cref="ExternalIdentifierMap"/>s
        /// </summary>
        public IEnumerable<ExternalIdentifierMap> AvailablExternalIdentifierMap =>
            this.hubController.AvailableExternalIdentifierMap(this.ThisToolName);

        /// <summary>
        /// Gets the colection of mapped <see cref="ElementDefinition"/>s and <see cref="Parameter"/>s
        /// </summary>
        public ReactiveList<ElementBase> MapResult { get; private set; } = new ReactiveList<ElementBase>();

        /// <summary>
        /// Gets the colection of mapped <see cref="Step3DTargetSourceParameter"/> which needs to be updated in the transfer operation
        /// </summary>
        public List<Step3DTargetSourceParameter> TargetSourceParametersDstStep3dMaps { get; private set; } = new List<Step3DTargetSourceParameter>();

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap ExternalIdentifierMap { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="IdCorrespondences"/>
        /// </summary>
        public List<IdCorrespondence> IdCorrespondences { get; } = new List<IdCorrespondence>();

        /// <summary>
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dst3DPart">The <see cref="Step3DRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        public void Map(Step3DRowViewModel dst3DPart)
        {
            var parts = new List<Step3DRowViewModel> { dst3DPart };

            var (elements, sources) = ((List<ElementBase>, List<Step3DTargetSourceParameter>))
                this.mappingEngine.Map(parts);

            if (elements.Any())
            {
#if REMOVE_PREVIOUS_MAPPED_ELEMENTS
                foreach (var e in elements)
                {
                    // Remove previous mapping entries (keep only one)
                    ElementBase elementOnMap = null;

                    // New ElementDefinitions do not have Guid, look by name is required
                    if (e.Iid == Guid.Empty)
                    {
                        elementOnMap = this.MapResult.FirstOrDefault(x => x.Name == e.Name);
                    }
                    else
                    {
                        elementOnMap = this.MapResult.FirstOrDefault(x => x.Iid == e.Iid);
                    }

                    if (elementOnMap is { })
                    {
                        this.MapResult.Remove(elementOnMap);
                    }

                    //this.MapResult.Remove(this.MapResult.FirstOrDefault(x => x.Iid == e.Iid && x.Name == e.Name));
                }
#endif
                this.MapResult.AddRange(elements);
                this.TargetSourceParametersDstStep3dMaps.AddRange(sources);

                this.UpdateExternalIdentifierMap();
                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
            }
        }

        /// <summary>
        /// Transfers the mapped parts to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Transfer()
        {
            if (this.MappingDirection == MappingDirection.FromDstToHub)
            {
                await this.TransferMappedThingsToHub();
            }
            else
            {
                //TODO: nothing in that direction
            }
        }

        /// <summary>
        /// Updates the configured mapping 
        /// </summary>
        public void UpdateExternalIdentifierMap()
        {
            this.ExternalIdentifierMap.Correspondence.AddRange(this.IdCorrespondences);
            this.IdCorrespondences.Clear();
        }

        /// <summary>
        /// Creates and sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="ExternalIdentifierMap"/></param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap"/></returns>
        public ExternalIdentifierMap CreateExternalIdentifierMap(string newName)
        {
            return new ExternalIdentifierMap()
            {
                Name = newName,
                ExternalToolName = this.ThisToolName,
                ExternalModelName = newName,
                Owner = this.hubController.CurrentDomainOfExpertise
            };
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="IDstController.IdCorrespondences"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        public void AddToExternalIdentifierMap(Guid internalId, string externalId)
        {
            if (internalId != Guid.Empty && 
                !this.ExternalIdentifierMap.Correspondence.Any(x => x.ExternalId == externalId && x.InternalThing == internalId) &&
                !this.IdCorrespondences.Any(x => x.ExternalId == externalId && x.InternalThing == internalId))
            {
                this.IdCorrespondences.Add(new IdCorrespondence()
                {
                    ExternalId = externalId,
                    InternalThing = internalId
                });
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="mappingEngine">The <<see cref="IMappingEngine"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine,
            INavigationService navigationService, IDstHubService dstHubService, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
            this.navigationService = navigationService;
            this.dstHubService = dstHubService;
            this.statusBar = statusBar;
        }

        #endregion

        #region Private Transfer Methods

        /// <summary>
        /// Initializes a new <see cref="IThingTransaction"/> based on the current open <see cref="Iteration"/>
        /// </summary>
        /// <returns>A <see cref="ValueTuple"/> Containing the <see cref="Iteration"/> clone and the <see cref="IThingTransaction"/></returns>
        private (Iteration clone, ThingTransaction transaction) GetIterationTransaction()
        {
            var iterationClone = this.hubController.OpenIteration.Clone(false);
            return (iterationClone, new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone));
        }

        /// <summary>
        /// Transfers the input file and mapped parts to the Hub
        /// 
        /// Workflow:
        /// 1. Update current STEP file to the Hub
        /// 2. Get Uuid of its FileRevision and update "source" parameters
        /// 3. Send CreateOrUpdate for ED/EU
        /// 4. Update ValueSets
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task TransferMappedThingsToHub()
        {
            try
            {
                var iterationClone = this.hubController.OpenIteration.Clone(false);
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone);

                if (!(this.MapResult.Any() && this.TrySupplyingAndCreatingLogEntry(transaction)))
                {
                    return;
                }

                // Step 1: upload file
                string filePath = Step3DFile.FileName;
                var file = this.dstHubService.FindFile(filePath);

                //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Uploading STEP file to Hub: {filePath}"));
                await this.hubController.Upload(filePath, file);

                // Step 2: update Step3dParameter.source with FileRevision from uploaded file
                file = this.dstHubService.FindFile(filePath);
                var fileRevision = file.CurrentFileRevision;

                //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Updating STEP file references Guid to: {fileRevision.Iid}"));
                foreach (var sourceFieldToUpdate in this.TargetSourceParametersDstStep3dMaps)
                {
                    sourceFieldToUpdate.UpdateSource(fileRevision);
                }

                // Step 3: create/update things
                //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfering {this.MapResult.Count} ElementDefinitions..."));
                foreach (var elementBase in this.MapResult)
                {
                    if (elementBase is ElementDefinition elementDefinition)
                    {
                        var elementDefinitionCloned = this.TransactionCreateOrUpdate(transaction, elementDefinition, iterationClone.Element);

                        foreach (var parameter in elementDefinition.Parameter)
                        {
                            this.TransactionCreateOrUpdate(transaction, parameter, elementDefinitionCloned.Parameter);
                        }
                    }
                    else if (elementBase is ElementUsage elementUsage)
                    {
                        foreach (var parameterOverride in elementUsage.ParameterOverride)
                        {
                            var elementUsageClone = elementUsage.Clone(false);
                            transaction.CreateOrUpdate(elementUsageClone);
                            this.TransactionCreateOrUpdate(transaction, parameterOverride, elementUsageClone.ParameterOverride);
                        }
                    }
                }

                /*
                foreach (var elementDefinition in this.MapResult)
                {
                    var elementDefinitionCloned = this.TransactionCreateOrUpdate(transaction, elementDefinition, iterationClone.Element);

                    foreach (var parameter in elementDefinition.Parameter)
                    {
                        this.TransactionCreateOrUpdate(transaction, parameter, elementDefinitionCloned.Parameter);
                    }

                    foreach (var parameterOverride in elementDefinition.ContainedElement.SelectMany(x => x.ParameterOverride))
                    {
                        var elementUsageClone = (ElementUsage)parameterOverride.Container.Clone(false);
                        transaction.CreateOrUpdate(elementUsageClone);

                        this.TransactionCreateOrUpdate(transaction, parameterOverride, elementUsageClone.ParameterOverride);
                    }
                }
                */

                this.PersistExternalIdentifierMap(transaction, iterationClone);

                transaction.CreateOrUpdate(iterationClone);

                await this.hubController.Write(transaction);

                // Update ValueSet after the commit.
                // The ValueArray are constructed with the correct size
                // in the last HubController.Write(transaction) call.

                //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfering ValueSets..."));
                await this.UpdateParametersValueSets();

                foreach (var sourceFieldToUpdate in this.TargetSourceParametersDstStep3dMaps)
                {
                    sourceFieldToUpdate.part.SetTransferedStatus();
                }

                //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfer to Hub done"));
                await this.hubController.Refresh();
                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));

                this.MapResult.Clear();
                this.TargetSourceParametersDstStep3dMaps.Clear();
            }
            catch (Exception e)
            {
                this.logger.Error(e);
                ExceptionDispatchInfo.Capture(e).Throw();
                //Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfer to Hub failed: {e.Message}"));
                throw;
            }
        }

        /// <summary>
        /// Registers the provided <paramref cref="Thing"/> to be created or updated by the <paramref name="transaction"/>
        /// </summary>
        /// <typeparam name="TThing">The type of the <paramref name="containerClone"/></typeparam>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="thing">The <see cref="Thing"/></param>
        /// <param name="containerClone">The <see cref="ContainerList{T}"/> of the cloned container</param>
        /// <returns>A cloned <typeparamref name="TThing"/></returns>
        private TThing TransactionCreateOrUpdate<TThing>(IThingTransaction transaction, TThing thing, ContainerList<TThing> containerClone) where TThing : Thing
        {
            var clone = thing.Clone(false);

            if (clone.Iid == Guid.Empty)
            {
                clone.Iid = Guid.NewGuid();
                thing.Iid = clone.Iid;
                transaction.Create(clone);
                containerClone.Add((TThing)clone);
                this.AddIdCorrespondence(clone);
            }
            else
            {
                transaction.CreateOrUpdate(clone);
            }

            return (TThing)clone;
        }

        /// <summary>
        /// If the <see cref="Thing"/> is new save the mapping
        /// </summary>
        /// <param name="clone">The <see cref="Thing"/></param>
        private void AddIdCorrespondence(Thing clone)
        {
            string externalId;

            switch (clone)
            {
                case INamedThing namedThing:
                    externalId = namedThing.Name;
                    break;
                case ParameterOrOverrideBase parameterOrOverride:
                    externalId = parameterOrOverride.ParameterType.Name;
                    break;
                default:
                    return;
            }

            this.IdCorrespondences.Add(new IdCorrespondence(Guid.NewGuid(), null, null)
            {
                ExternalId = externalId,
                InternalThing = clone.Iid
            });
        }

        /// <summary>
        /// Updates the <see cref="IValueSet"/> of all <see cref="Parameter"/> and all <see cref="ParameterOverride"/>
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task UpdateParametersValueSets()
        {
            var (iterationClone, transaction) = this.GetIterationTransaction();

            //var elementDefinitions = this.MapResult.SelectMany(x => x is ElementDefinition);

            this.UpdateParametersValueSets(transaction, this.MapResult.Where(x => x is ElementDefinition).Select(eb=>(ElementDefinition)eb).SelectMany(e => e.Parameter));
            this.UpdateParametersValueSets(transaction, this.MapResult.Where(x => x is ElementUsage).Select(eb => (ElementUsage)eb).SelectMany(eu => eu.ParameterOverride));

            transaction.CreateOrUpdate(iterationClone);
            await this.hubController.Write(transaction);
        }

        /// <summary>
        /// Updates the specified <see cref="Parameter"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="transaction">the <see cref="IThingTransaction"/></param>
        /// <param name="parameters">The collection of <see cref="Parameter"/></param>
        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<Parameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out Parameter newParameter);
                var container = newParameter.Clone(false);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);
            }
        }

        /// <summary>
        /// Updates the specified <see cref="ParameterOverride"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="transaction">the <see cref="IThingTransaction"/></param>
        /// <param name="parameters">The collection of <see cref="ParameterOverride"/></param>
        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<ParameterOverride> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out ParameterOverride newParameter);
                var newParameterClone = newParameter.Clone(true);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameterClone.ValueSet[index];
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(newParameterClone);
            }
        }

        /// <summary>
        /// Sets the value of the <paramref name="valueSet"></paramref> to the <paramref name="clone"/>
        /// </summary>
        /// <param name="clone">The clone to update</param>
        /// <param name="valueSet">The <see cref="IValueSet"/> of reference</param>
        private static void UpdateValueSet(ParameterValueSetBase clone, IValueSet valueSet)
        {
            // Only Computed IValueSet is being updated
            clone.Computed = valueSet.Computed;
            clone.ValueSwitch = valueSet.ValueSwitch;
        }

        /// <summary>
        /// Updates the configured mapping, registering the <see cref="ExternalIdentifierMap"/> and its <see cref="IdCorrespondence"/>
        /// to a <see name="IThingTransaction"/>
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="iterationClone">The <see cref="Iteration"/> clone</param>
        private void PersistExternalIdentifierMap(IThingTransaction transaction, Iteration iterationClone)
        {
            this.UpdateExternalIdentifierMap();

            if (this.ExternalIdentifierMap.Iid == Guid.Empty)
            {
                this.ExternalIdentifierMap.Iid = Guid.NewGuid();
                iterationClone.ExternalIdentifierMap.Add(this.ExternalIdentifierMap);
            }

            foreach (var correspondence in this.ExternalIdentifierMap.Correspondence)
            {
                if (correspondence.Iid == Guid.Empty)
                {
                    correspondence.Iid = Guid.NewGuid();
                    transaction.Create(correspondence);
                }
            }

            transaction.CreateOrUpdate(this.ExternalIdentifierMap);

            this.statusBar.Append("Mapping configuration processed");
        }

        /// <summary>
        /// Pops the <see cref="CreateLogEntryDialog"/> and based on its result, either registers a new ModelLogEntry to the <see cref="transaction"/> or not
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction"/> that will get the changes registered to</param>
        /// <returns>A boolean result, true if the user pressed OK, otherwise false</returns>
        private bool TrySupplyingAndCreatingLogEntry(ThingTransaction transaction)
        {
            var vm = new CreateLogEntryDialogViewModel();

            var dialogResult = this.navigationService.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(vm);

            if (dialogResult != true)
            {
                return false;
            }

            this.hubController.RegisterNewLogEntryToTransaction(vm.LogEntryContent, transaction);
            return true;
        }

        #endregion
    }
}
