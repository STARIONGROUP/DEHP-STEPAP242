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

namespace DEHPSTEPAP242.Services.DstHubService
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    public interface IDstHubService
    {
        /// <summary>
        /// Checks that all DST required data are in the Hub.
        /// 
        /// Creates any missing data:
        /// - FileTypes
        /// - ParameterTypes
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        Task CheckHubDependencies();

        /// <summary>
        /// First compatible DST <see cref="FileType"/> of a <see cref="FileRevision"/>
        /// </summary>
        /// <param name="fileRevision">The <see cref="FileRevision"/></param>
        /// <returns>First compatible FileType or null if not found</returns>
        FileType FirstSTEPFileType(FileRevision fileRevision);

        /// <summary>
        /// Finds all the revisions for DST files
        /// </summary>
        /// <returns></returns>
        List<FileRevision> GetFileRevisions();

        /// <summary>
        /// Checks if it is a STEP file type
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if is a STEP file</returns>
        bool IsSTEPFileType(FileRevision fileRevision);

        /// <summary>
        /// Checks if a parameter is compatible with STEP 3D mapping
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>True if it is a candidate for the mapping</returns>
        bool IsSTEPParameterType(ParameterType param);

        /// <summary>
        /// Gets the step geometric parameter where to store a STEP-AP242 part information
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        public ParameterType FindSTEPParameterType();

        /// <summary>
        /// Gets the <see cref="ParameterTypeComponent"/> corresponding to the source file reference
        /// </summary>
        /// <param name="param">The <see cref="ParameterType"/> to check</param>
        /// <returns>A <see cref="ParameterTypeComponent"/> or null if does not contain the component</returns>
        public ParameterTypeComponent FindSourceParameterType(ParameterType param);

        /// <summary>
        /// Gets the <see cref="ReferenceDataLibrary"/> where to add DST content
        /// </summary>
        /// <returns>A <see cref="ReferenceDataLibrary"/></returns>
        ReferenceDataLibrary GetReferenceDataLibrary();

        /// <summary>
        /// Finds the DST <see cref="CDP4Common.EngineeringModelData.File"/> in the Hub
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.File"/> or null if does not exist</returns>
        File FindFile(string filePath);

        /// <summary>
        /// Finds the <see cref="CDP4Common.EngineeringModelData.FileRevision"/> from string <see cref="System.Guid"/>
        /// </summary>
        /// <param name="guid">The string value of an <see cref="System.Guid"/></param>
        /// <returns>The <see cref="CDP4Common.EngineeringModelData.FileRevision"/> or null if does not exist</returns>
        FileRevision FindFileRevision(string guid);
    }
}
