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
        /// Gets the collection of <see cref="IdCorrespondences"/> tagged as used in the current mapping process
        /// </summary>
        public List<IdCorrespondence> UsedIdCorrespondences { get; } = new List<IdCorrespondence>();

        /// <summary>
        /// Gets the collection of <see cref="IdCorrespondences"/> for all mapping configurations before the mapping process
        /// </summary>
        public List<IdCorrespondence> PreviousIdCorrespondences { get; } = new List<IdCorrespondence>();

        /// <summary>
        /// Adds mapping configurations used to detect the not used ones in the mapping process
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> from which store the current mapping</param>
        private void AddPreviousIdCorrespondances(Step3DRowViewModel part)
        {
            this.PreviousIdCorrespondences.Clear();
            PreviousIdCorrespondences.AddRange(part.MappingConfigurations);
        }

        /// <summary>
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        public void Map(Step3DRowViewModel part)
        {
            var parts = new List<Step3DRowViewModel> { part };

            this.AddPreviousIdCorrespondances(part);

            var (elements, sources) = ((List<ElementBase>, List<Step3DTargetSourceParameter>))
                this.mappingEngine.Map(parts);

            if (elements.Any())
            {
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
                // No transfer to STEP AP242 file is available
            }
        }

        /// <summary>
        /// Helper method for debugging in console the content of <see cref="IdCorrespondence"/>
        /// </summary>
        /// <param name="correspondences">The <see cref="IEnumerable{IdCorrespondence}"/> of correspondances</param>
        public void ShowCorrespondances(IEnumerable<IdCorrespondence> correspondences)
        {
            foreach (var c in correspondences)
            {
                this.ShowCorrespondance(c);
            }
        }

        /// <summary>
        /// Helper method for debugging in console the content of <see cref="IdCorrespondence"/>
        /// </summary>
        /// <param name="correspondences">The <see cref="IdCorrespondence"/> of correspondances</param>
        public void ShowCorrespondance(IdCorrespondence correspondence)
        {
            string thingType = "UNKNOWN";

            if (this.hubController.GetThingById(correspondence.InternalThing, this.hubController.OpenIteration, out Thing thing))
            {
                thingType = $"{thing}";
            }

            Debug.WriteLine($"  CorrespondanceId = {correspondence.Iid}");
            Debug.WriteLine($"  ExternalId       = {correspondence.ExternalId}");
            Debug.WriteLine($"  InternalThing    = {correspondence.InternalThing} --> {thingType}");
            Debug.WriteLine($"  IsCached         = {correspondence.IsCached()}");
        }

        /// <summary>
        /// Merges new mappings and unused mappings into the current <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <remarks>
        /// The new mappings are taken from <see cref="UsedIdCorrespondences"/> mappings.
        /// The unused mappings are calculated from <see cref="PreviousIdCorrespondences"/> and <see cref="UsedIdCorrespondences"/> mappings.
        /// 
        /// All of those lists are cleaned at the end.
        /// </remarks>
        public void UpdateExternalIdentifierMap()
        {
            var previousCorrespondances = this.PreviousIdCorrespondences.Distinct();
            var unusedCorrespondances = previousCorrespondances.Where(x => !this.UsedIdCorrespondences.Contains(x));

#if DEBUG_EXTERNAL_IDENTITIER_MAP
            Debug.WriteLine("\nUpdateExternalIdentifierMap IdCorrespondances");
            this.ShowCorrespondances(this.IdCorrespondences);

            Debug.WriteLine("\nUpdateExternalIdentifierMap UsedIdCorrespondances");
            this.ShowCorrespondances(this.UsedIdCorrespondences);


            Debug.WriteLine("\nUpdateExternalIdentifierMap previousCorrespondances");
            this.ShowCorrespondances(previousCorrespondances);

            Debug.WriteLine("\nUpdateExternalIdentifierMap unusedCorrespondances");
            this.ShowCorrespondances(unusedCorrespondances);
#endif

            foreach (var unused in unusedCorrespondances)
            {
#if DEBUG_EXTERNAL_IDENTITIER_MAP
                Debug.WriteLine($"Removing unusedCorrespondance {unused}");
#endif
                this.ExternalIdentifierMap.Correspondence.Remove(unused);
            }

            foreach (var item in this.IdCorrespondences)
            {
                var clonedCorrespondance  = item.Clone(false);
                this.ExternalIdentifierMap.Correspondence.Add(clonedCorrespondance);

#if DEBUG_EXTERNAL_IDENTITIER_MAP
                Debug.WriteLine($"no Adding clonedCorrespondance {clonedCorrespondance}");
                this.ShowCorrespondance(clonedCorrespondance);
#endif
            }

            this.IdCorrespondences.Clear();
            this.UsedIdCorrespondences.Clear();
            this.PreviousIdCorrespondences.Clear();
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
        /// Adds one correspondance to the <see cref="IDstController.IdCorrespondences"/> if does not already exists
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        public void AddToExternalIdentifierMap(Guid internalId, string externalId)
        {
            if (internalId == Guid.Empty)
            {
                return;
            }

            if (this.ExternalIdentifierMap.Correspondence
                .FirstOrDefault(x => x.ExternalId == externalId && x.InternalThing == internalId) is { } correspondence)
            {
                this.UsedIdCorrespondences.Add(correspondence);
                return;
            }

            if (this.IdCorrespondences
                .FirstOrDefault(x => x.ExternalId == externalId && x.InternalThing == internalId) is { } tempCorrespondence)
            {
                this.UsedIdCorrespondences.Add(tempCorrespondence);
            }
            else
            {
                var newCorrespondence = new IdCorrespondence()
                {
                    ExternalId = externalId,
                    InternalThing = internalId
                };

                this.IdCorrespondences.Add(newCorrespondence);
                this.UsedIdCorrespondences.Add(newCorrespondence);
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
                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfer to Hub failed: {e.Message}"));
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

            this.UpdateParametersValueSets(transaction, this.MapResult.OfType<ElementDefinition>().SelectMany(e => e.Parameter));
            this.UpdateParametersValueSets(transaction, this.MapResult.OfType<ElementUsage>().SelectMany(eu => eu.ParameterOverride));

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

                var newParameterCloned = newParameter.Clone(false);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(newParameterCloned);
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
#if DEBUG_SHOW_CORRESPONDENCES
            this.ShowCorrespondances(this.ExternalIdentifierMap.Correspondence);
#endif
            
            if (this.ExternalIdentifierMap.Iid == Guid.Empty)
            {
                this.ExternalIdentifierMap = this.ExternalIdentifierMap.Clone(true);
                this.ExternalIdentifierMap.Iid = Guid.NewGuid();
                iterationClone.ExternalIdentifierMap.Add(this.ExternalIdentifierMap);
            }

            foreach (var correspondence in this.ExternalIdentifierMap.Correspondence)
            {
#if DEBUG_SHOW_CORRESPONDENCES
                this.ShowCorrespondance(correspondence);
#endif

                if (correspondence.Iid == Guid.Empty)
                {
                    correspondence.Iid = Guid.NewGuid();
                    transaction.Create(correspondence);
                }
                else
                {
                    transaction.CreateOrUpdate(correspondence.Clone(false));
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
