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
    using System.Diagnostics;
    using System.Threading.Tasks;
    
    using ReactiveUI;

    using NLog;

    using STEP3DAdapter;
    using System;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

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

        #endregion
    }
}
