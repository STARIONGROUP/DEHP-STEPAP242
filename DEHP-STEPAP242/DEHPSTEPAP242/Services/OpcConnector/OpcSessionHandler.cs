// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpcSessionHandler.cs" company="RHEA System S.A.">
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

namespace DEHPSTEPAP242.Services.OpcConnector
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    using DEHPSTEPAP242.Services.OpcConnector.Interfaces;

    using Opc.Ua;
    using Opc.Ua.Client;

    /// <summary>
    /// The <see cref="OpcSessionHandler"/> is a wrapper arround the <see cref="Opc.Ua.Client.Session"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OpcSessionHandler : IOpcSessionHandler
    {
        /// <summary>
        /// Gets the <see cref="Opc.Ua.Client.Session"/>
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// Gets or Sets the default subscription for the session.
        /// </summary>
        public Subscription DefaultSubscription
        {
            get => this.Session.DefaultSubscription;
            set => this.Session.DefaultSubscription = value;
        }

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        public NamespaceTable NamespaceUris => this.Session.NamespaceUris;

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times the KeepAliveInterval.
        /// Set to false is communication recovers.
        /// </remarks>
        public bool KeepAliveStopped => this.Session.KeepAliveStopped;

        /// <summary>
        /// Raised when a keep alive arrives from the server or an error is detected.
        /// </summary>
        /// <remarks>
        /// Once a session is created a timer will periodically read the server state and current time.
        /// If this read operation succeeds this event will be raised each time the keep alive period elapses.
        /// If an error is detected (KeepAliveStopped == true) then this event will be raised as well.
        /// </remarks>
        public event KeepAliveEventHandler KeepAlive
        {
            add => this.Session.KeepAlive += value;
            remove => this.Session.KeepAlive -= value;
        }
    
        /// <summary>
        /// Fetches all references for the specified node.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <returns>A <see cref="ReferenceDescriptionCollection"/></returns>
        public ReferenceDescriptionCollection FetchReferences(NodeId nodeId)
        {
            return this.Session.FetchReferences(nodeId);
        }

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
        public void Browse(NodeId nodeToBrowse, NodeId referenceTypeId, bool includeSubtypes, 
            uint nodeClassMask, out byte[] continuationPoint, out ReferenceDescriptionCollection references)
        {
            this.Session.Browse(null, null, nodeToBrowse, 0u, BrowseDirection.Forward, referenceTypeId,
                includeSubtypes, nodeClassMask, out continuationPoint, out references);
        }

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
        public async Task CreateSession(ApplicationConfiguration configuration, ConfiguredEndpoint endpoint, bool updateBeforeConnect, string sessionName,
            uint sessionTimeout, IUserIdentity identity, IList<string> preferredLocales)
        {
            this.Session = await Session.Create(configuration, endpoint, updateBeforeConnect, sessionName, sessionTimeout, identity, preferredLocales);
        }

        /// <summary>
        /// Adds a subscription to the session.
        /// </summary>
        /// <param name="subscription">The subscription to add.</param>
        /// <returns>An assert whether the removal whent ok</returns>
        public bool AddSubscription(Subscription subscription)
        {
            var result = this.Session.AddSubscription(subscription);
            subscription.Create();
            return result;
        }

        /// <summary>
        /// Removes a subscription from the session.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <returns>An assert whether the removal whent ok</returns>
        public bool RemoveSubscription(Subscription subscription)
        {
            return this.Session.RemoveSubscription(subscription);
        }

        /// <summary>
        /// Removes a list of subscriptions from the sessiont.
        /// </summary>
        /// <param name="subscriptions">The list of subscriptions to remove.</param>
        /// <returns>An assert whether the removal whent ok</returns>
        public bool RemoveSubscriptions(IEnumerable<Subscription> subscriptions)
        {
            return this.Session.RemoveSubscriptions(subscriptions);
        }

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The <see cref="NodeId"/> object Id</param>
        /// <param name="methodId">The <see cref="NodeId"/> method Id </param>
        /// <param name="arguments">The arguments to input</param>
        /// <returns>The <see cref="IList{T}"/> of output argument values.</returns>
        public IList<object> CallMethod(NodeId objectId, NodeId methodId, params object[] arguments)
        {
            return this.Session.Call(objectId, methodId, arguments);
        }

        /// <summary>
        /// Sets the session from a <see cref="IOpcSessionReconnectHandler"/>
        /// </summary>
        /// <param name="reconnectHandler">A <see cref="IOpcSessionReconnectHandler"/></param>
        public void SetSession(IOpcSessionReconnectHandler reconnectHandler)
        {
            this.Session = reconnectHandler?.Session;
        }

        /// <summary>
        /// Closes the <see cref="Session"/> and deletes subscription
        /// </summary>
        /// <param name="deleteSubscription">An assert whether to delete subscriptions</param>
        public void CloseSession(bool deleteSubscription = true)
        {
            this.Session.Close();
        }

        /// <summary>
        /// Finds the endpoint that best matches the current settings.
        /// </summary>
        /// <param name="discoveryUrl">The discovery URL.</param>
        /// <param name="useSecurity">if set to <c>true</c> select an endpoint that uses security.</param>
        /// <param name="discoverTimeout">Operation timeout in milliseconds.</param>
        /// <returns>The best available <see cref="EndpointDescription"/>.</returns>
        public EndpointDescription SelectEndpoint(string discoveryUrl, bool useSecurity = true, int discoverTimeout = 15000)
        {
            return CoreClientUtils.SelectEndpoint(discoveryUrl, useSecurity, discoverTimeout);
        }
    }
}
