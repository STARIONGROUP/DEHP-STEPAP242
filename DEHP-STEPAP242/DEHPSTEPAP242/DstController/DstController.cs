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

        /// <summary>
        /// Gets this running tool name
        /// </summary>
        public string ThisToolName => this.GetType().Assembly.GetName().Name;

        #region IDstController interface

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
        public ReactiveList<ElementDefinition> MapResult { get; private set; } = new ReactiveList<ElementDefinition>();

        /// <summary>
        /// Gets the colection of mapped <see cref="Step3dTargetSourceParameter"/> which needs to be updated in the transfer operation
        /// </summary>
        public List<Step3dTargetSourceParameter> TargetSourceParametersDstStep3dMaps { get; private set; } = new List<Step3dTargetSourceParameter>();

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
        /// <param name="dst3DPart">The <see cref="Step3dRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        public async Task Map(Step3dRowViewModel dst3DPart)
        {
            Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Mapping in progress of {dst3DPart.Description}..."));

            var parts = new List<Step3dRowViewModel> { dst3DPart };

            try
            {
                var (elements, sources) = ((IEnumerable<ElementDefinition>, IEnumerable<Step3dTargetSourceParameter>))
                   this.mappingEngine.Map(parts);

                this.MapResult.AddRange(elements);
                this.TargetSourceParametersDstStep3dMaps.AddRange(sources);

                await this.UpdateExternalIdentifierMap();

                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Mapping of {dst3DPart.Description} done"));
            }
            catch (Exception exception)
            {
                this.logger.Error(exception);
                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Mapping of {dst3DPart.Description} failed"));
            }

            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
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
        /// <returns>A <see cref="Task"/></returns>
        public async Task UpdateExternalIdentifierMap()
        {
#if DO_UPDATE_EXTERNAL_IDMAP
            await this.hubController.Delete<ExternalIdentifierMap, IdCorrespondence>(this.ExternalIdentifierMap.Correspondence.ToList(),
                (map, correspondence) => map.Correspondence.Remove(correspondence));

            this.ExternalIdentifierMap.Correspondence.Clear();

            foreach (var correspondence in this.IdCorrespondences)
            {
                correspondence.Container = this.ExternalIdentifierMap;
            }

            await this.hubController.CreateOrUpdate<ExternalIdentifierMap, IdCorrespondence>(this.IdCorrespondences,
                (map, correspondence) => map.Correspondence.Add(correspondence));

            this.ExternalIdentifierMap.Correspondence.AddRange(this.IdCorrespondences);
            this.IdCorrespondences.Clear();
            this.statusBar.Append("Mapping configuration saved");
#else
            // Currently nothing to save (feature not fully implemented)
            await Task.FromResult(0);
#endif
        }

        /// <summary>
        /// Creates and sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="ExternalIdentifierMap"/></param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap"/></returns>
        public async Task<ExternalIdentifierMap> CreateExternalIdentifierMap(string newName)
        {
            var externalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Name = newName,
                ExternalToolName = this.ThisToolName,
                ExternalModelName = newName,
                Owner = this.hubController.CurrentDomainOfExpertise,
                Container = this.hubController.OpenIteration
            };

            await this.hubController.CreateOrUpdate<Iteration, ExternalIdentifierMap>(externalIdentifierMap,
                (i, m) => i.ExternalIdentifierMap.Add(m), true);

            return externalIdentifierMap;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="mappingEngine">The <<see cref="IMappingEngine"/></param>
        /// <param name="dstHubService">The <see cref="IDstHubService"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine, 
            IDstHubService dstHubService, IStatusBarControlViewModel statusBar)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
            this.dstHubService = dstHubService;
            this.statusBar = statusBar;
        }

        #endregion

        #region Private Transfer Methods

        /// <summary>
        /// Transfers the input file and mapped parts to the Hub
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task TransferMappedThingsToHub()
        {
            // Step 1: upload file
            string filePath = Step3DFile.FileName;
            var file = this.dstHubService.FindFile(filePath);

            Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Uploading STEP file to Hub: {filePath}"));
            await this.hubController.Upload(filePath, file);

            // Step 2: update Step3dParameter.source with FileRevision from uploaded file
            file = this.dstHubService.FindFile(filePath);
            var fileRevision = file.CurrentFileRevision;

            Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Updating STEP file references Guid to: {fileRevision.Iid}"));
            foreach (var sourceFieldToUpdate in this.TargetSourceParametersDstStep3dMaps)
            {
                sourceFieldToUpdate.UpdateSource(fileRevision);
            }

            // Step 3: create/update things
            try
            {
                var iterationClone = this.hubController.OpenIteration.Clone(false);
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone);

                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfering {this.MapResult.Count} ElementDefinitions..."));

                foreach (var elementDefinition in this.MapResult)
                {
                    var elementDefinitionCloned = this.TransactionCreateOrUpdate(transaction, elementDefinition, iterationClone.Element);

                    foreach (var parameter in elementDefinition.Parameter)
                    {
                        _ = this.TransactionCreateOrUpdate(transaction, parameter, elementDefinitionCloned.Parameter);
                    }

                    foreach (var parameterOverride in elementDefinition.ContainedElement.SelectMany(x => x.ParameterOverride))
                    {
                        var elementUsageClone = (ElementUsage)parameterOverride.Container.Clone(false);
                        transaction.CreateOrUpdate(elementUsageClone);

                        _ = this.TransactionCreateOrUpdate(transaction, parameterOverride, elementUsageClone.ParameterOverride);
                    }
                }

                transaction.CreateOrUpdate(iterationClone);

                await this.hubController.Write(transaction);

                // Update ValueSet after the commit.
                // The ValueArray are constructed with the correct size
                // in the last HubController.Write(transaction) call.

                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfering ValueSets..."));

                await this.UpdateParametersValueSets();

                await this.UpdateExternalIdentifierMap();

                Application.Current.Dispatcher.Invoke(() => this.statusBar.Append($"Transfer to Hub done"));

                await this.hubController.Refresh();
                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
            }
            catch (Exception e)
            {
                this.logger.Error(e);
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
        private async Task UpdateParametersValueSets(Thing clonedContainer)
        {
            await this.UpdateParametersValueSets(this.MapResult.SelectMany(x => x.Parameter));
            await this.UpdateParametersValueSets(this.MapResult.SelectMany(x => x.ContainedElement.SelectMany(p => p.ParameterOverride)));
        }

        /// <summary>
        /// Updates the <see cref="IValueSet"/> of all <see cref="Parameter"/> and all <see cref="ParameterOverride"/>
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task UpdateParametersValueSets()
        {
            await this.UpdateParametersValueSets(this.MapResult.SelectMany(x => x.Parameter));
            await this.UpdateParametersValueSets(this.MapResult.SelectMany(x => x.ContainedElement.SelectMany(p => p.ParameterOverride)));
        }

        /// <summary>
        /// Updates the specified <see cref="Parameter"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="parameters">The collection of <see cref="Parameter"/></param>
        private async Task UpdateParametersValueSets(IEnumerable<Parameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out Parameter newParameter);
                var container = newParameter.Clone(false);

                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);

                await this.hubController.Write(transaction);
            }
        }

        /// <summary>
        /// Updates the specified <see cref="ParameterOverride"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="parameters">The collection of <see cref="ParameterOverride"/></param>
        private async Task UpdateParametersValueSets(IEnumerable<ParameterOverride> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out ParameterOverride newParameter);
                var container = newParameter.Clone(false);

                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);

                await this.hubController.Write(transaction);
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
        }

#if OLDMETHODS
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

        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<ParameterOverride> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out ParameterOverride newParameter);

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

        private static void UpdateValueSet(ParameterValueSetBase clone, IValueSet valueSet)
        {
            clone.Reference = valueSet.Reference;
            clone.Computed = valueSet.Computed;
            clone.Manual = valueSet.Manual;
            clone.ActualState = valueSet.ActualState;
            clone.ActualOption = valueSet.ActualOption;
            clone.Formula = valueSet.Formula;
            clone.ValueSwitch = valueSet.ValueSwitch;
        }
#endif
        #endregion
    }
}
