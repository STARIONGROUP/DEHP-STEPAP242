// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOpcSessionHandler.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Opc.Ua;
    using Opc.Ua.Client;

    /// <summary>
    /// Interface definition for <see cref="OpcSessionHandler"/>
    /// </summary>
    public interface IOpcSessionHandler
    {
        /// <summary>
        /// Gets the <see cref="Opc.Ua.Client.Session"/>
        /// </summary>
        Session Session { get; }

        /// <summary>
        /// Gets or Sets the default subscription for the session.
        /// </summary>
        Subscription DefaultSubscription { get; set; }

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        NamespaceTable NamespaceUris { get; }

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times the KeepAliveInterval.
        /// Set to false is communication recovers.
        /// </remarks>
        bool KeepAliveStopped { get; }

        /// <summary>
        /// Raised when a keep alive arrives from the server or an error is detected.
        /// </summary>
        /// <remarks>
        /// Once a session is created a timer will periodically read the server state and current time.
        /// If this read operation succeeds this event will be raised each time the keep alive period elapses.
        /// If an error is detected (KeepAliveStopped == true) then this event will be raised as well.
        /// </remarks>
        event KeepAliveEventHandler KeepAlive;

        /// <summary>
        /// Fetches all references for the specified node.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <returns>A <see cref="ReferenceDescriptionCollection"/></returns>
        ReferenceDescriptionCollection FetchReferences(NodeId nodeId);

        /// <summary>
        /// Invokes the Browse service.
        /// </summary>
        /// <param name="nodeToBrowse">The node to browse.</param>
        /// <param name="referenceTypeId">The reference type id.</param>
        /// <param name="includeSubtypes">If set to <c>true</c> the subtypes of the ReferenceType will be included in the browse.</param>
        /// <param name="nodeClassMask">The node class mask.</param>
        /// <param name="continuationPoint">The continuation point.</param>
        /// <param name="references">The list of node references.</param>
        /// <returns>A<see cref="ResponseHeader"/></returns>
        void Browse(NodeId nodeToBrowse, NodeId referenceTypeId, bool includeSubtypes, 
            uint nodeClassMask, out byte[] continuationPoint, out ReferenceDescriptionCollection references);

        /// <summary>
        /// Creates a new communication session with a server by invoking the CreateSession service
        /// </summary>
        /// <param name="configuration">The configuration for the client application.</param>
        /// <param name="endpoint">The endpoint for the server.</param>
        /// <param name="updateBeforeConnect">If set to <c>true</c> the discovery endpoint is used to update the endpoint description before connecting.</param>
        /// <param name="sessionName">The name to assign to the session.</param>
        /// <param name="sessionTimeout">The timeout period for the session.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="preferredLocales">The user identity to associate with the session.</param>
        /// <returns>The new session object</returns>
        Task CreateSession(ApplicationConfiguration configuration, ConfiguredEndpoint endpoint, bool updateBeforeConnect, string sessionName,
            uint sessionTimeout, IUserIdentity identity, IList<string> preferredLocales);

        /// <summary>
        /// Adds a subscription to the session.
        /// </summary>
        /// <param name="subscription">The subscription to add.</param>
        /// <returns>An assert whether the removal whent ok</returns>
        bool AddSubscription(Subscription subscription);

        /// <summary>
        /// Removes a subscription from the session.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <returns>An assert whether the removal whent ok</returns>
        bool RemoveSubscription(Subscription subscription);

        /// <summary>
        /// Removes a list of subscriptions from the sessiont.
        /// </summary>
        /// <param name="subscriptions">The list of subscriptions to remove.</param>
        /// <returns>An assert whether the removal whent ok</returns>
        bool RemoveSubscriptions(IEnumerable<Subscription> subscriptions);

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The <see cref="NodeId"/> object Id</param>
        /// <param name="methodId">The <see cref="NodeId"/> method Id </param>
        /// <param name="arguments">The arguments to input</param>
        /// <returns>The <see cref="IList{T}"/> of output argument values.</returns>
        IList<object> CallMethod(NodeId objectId, NodeId methodId, params object[] arguments);

        /// <summary>
        /// Sets the session from a <see cref="IOpcSessionReconnectHandler"/>
        /// </summary>
        /// <param name="reconnectHandler">A <see cref="IOpcSessionReconnectHandler"/></param>
        void SetSession(IOpcSessionReconnectHandler reconnectHandler);

        /// <summary>
        /// Closes the <see cref="OpcSessionHandler.Session"/> and deletes subscription
        /// </summary>
        /// <param name="deleteSubscription">An assert whether to delete subscriptions</param>
        void CloseSession(bool deleteSubscription = true);

        /// <summary>
        /// Finds the endpoint that best matches the current settings.
        /// </summary>
        /// <param name="discoveryUrl">The discovery URL.</param>
        /// <param name="useSecurity">if set to <c>true</c> select an endpoint that uses security.</param>
        /// <param name="discoverTimeout">Operation timeout in milliseconds.</param>
        /// <returns>The best available <see cref="EndpointDescription"/>.</returns>
        EndpointDescription SelectEndpoint(string discoveryUrl, bool useSecurity = true, int discoverTimeout = 15000);
    }
}
