// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
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

namespace DEHPSTEPAP242.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.MetaInfo;
    using CDP4Common.Types;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;

    using ReactiveUI;
    using NLog;

    /// <summary>
    /// View Model for showing mapped things in the main window
    /// </summary>
    public class MappingViewModel : ReactiveObject, IMappingViewModel
    {
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstObjectBrowserViewModel"/>
        /// </summary>
        private readonly IDstObjectBrowserViewModel dstStep3DControlViewModel;

        /// <summary>
        /// Gets or sets the collection of <see cref="MappingRows"/>
        /// </summary>
        public ReactiveList<MappingRowViewModel> MappingRows { get; set; } = new ReactiveList<MappingRowViewModel>();

        /// <summary>
        /// Initializes a new <see cref="MappingViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubController">The <see cref="IHubController"/>"/></param>
        /// <param name="dstVariablesControlViewModel">The <see cref="IDstObjectBrowserViewModel"/></param>
        public MappingViewModel(IDstController dstController, IHubController hubController,
            IDstObjectBrowserViewModel dstVariablesControlViewModel)
        {
            this.dstController = dstController;
            this.hubController = hubController;
            this.dstStep3DControlViewModel = dstVariablesControlViewModel;

            this.InitializeObservables();
        }

        /// <summary>
        /// Initializes the observables
        /// </summary>
        private void InitializeObservables()
        {
            this.dstController.MapResult.ItemsAdded.ObserveOn(RxApp.MainThreadScheduler).Subscribe(this.UpdateMappedThings);

            this.dstController.MapResult.IsEmptyChanged.ObserveOn(RxApp.MainThreadScheduler).Where(x => x)
                .Subscribe(_ => this.MappingRows.RemoveAll(
                    this.MappingRows.Where(x => x.Direction == MappingDirection.FromDstToHub).ToList()));

            this.WhenAnyValue(x => x.dstController.MappingDirection)
                .Subscribe(this.UpdateMappingRowsDirection);
        }

        /// <summary>
        /// Updates the row according to the new <see cref="IDstController.MappingDirection"/>
        /// </summary>
        /// <param name="mappingDirection"></param>
        public void UpdateMappingRowsDirection(MappingDirection mappingDirection)
        {
            foreach (var mappingRowViewModel in this.MappingRows)
            {
                mappingRowViewModel.UpdateDirection(mappingDirection);
            }
        }

        /// <summary>
        /// Updates the <see cref="MappingRows"/>
        /// </summary>
        /// <param name="element">The <see cref="ElementBase"/></param>
        private void UpdateMappedThings(ElementBase element)
        {
            this.logger.Debug($"Updating Mapped View for ElementBase {element.Name}");

            var parametersMappingInfo = element switch
            {
                ElementDefinition elementDefinition => this.GetParameters(elementDefinition),
                ElementUsage elementUsage => this.GetParameters(elementUsage),
                _ => new List<(ParameterOrOverrideBase parameter, MappedParameterValue info)>()
            };

            foreach (var parameter in parametersMappingInfo)
            {
                this.MappingRows.Add(new MappingRowViewModel(this.dstController.MappingDirection,
                    parameter.parameter, parameter.info.Part));
                //this.MappingRows.Add(new MappingRowViewModel(this.dstController.MappingDirection, parameter.parameter,
                //    this.dstStep3DControlViewModel.Step3DHLR.FirstOrDefault(x => x.ID == parameter.info.Part.ID)));
            }
        }

        /// <summary>
        /// Queries the parameters in the <see cref="IDstController.ParameterNodeIds"/> with their associated <see cref="MappedParameterValue"/> and a collection of their original references
        /// </summary>
        /// <param name="element">The <see cref="ElementDefinition"/></param>
        /// <returns>A List{(ParameterOrOverrideBase parameter, MappedParameterValue nodeId)}</returns>
        private List<(ParameterOrOverrideBase parameter, MappedParameterValue info)> GetParameters(ElementDefinition element)
        {
            var result = new List<(ParameterOrOverrideBase, MappedParameterValue)>();

            var modified = this.dstController.ParameterNodeIds.Where(
                x => x.Key.GetContainerOfType<ElementDefinition>().Iid == element.Iid).ToList();

            var originals = this.hubController.OpenIteration.Element
                .FirstOrDefault(x => x.Iid == element.Iid)?
                .Parameter.Where(x => modified
                    .Select(o => o.Key)
                    .Any(p => p.Iid == x.Iid)) ?? modified.Select(x => x.Key);

            foreach (var parameterOverride in originals)
            {
                result.Add((parameterOverride, modified.FirstOrDefault(p => p.Key.Iid == parameterOverride.Iid).Value));
            }

            return result;
        }

        /// <summary>
        /// Queries the parameters in the <see cref="IDstController.ParameterNodeIds"/> with their associated <see cref="MappedParameterValue"/> and a collection of their original references
        /// </summary>
        /// <param name="element">The <see cref="ElementUsage"/></param>
        /// <returns>A List{(ParameterOrOverrideBase parameter, MappedParameterValue nodeId)}</returns>
        private List<(ParameterOrOverrideBase parameter, MappedParameterValue info)> GetParameters(ElementUsage element)
        {
            var result = new List<(ParameterOrOverrideBase, MappedParameterValue)>();

            var modified = this.dstController.ParameterNodeIds.Where(
                x => x.Key.GetContainerOfType<ElementUsage>().Iid == element.Iid).ToList();

            var originals = this.hubController.OpenIteration.Element
                .FirstOrDefault(x => x.Iid == element.ElementDefinition.Iid)?
                .ReferencingElementUsages().FirstOrDefault(x => x.Iid == element.Iid)?.ParameterOverride
                .Where(p => modified.Any(x => p.Iid == x.Key.Iid)) ?? new List<ParameterOverride>();

            foreach (var parameterOverride in originals)
            {
                result.Add((parameterOverride, modified.FirstOrDefault(p => p.Key.Iid == parameterOverride.Iid).Value));
            }

            return result;
        }
    }
}
