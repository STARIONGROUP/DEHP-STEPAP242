// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.ViewModel.Rows
{
    using System.Linq;

    using ReactiveUI;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.Enumerators;
    using System;
    using DEHPSTEPAP242.DstController;

    /// <summary>
    /// Represents a row of mapped <see cref="ParameterOrOverrideBase"/> and <see cref="Step3DRowViewModel"/>
    /// </summary>
    /// <remarks>
    /// The DEHP STEP-AP242 adapter does not have a Hub to STEP mapping capability.
    /// </remarks>
    public class MappingRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Gets or sets the hub <see cref="MappedThing"/>
        /// </summary>
        public MappedThing HubThing { get; set; }
        
        /// <summary>
        /// Gets or sets the dst <see cref="MappedThing"/>
        /// </summary>
        public MappedThing DstThing { get; set; }

        /// <summary>
        /// Backing field for <see cref="direction"/>
        /// </summary>
        private MappingDirection direction;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public MappingDirection Direction
        {
            get => this.direction;
            set => this.RaiseAndSetIfChanged(ref this.direction, value);
        }

        /// <summary>
        /// Backing field for <see cref="ArrowDirection"/>
        /// </summary>
        private double arrowDirection;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public double ArrowDirection
        {
            get => this.arrowDirection;
            set => this.RaiseAndSetIfChanged(ref this.arrowDirection, value);
        }

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel"/> from a mapped <see cref="ParameterOrOverrideBase"/> and <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <param name="currentMappingDirection">The current <see cref="MappingDirection"/></param>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase"/></param>
        /// <param name="part">The <see cref="VariableRowViewModel"/></param>
        public MappingRowViewModel(MappingDirection currentMappingDirection, ParameterBase parameter, Step3DRowViewModel part)
        {
            this.Direction = MappingDirection.FromDstToHub;
            
            this.DstThing = new MappedThing() 
            {
                Name = part.InstancePath, 
                Value = string.IsNullOrEmpty(part.RelationLabel) ? $"STEP Entity {part.Description}" : $"STEP entity {part.Description} used at Relation {part.RelationLabel}"
                //Value = $"STEP entity {part.Description}"
            };

            string value;

            var valueSet = parameter.QueryParameterBaseValueSet(part.SelectedOption, part.SelectedActualFiniteState);
            
            if (valueSet.Computed[0] is { })
            {
                var computed = valueSet.Computed;
                
                value = $"[{string.Join(", ", computed.Where(x => !string.IsNullOrEmpty(x)))}]";
            }
            else
            {
                value = "-";
            }

            this.HubThing = new MappedThing()
            {
                Name = parameter.ModelCode(),
                Value = value
            };

            this.UpdateDirection(currentMappingDirection);
        }

        public MappingRowViewModel(MappingDirection currentMappingDirection, ParameterBase parameter, MappedParameterValue mappInfo)
        {
            this.Direction = MappingDirection.FromDstToHub;

            var part = mappInfo.Part;
            var fields = mappInfo.Fields;

            this.DstThing = new MappedThing()
            {
#if USE_DST_THING_NAME_DESCRIPTION
                // Use NAME Description --> inform the relation where it is used
                Name = part.Description,
                Value = string.IsNullOrEmpty(part.RelationLabel) ? $"STEP Entity {part.Description}" : $"STEP entity {part.Description} used at Relation {part.RelationLabel}"
#else
                // Use NAME InstancePath
                Name = part.InstancePath,
                Value = $"STEP entity {part.Description}"
#endif
            };

            // Step 3D geometry is a vector, fill with non-empty values
            var valueSet = fields;
            var value = $"[{string.Join(", ", valueSet.Where(x => !string.IsNullOrEmpty(x)))}]";

            // Inform extra configuration parameters
            if (part.SelectedOption is { } option)
            {
                value += $" Option: {option.Name}";
            }

            if (part.SelectedActualFiniteState is { } state)
            {
                value += $" State: {state.Name}";
            }

            this.HubThing = new MappedThing()
            {
                Name = parameter.ModelCode(),
                Value = value
            };

            this.UpdateDirection(currentMappingDirection);
        }

        /// <summary>
        /// Updates the arrow angle factor <see cref="ArrowDirection"/>, the <see cref="HubThing"/>
        /// and the <see cref="DstThing"/> <see cref="MappedThing.GridColumnIndex"/>
        /// </summary>
        /// <param name="actualMappingDirection">The actual <see cref="MappingDirection"/></param>
        public void UpdateDirection(MappingDirection actualMappingDirection)
        {
            switch (this.Direction)
            {
                case MappingDirection.FromDstToHub when actualMappingDirection is MappingDirection.FromDstToHub:
                    this.HubThing.GridColumnIndex = 2;
                    this.DstThing.GridColumnIndex = 0;
                    this.ArrowDirection = 0;
                    break;
                case MappingDirection.FromDstToHub when actualMappingDirection is MappingDirection.FromHubToDst:
                    this.HubThing.GridColumnIndex = 0;
                    this.DstThing.GridColumnIndex = 2;
                    this.ArrowDirection = 180;
                    break;
                case MappingDirection.FromHubToDst when actualMappingDirection is MappingDirection.FromHubToDst:
                    this.HubThing.GridColumnIndex = 0;
                    this.DstThing.GridColumnIndex = 2;
                    this.ArrowDirection = 0;
                    break;
                case MappingDirection.FromHubToDst when actualMappingDirection is MappingDirection.FromDstToHub:
                    this.HubThing.GridColumnIndex = 2;
                    this.DstThing.GridColumnIndex = 0;
                    this.ArrowDirection = 180;
                    break;
            }
        }
    }
}
