// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstController.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CDP4Common.EngineeringModelData;
    using DEHPCommon.Enumerators;
    using DEHPSTEPAP242.ViewModel.Rows;

    using STEP3DAdapter;

    /// <summary>
    /// Interface definition for <see cref="DstController"/>
    /// </summary>
    public interface IDstController
    {
        /// <summary>
        /// Returns the status of the last load action.
        /// </summary>
        bool IsFileOpen { get; }

        /// <summary>
        /// Gets or sets the status flag for the load action.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Gets the <see cref="STEP3DFile"/> instance.
        /// </summary>
        STEP3DFile Step3DFile { get; }

        /// <summary>
        /// Loads a STEP-AP242 file.
        /// </summary>
        /// <param name="filename">Full path to file</param>
        void Load(string filename);

        /// <summary>
        /// Loads a STEP-AP242 file asynchronusly.
        /// </summary>
        /// <param name="filename">Full path to file</param>
        Task LoadAsync(string filename);

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/>
        /// </summary>
        MappingDirection MappingDirection { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="ExternalIdentifierMap"/>s
        /// </summary>
        IEnumerable<ExternalIdentifierMap> AvailablExternalIdentifierMap { get; }

        /// <summary>
        /// Gets the colection of mapped <see cref="ElementDefinition"/>s and <see cref="Parameter"/>s
        /// </summary>
        IEnumerable<ElementDefinition> DstMapResult { get; }

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        ExternalIdentifierMap ExternalIdentifierMap { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="IdCorrespondences"/>
        /// </summary>
        List<IdCorrespondence> IdCorrespondences { get; }

        /// <summary>
        /// Creates and sets the <see cref="DstController.ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="DstController.ExternalIdentifierMap"/></param>
        /// <returns>A awaitable <see cref="ExternalIdentifierMap"/></returns>
        Task<ExternalIdentifierMap> CreateExternalIdentifierMap(string newName);

        /// <summary>
        /// Map the provided object using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dst3DPart">The <see cref="Step3dRowViewModel"/> data</param>
        /// <returns>A awaitable assert whether the mapping was successful</returns>
        bool Map(Step3dRowViewModel dst3DPart);

        /// <summary>
        /// Transfers the mapped parts to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        Task Transfer();
    }
}
