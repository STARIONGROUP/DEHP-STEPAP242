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

    using STEP3DAdapter;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : IDstController
    {
        /// <summary>
        /// The <see cref="STEP3DFile"/> that handles the interaction with a STEP-AP242 file/
        /// </summary>
        private STEP3DFile step3dFile;

		public STEP3DFile Step3DFile { get => step3dFile; }

		public bool IsFileOpen => step3dFile?.HasFailed == false;

        /// <summary>
        /// Load a STEP-AP242 file.
        /// <param name="filename"></param>
		public void Load(string filename)
		{
            //Logger.Error($"Loading file: {filename}");

            step3dFile = new STEP3DFile(filename);

            if (step3dFile.HasFailed)
            {
                Debug.WriteLine($"Error message: { step3dFile.ErrorMessage }");
                return;
            }
        }

        /// <summary>
        /// This should be returns <see cref="Task"/>.
        /// 
        /// public async Task XXX()
        /// {
        ///     await method();
        /// }
        /// </summary>
        /// <param name="filename"></param>
		public Task LoadFile(string filename)
		{
			throw new System.NotImplementedException();
		}
	}
}
