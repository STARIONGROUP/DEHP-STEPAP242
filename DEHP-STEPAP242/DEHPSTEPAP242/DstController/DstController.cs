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

using CDP4Common;
using CDP4Common.CommonData;
using CDP4Common.EngineeringModelData;
using CDP4Common.Types;
using CDP4Dal;
using CDP4Dal.Operations;
using DEHPCommon.Enumerators;
using DEHPCommon.Events;
using DEHPCommon.HubController.Interfaces;
using DEHPCommon.MappingEngine;
using DEHPCommon.Services.ExchangeHistory;
using DEHPCommon.Services.NavigationService;
using DEHPCommon.UserInterfaces.ViewModels;
using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
using DEHPCommon.UserInterfaces.Views;
using DEHPSTEPAP242.Events;
using DEHPSTEPAP242.Services.DstHubService;
using DEHPSTEPAP242.ViewModel.Rows;
using NLog;
using ReactiveUI;
using STEP3DAdapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DEHPSTEPAP242.DstController
{
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
        /// The <see cref="IExchangeHistoryService"/>
        /// </summary>
        private readonly IExchangeHistoryService exchangeHistory;

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

        #endregion Private Members

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
        ///

        private bool isFileOpen;

        public bool IsFileOpen
        {
            get => isFileOpen;
            private set => this.RaiseAndSetIfChanged(ref isFileOpen, value);
        }

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
            this.IsLoading = true;
            this.IsFileOpen = false;
            var timer = new Stopwatch();
            timer.Start();

            this.logger.Info($"Loading STEP file: { filename }");

            var step = new STEP3DFile(filename);

            if (step.HasFailed)
            {
                this.IsLoading = false;

                // In case of error the current Step3DFile is not updated (keep previous)
                this.logger.Error($"Error loading STEP file: { step.ErrorMessage }");

                throw new InvalidOperationException($"Error loading STEP file: { step.ErrorMessage }");
            }

            timer.Stop();
            this.logger.Info($"STEP file loaded on { timer.ElapsedMilliseconds } ms");

            // Clean pending mapping information
            this.ResetExternalMappingIdentifier();
            this.CleanCurrentMapping();

            // Update the new instance only when a load success
            this.Step3DFile = step;
            this.IsFileOpen = !step.HasFailed;
            this.IsLoading = false;
        }

        /// <summary>
        /// Loads a STEP-AP242 file asynchronously.
        /// </summary>
        /// <param name="filename">Full path to a STEP-AP242 file</param>
        public async Task LoadAsync(string filename)
        {
            await Task.Run(() => Load(filename));
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
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of all mapped parameter and the associate <see cref="Step3DRowViewModel.ID"/>
        /// </summary>
        public Dictionary<ParameterOrOverrideBase, MappedParameterValue> ParameterNodeIds { get; } = new Dictionary<ParameterOrOverrideBase, MappedParameterValue>();

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap ExternalIdentifierMap { get; private set; }

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
        /// Contains the list of ElementDeinfinition or ElementUsage that will be transfere
        /// </summary>
        public ReactiveList<ElementBase> SelectedThingsToTransfer { get;  set; } = new ReactiveList<ElementBase>();
        /// <summary>
        ///  Dictionnary that is used to track a step entity to its mapped entities.
        ///  Used internally to be able to change the state of the HLR parts
        /// </summary>
        private readonly Dictionary<string, List<ElementBase>> ParamToElements = new();

        /// <summary>
        /// Adds mapping configurations used to detect the not used ones in the mapping process
        /// </summary>
        /// <param name="correspondences">The <see cref="IEnumerable{IdCorrespondence}"/> from which store the current mapping</param>
        public void AddPreviousIdCorrespondances(IEnumerable<IdCorrespondence> correspondences)
        {
            this.PreviousIdCorrespondences.Clear();
            PreviousIdCorrespondences.AddRange(correspondences);
        }

        /// <summary>
        /// Remove current mapping information
        /// </summary>
        public void CleanCurrentMapping()
        {
            if (this.MapResult.Count == 0)
            {
                return;
            }

            this.MapResult.Clear();
            this.SelectedThingsToTransfer.Clear();
            this.ParameterNodeIds.Clear();

            // Mapping status is reset to default
            CDPMessageBus.Current.SendMessage(new UpdateHighLevelRepresentationTreeEvent(true));

            // Current NetChange preview must be cleaned (Impact and Object Browser)
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
        }

        /// <summary>
        /// Updates mapped parts to <see cref="Step3DRowViewModel.MappingStatusType.Transfered"/>
        /// </summary>
        private void SetMappedToTransferStatus()
        {
            foreach (var item in this.ParameterNodeIds)
            {
                // Unfortunately, it looks likes both sets of data do not share actual data that can be used to link them after creation.
                // This is a solution that permits to do it wihthout deep changes to the data structures.
                
                int idx = item.Key.UserFriendlyName.IndexOf(".step");
                if (idx < 1)
                {
                    break;
                }
                var key= item.Key.UserFriendlyName.Substring(0,idx);

                var transfered = (from el in SelectedThingsToTransfer where el.UserFriendlyName == key select el).Any();

                if (transfered)
                {
                    this.logger.Debug($"Changing status of  {item.Value.Part.Name} to Transfered");
                    item.Value.Part.SetTransferedStatus();
                }
            }
        }

        /// <summary>
        /// Remove current mapping information preserving the status of transfered mappings
        /// </summary>
        private void CleanCurrentMappingOnTransfer()
        {
            this.MapResult.Clear();
            this.SelectedThingsToTransfer.Clear();
            this.ParameterNodeIds.Clear();
            ParamToElements.Clear();
            // Current NetChange preview must be cleaned (Impact and Object Browser)
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
        }

        /// <summary>
        /// Remove existing <see cref="ExternalIdentifierMap"/> and <see cref="IdCorrespondences"/> data
        /// </summary>
        public void ResetExternalMappingIdentifier()
        {
            this.ExternalIdentifierMap = null;
            this.IdCorrespondences.Clear();
            this.UsedIdCorrespondences.Clear();
            this.PreviousIdCorrespondences.Clear();
        }

        /// <summary>
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="part">The <see cref="Step3DRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        public void Map(Step3DRowViewModel part)
        {
            var parts = new List<Step3DRowViewModel> { part };

            this.AddPreviousIdCorrespondances(part.MappingConfigurations);

            if (this.mappingEngine.Map(parts) is (Dictionary<ParameterOrOverrideBase, MappedParameterValue> parameterMappingInfo, List<ElementBase> elements) && elements.Any())
            {
                foreach (var e in elements)
                {
                    this.logger.Debug($"Adding Map ElementBase {e.Name}");
                }

                this.UpdateParmeterNodeId(parameterMappingInfo, elements);
                this.MapResult.AddRange(elements);
                
                this.MergeExternalIdentifierMap();
                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
            }
        }

        /// <summary>
        /// Updates <see cref="ParameterNodeIds"/> by adding or replacing values
        /// </summary>
        ///
        private void UpdateParmeterNodeId(Dictionary<ParameterOrOverrideBase, MappedParameterValue> parameterMappingInfo, List<ElementBase> elements)
        {
            foreach (var entry in parameterMappingInfo)
            {
                ParamToElements[entry.Key.UserFriendlyName] = elements;
                if (this.ParameterNodeIds.ContainsKey(entry.Key))
                    this.logger.Debug($"Updating ParameterNodeIds[{entry.Key.ModelCode()}] = {entry.Value.Part.Description} ...");
                else
                    this.logger.Debug($"Adding ParameterNodeIds[{entry.Key.ModelCode()}] = {entry.Value.Part.Description} ...");

                this.ParameterNodeIds[entry.Key] = entry.Value;
            }
        }

        /// <summary>
        /// Time (milliseconds) consumed by the last succesull transfer
        /// </summary>
        public long TransferTime { get; private set; } = 0;

        /// <summary>
        /// Transfers the mapped parts to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Transfer()
        {
            this.TransferTime = 0;

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
        [ExcludeFromCodeCoverage]
        public void ShowCorrespondences(IEnumerable<IdCorrespondence> correspondences)
        {
            foreach (var c in correspondences)
            {
                this.ShowCorrespondence(c);
            }
        }

        /// <summary>
        /// Helper method for debugging in console the content of <see cref="IdCorrespondence"/>
        /// </summary>
        /// <param name="correspondences">The <see cref="IdCorrespondence"/> of correspondances</param>
        [ExcludeFromCodeCoverage]
        public void ShowCorrespondence(IdCorrespondence correspondence)
        {
            string thingType = "UNKNOWN";

            if (this.hubController.GetThingById(correspondence.InternalThing, this.hubController.OpenIteration, out Thing thing))
            {
                thingType = $"{thing}";
            }

            this.logger.Debug($"  CorrespondanceId = {correspondence.Iid}");
            this.logger.Debug($"  ExternalId       = {correspondence.ExternalId}");
            this.logger.Debug($"  InternalThing    = {correspondence.InternalThing} --> {thingType}");
            this.logger.Debug($"  IsCached         = {correspondence.IsCached()}");
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
        public void MergeExternalIdentifierMap()
        {
            if (this.ExternalIdentifierMap is null)
            {
                this.IdCorrespondences.Clear();
                this.UsedIdCorrespondences.Clear();
                this.PreviousIdCorrespondences.Clear();

                return;
            }

            var previousCorrespondances = this.PreviousIdCorrespondences.Distinct();
            var unusedCorrespondances = previousCorrespondances.Where(x => !this.UsedIdCorrespondences.Contains(x));

#if DEBUG_EXTERNAL_IDENTITIER_MAP
            this.logger.Debug("UpdateExternalIdentifierMap IdCorrespondances");
            this.ShowCorrespondences(this.IdCorrespondences);

            this.logger.Debug("UpdateExternalIdentifierMap UsedIdCorrespondances");
            this.ShowCorrespondences(this.UsedIdCorrespondences);

            this.logger.Debug("UpdateExternalIdentifierMap previousCorrespondances");
            this.ShowCorrespondences(previousCorrespondances);

            this.logger.Debug("UpdateExternalIdentifierMap unusedCorrespondances");
            this.ShowCorrespondences(unusedCorrespondances);
#endif

            foreach (var unused in unusedCorrespondances)
            {
#if DEBUG_EXTERNAL_IDENTITIER_MAP
                this.logger.Debug($"Removing unusedCorrespondance {unused}");
#endif
                this.ExternalIdentifierMap.Correspondence.Remove(unused);
            }

            foreach (var item in this.IdCorrespondences)
            {
                var clonedCorrespondance = item.Clone(false);
                this.ExternalIdentifierMap.Correspondence.Add(clonedCorrespondance);

#if DEBUG_EXTERNAL_IDENTITIER_MAP
                this.logger.Debug($"Adding clonedCorrespondance {clonedCorrespondance}");
                this.ShowCorrespondence(clonedCorrespondance);
#endif
            }

            this.IdCorrespondences.Clear();
            this.UsedIdCorrespondences.Clear();
            this.PreviousIdCorrespondences.Clear();
        }

        /// <summary>
        /// Updates the mappings of the current <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="externalIdentifierMap"></param>
        public void SetExternalIdentifierMap(ExternalIdentifierMap externalIdentifierMap)
        {
            this.ExternalIdentifierMap = externalIdentifierMap;
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
            if (this.ExternalIdentifierMap is null)
            {
                return;
            }

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

        #endregion IDstController interface

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="mappingEngine">The <<see cref="IMappingEngine"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="exchangeHistory">The <see cref="IExchangeHistoryService"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine,
            INavigationService navigationService, IExchangeHistoryService exchangeHistory,
            IDstHubService dstHubService, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
            this.navigationService = navigationService;
            this.exchangeHistory = exchangeHistory;
            this.dstHubService = dstHubService;
            this.statusBar = statusBar;
        }

        #endregion Constructor

        #region Private Transfer Methods

        /// <summary>
        /// Send message to the status bar
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="messageSeverity">The status severity</param>
        [ExcludeFromCodeCoverage]
        private void SendStatusMessage(string message, StatusBarMessageSeverity messageSeverity = StatusBarMessageSeverity.Info)
        {
            if (Application.ResourceAssembly == null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() => this.statusBar.Append(message, messageSeverity));
        }

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
                this.logger.Info($"Begin Transfer of {this.MapResult.Count} elements...");
                this.logger.Debug($"MapResult distinct elements = {this.MapResult.Distinct()}");

                var timer = new Stopwatch();
                timer.Start();

                var iterationClone = this.hubController.OpenIteration.Clone(false);
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone);

                if (!(this.SelectedThingsToTransfer.Any() && this.TrySupplyingAndCreatingLogEntry(transaction)))
                {
                    return;
                }

                // Step 1: upload file
                string filePath = Step3DFile.FileName;
                var file = this.dstHubService.FindFile(filePath);

                this.SendStatusMessage($"Uploading STEP file to Hub: {filePath}");
                await this.hubController.Upload(filePath, file);

                // Step 2: update Step3dParameter.source with FileRevision from uploaded file
                file = this.dstHubService.FindFile(filePath);
                var fileRevision = file.CurrentFileRevision;

                this.SendStatusMessage($"Updating STEP file references Guid to: {fileRevision.Iid}");

                foreach (var item in this.ParameterNodeIds)
                {
                    item.Value.UpdateSource(fileRevision);
                }

                // Step 3: create/update things
                this.SendStatusMessage($"Processing {this.MapResult.Count} mapping data...");

                foreach (var elementBase in this.SelectedThingsToTransfer)
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

                this.SendStatusMessage($"Transfering changes...");
                await this.hubController.Write(transaction);

                // Update ValueSet after the commit.
                // The ValueArray are constructed with the correct size
                // in the last HubController.Write(transaction) call.

                this.SendStatusMessage($"Transfering ValueSets...");
                await this.UpdateParametersValueSets();

                timer.Stop();
                this.TransferTime = timer.ElapsedMilliseconds;

                this.SetMappedToTransferStatus();

                this.SendStatusMessage($"Transfers to Hub done, updating data source...");
                await this.hubController.Refresh();

                // Update ExternalIdentifierMap with current information from Hub
                if (this.ExternalIdentifierMap is { })
                {
                    this.hubController.GetThingById(this.ExternalIdentifierMap.Iid, this.hubController.OpenIteration, out ExternalIdentifierMap map);
                    this.ExternalIdentifierMap = map.Clone(true);
                }

                this.CleanCurrentMappingOnTransfer();

                this.logger.Info($"Transfer finished");
            }
            catch (Exception exception)
            {
                this.logger.Error(exception);
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
                this.exchangeHistory.Append(clone, ChangeKind.Create);
            }
            else
            {
                transaction.CreateOrUpdate(clone);
                this.exchangeHistory.Append(clone, ChangeKind.Update);
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

            this.UpdateParametersValueSets(transaction, this.SelectedThingsToTransfer.OfType<ElementDefinition>().SelectMany(e => e.Parameter));
            this.UpdateParametersValueSets(transaction, this.SelectedThingsToTransfer.OfType<ElementUsage>().SelectMany(eu => eu.ParameterOverride));

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
                    this.UpdateValueSet(clone, parameter.ValueSet[index]);
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
        private void UpdateValueSet(ParameterValueSetBase clone, IValueSet valueSet)
        {
            this.exchangeHistory.Append(clone, valueSet);

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
            if (this.ExternalIdentifierMap is null)
            {
                return;
            }

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

        #endregion Private Transfer Methods
    }
}