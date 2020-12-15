// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpcClientStatusCode.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.Enumerator
{
    using DEHPSTEPAP242.Services.OpcConnector;

    /// <summary>
    /// The <see cref="OpcClientStatusCode"/> represents the status of the <see cref="OpcClientService"/>
    /// </summary>
    public enum OpcClientStatusCode
    {
        /// <summary>
        /// The default <see cref="OpcClientService"/> status
        /// </summary>
        Ready,

        /// <summary>
        /// The Status when a the <see cref="Opc.Ua.Client.Session"/> is open
        /// </summary>
        Connected,

        /// <summary>
        /// Represents a error state creating the OPC application
        /// </summary>
        ErrorCreateApplication,

        /// <summary>
        /// Represents an error state after trying to discover endpoints
        /// </summary>
        ErrorDiscoverEndpoints,

        /// <summary>
        /// Represents an error state trying to create the <see cref="Opc.Ua.Client.Session"/>
        /// </summary>
        ErrorCreateSession,

        /// <summary>
        /// Represents an error state trying to browse namespaces
        /// </summary>
        ErrorBrowseNamespace,

        /// <summary>
        /// Represents an error state trying to add asubscription
        /// </summary>
        ErrorAddSubscription,

        /// <summary>
        /// A state where the keep alive process has been stopped
        /// </summary>
        KeepAliveStopped,

        /// <summary>
        /// A state where the <see cref="Opc.Ua.Client.Session"/> has been disconnected
        /// </summary>
        Disconnected
    };
}
