// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOpcSessionReconnectHandler.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.Services.OpcConnector.Interfaces
{
    using System;
    using System.Threading;

    using Opc.Ua.Client;

    /// <summary>
    /// Interface definition for <see cref="OpcSessionReconnectHandler"/>
    /// </summary>
    public interface IOpcSessionReconnectHandler
    {
        /// <summary>
        /// Gets the <see cref="SessionReconnectHandler"/> that this <see cref="OpcSessionReconnectHandler"/> wraps up
        /// </summary>
        SessionReconnectHandler SessionReconnectHandler { get; }

        /// <summary>
        /// Gets the session managed by the handler.
        /// </summary>
        Session Session { get; }

        /// <summary>
        /// Renew the <see cref="OpcSessionReconnectHandler.SessionReconnectHandler"/>
        /// </summary>
        void Activate();

        /// <summary>
        /// Begins the reconnect process.
        /// </summary>
        /// <param name="session">The <see cref="OpcSessionReconnectHandler.Session"/> to reconnect</param>
        /// <param name="dueTime">The amount of time to delay before <paramref name="callback" /> is invoked, in milliseconds.
        /// Specify <see cref="Timeout.Infinite" /> to prevent the timer from starting. Specify zero (0) to start the timer immediately. </param>
        /// <param name="callback">A delegate representing the method to be executed when the reconnection is complete</param>
        void BeginReconnect(Session session, EventHandler callback, int dueTime = 1000);

        /// <summary>
        /// Frees any unmanaged resources and sets to null
        /// </summary>
        void Deactivate();
    }
}
