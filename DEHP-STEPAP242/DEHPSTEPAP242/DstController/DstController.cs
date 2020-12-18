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

    using DEHPSTEPAP242.Enumerator;
    using DEHPSTEPAP242.Services.OpcConnector.Interfaces;

    using Opc.Ua;

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

		public bool IsFileOpen => this.step3dFile?.HasFailed == false;

        /// <summary>
        /// The <see cref="IOpcClientService"/> that handles the OPC connection with EcosimPro
        /// </summary>
        private readonly IOpcClientService opcClientService;
        
        /// <summary>
        /// Assert whether the <see cref="Services.OpcConnector.OpcSessionHandler.Session"/> is Open
        /// </summary>
        public bool IsSessionOpen =>  this.opcClientService.OpcClientStatusCode == OpcClientStatusCode.Connected;

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="opcClientService">The <see cref="IOpcClientService"/></param>
        public DstController(IOpcClientService opcClientService)
        {
            this.opcClientService = opcClientService;
        }

        /// <summary>
        /// Connects to the provided endpoint
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null)
        {
            await this.opcClientService.Connect(endpoint, autoAcceptConnection, credential);
        }
        
        /// <summary>
        /// Closes the <see cref="Services.OpcConnector.OpcSessionHandler.Session"/>
        /// </summary>
        public void CloseSession()
        {
            this.opcClientService.CloseSession();
        }

		public void Load(string filename)
		{
            //Logger.Error($"Loading file: {filename}");

            this.step3dFile = new STEP3DFile(filename);

            if (this.step3dFile.HasFailed)
            {
                Debug.WriteLine($"Error message: { this.step3dFile.ErrorMessage }");
                return;
            }
        }
    }
}
