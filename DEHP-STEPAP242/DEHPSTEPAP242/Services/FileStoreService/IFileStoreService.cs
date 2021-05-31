// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileStoreService.cs" company="Open Engineering S.A.">
//    Copyright (c) 2021 Open Engineering S.A.
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

namespace DEHPSTEPAP242.Services.FileStoreService
{
    using CDP4Common.EngineeringModelData;

    /// <summary>
    /// Service to store and cache files downloaded from the <see cref="DomainFileStore"/>.
    /// </summary>
    public interface IFileStoreService
    {
        /// <summary>
        /// Initializes the directory where files from the Hub are stored
        /// </summary>
        void InitializeStorage();

        /// <summary>
        /// Cleans all previous downloaded files
        /// </summary>
        void Clean();

        /// <summary>
        /// Adds a file content for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <param name="fileContent"></param>
        void Add(FileRevision fileRevision, byte[] fileContent);

        /// <summary>
        /// Adds a new writable file stream for a specific revision
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>A <see cref="System.IO.FileStream"/> where perform the write operations</returns>
        System.IO.FileStream AddFileStream(FileRevision fileRevision);

        /// <summary>
        /// Checks if a file for this revision already in the cache area.
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>True if a file exists</returns>
        bool Exists(FileRevision fileRevision);

        /// <summary>
        /// Gets the corresponding file name in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>Returns file name and extension</returns>
        string GetName(FileRevision fileRevision);

        /// <summary>
        /// Gets the corresponding file path in the <see cref="StorageDirectoryPath"/>
        /// </summary>
        /// <param name="fileRevision"></param>
        /// <returns>Returns full path to file name</returns>
        string GetPath(FileRevision fileRevision);
    }
}
