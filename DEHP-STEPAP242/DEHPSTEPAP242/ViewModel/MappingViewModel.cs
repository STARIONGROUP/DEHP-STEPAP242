// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingViewModel.cs" company="Open Engineering S.A.">
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

namespace DEHPSTEPAP242.ViewModel
{
    using CDP4Common.EngineeringModelData;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPSTEPAP242.DstController;
    using DEHPSTEPAP242.ViewModel.Interfaces;
    using DEHPSTEPAP242.ViewModel.Rows;
    using NLog;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

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
        /// Gets or sets the collection of <see cref="MappingRows"/>
        /// </summary>
        public ReactiveList<MappingRowViewModel> MappingRows { get; set; } = new ReactiveList<MappingRowViewModel>();

        /// <summary>
        /// Initializes a new <see cref="MappingViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubController">The <see cref="IHubController"/>"/></param>
        public MappingViewModel(IDstController dstController, IHubController hubController,
            IDstObjectBrowserViewModel dstVariablesControlViewModel)
        {
            this.dstController = dstController;            

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
                this.logger.Debug($"Adding MappingRowViewModel({parameter.parameter}, {parameter.info.Part.InstancePath}");

                this.MappingRows.Add(new MappingRowViewModel(this.dstController.MappingDirection,
                    parameter.parameter, parameter.info));
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
                    x => x.Key.GetContainerOfType<ElementDefinition>() == element).FirstOrDefault();

            result.Add((modified.Key, modified.Value));

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
                x => x.Key.GetContainerOfType<ElementUsage>() == element).ToList();

            foreach (var entry in modified)
            {
                result.Add((entry.Key, entry.Value));
            }

            return result;
        }
    }
}
