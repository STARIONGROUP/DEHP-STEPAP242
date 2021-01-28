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
    using System.Threading.Tasks;
    
    using ReactiveUI;

    using NLog;

    using CDP4Common.EngineeringModelData;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;

    using DEHPSTEPAP242.ViewModel;
    using STEP3DAdapter;
    using DEHPSTEPAP242.ViewModel.Rows;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        #region Private Members

        /// <summary>
        /// The <see cref="IMappingEngine"/>
        /// </summary>
        private readonly IMappingEngine mappingEngine;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

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
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dst3DPart">The <see cref="Step3dRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        public bool Map(Step3dRowViewModel dst3DPart)
        {
            var (elements, maps) = ((IEnumerable<ElementDefinition>, IEnumerable<ExternalIdentifierMap>))
                this.mappingEngine.Map(dst3DPart);

            this.ElementDefinitionParametersDstVariablesMaps = elements;
            this.ExternalIdentifierMaps = maps;
            return true;
        }

        #endregion

        /// <summary>
        /// Gets the collection of <see cref="ExternalIdentifierMap"/>s
        /// </summary>
        public IEnumerable<ExternalIdentifierMap> ExternalIdentifierMaps { get; private set; } = new List<ExternalIdentifierMap>();

        /// <summary>
        /// Gets the colection of mapped <see cref="ElementDefinition"/>s and <see cref="Parameter"/>s
        /// </summary>
        public IEnumerable<ElementDefinition> ElementDefinitionParametersDstVariablesMaps { get; private set; } = new List<ElementDefinition>();

        #region Constructor

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="mappingEngine">The <<see cref="IMappingEngine"/></param>
        public DstController(IHubController hubController, IMappingEngine mappingEngine)
        {
            this.hubController = hubController;
            this.mappingEngine = mappingEngine;
        }

        #endregion

        // /// <summary>
        // /// Transfers the mapped variables to the Hub data source
        // /// </summary>
        // /// <returns>A <see cref="Task"/></returns>
        // public async Task Transfer()
        // {
        //     await this.hubController.CreateOrUpdate(this.ElementDefinitionParametersDstVariablesMaps, true);
        //     await this.hubController.CreateOrUpdate(this.ExternalIdentifierMaps, false);
        // }


    }
}
