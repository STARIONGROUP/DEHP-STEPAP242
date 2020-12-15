// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpcClientService.cs" company="RHEA System S.A.">
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

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPSTEPAP242.Enumerator;
    using DEHPSTEPAP242.Services.OpcConnector.Interfaces;

    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Configuration;

    using ReactiveUI;

    using LogManager = NLog.LogManager;

    /// <summary>
    /// The <see cref="OpcClientService"/> handles the OPC connection with an OPC server configured through EcosimPro
    /// </summary>
    public class OpcClientService : ReactiveObject, IOpcClientService
    {
        /// <summary>
        /// The current class <see cref="NLog.Logger"/>
        /// </summary>
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControl;

        /// <summary>
        /// The <see cref="IOpcSessionHandler"/>
        /// </summary>
        private readonly IOpcSessionHandler sessionHandler;

        /// <summary>
        /// The <see cref="IOpcSessionReconnectHandler"/>
        /// </summary>
        private readonly IOpcSessionReconnectHandler reconnectHandler;

        /// <summary>
        /// The endpoint url
        /// </summary>
        private string endpointUrl;

        /// <summary>
        /// An assert whether the certificate should be auto accepted
        /// </summary>
        private bool autoAccept;
            
        /// <summary>
        /// The refresh interval for subscriptions in millisecond
        /// </summary>
        public int RefreshInterval { get; set; } = 1000;

        /// <summary>
        /// Backing field for <see cref="OpcClientStatusCode"/>
        /// </summary>
        private OpcClientStatusCode opcClientStatusCode;

        /// <summary>
        /// Gets the <see cref="OpcClientStatusCode"/> reflecting the connection status of this <see cref="OpcClientService"/>
        /// </summary>
        public OpcClientStatusCode OpcClientStatusCode
        { 
            get => this.opcClientStatusCode;
            private set => this.RaiseAndSetIfChanged(ref this.opcClientStatusCode, value);
        }

        /// <summary>
        /// Gets the collection of <see cref="ReferenceDescription"/> that holds for instance <see cref="ReferenceDescription.NodeId"/>
        /// </summary>
        public ReferenceDescriptionCollection References { get; private set; } = new ReferenceDescriptionCollection();

        /// <summary>
        /// Initializes a new <see cref="OpcClientService"/>
        /// </summary>
        /// <param name="statusBarViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="sessionHandler">The <see cref="IOpcSessionHandler"/></param>
        /// <param name="reconnectHandler">The <see cref="IOpcSessionReconnectHandler"/></param>
        public OpcClientService(IStatusBarControlViewModel statusBarViewModel, IOpcSessionHandler sessionHandler, IOpcSessionReconnectHandler reconnectHandler)
        {
            this.statusBarControl = statusBarViewModel;
            this.sessionHandler = sessionHandler;
            this.reconnectHandler = reconnectHandler;
        }

        /// <summary>
        /// Connects the client to the endpoint opening a <see cref="Session"/>
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null)
        {
            this.endpointUrl = endpoint;
            this.autoAccept = autoAcceptConnection;

            try
            {
                await this.Open(credential);
            }
            catch (Exception ex)
            {
                this.statusBarControl.Append($"{ex.Message}", StatusBarMessageSeverity.Error);
                Logger.Error($"Exception: {ex.Message}");
                throw;
            }

            this.OpcClientStatusCode = OpcClientStatusCode.Connected;
        }

        /// <summary>
        /// Closes the <see cref="Session"/>
        /// </summary>
        public void CloseSession()
        {
            this.sessionHandler.CloseSession();
            this.OpcClientStatusCode = OpcClientStatusCode.Disconnected;
            this.statusBarControl.Append($"Session from {this.endpointUrl} has been closed", StatusBarMessageSeverity.Warning);
        }

        /// <summary>
        /// Opens a connection to the <see cref="endpointUrl"/>
        /// </summary>
        /// <param name="credential">The <see cref="IUserIdentity"/> to use to authenticate the connection</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task Open(IUserIdentity credential)
        {
            this.statusBarControl.Append("Creating OPC-UA Application Configuration");
            this.OpcClientStatusCode = OpcClientStatusCode.ErrorCreateApplication;

            var application = new ApplicationInstance
            {
                ApplicationName = "DEPHEcosimPro OPC-UA Client", ApplicationType = ApplicationType.Client, ConfigSectionName = "Resources/OpcClient"
            };

            var configuration = await application.LoadApplicationConfiguration(false);

            if (!await application.CheckApplicationInstanceCertificate(false, 0))
            {
                Logger.Error("Missing application certificate, using unsecure connection.");
                return;
            }

            configuration.ApplicationUri = Utils.GetApplicationUriFromCertificate(configuration.SecurityConfiguration.ApplicationCertificate.Certificate);

            if (configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                this.autoAccept = true;
            }

            configuration.CertificateValidator.CertificateValidation += this.CertificateValidator;

            this.statusBarControl.Append($"Discovering endpoints of {this.endpointUrl}");
            this.OpcClientStatusCode = OpcClientStatusCode.ErrorDiscoverEndpoints;
            var selectedEndpoint = this.sessionHandler.SelectEndpoint(this.endpointUrl);

            Logger.Info($"Selected endpoint uses: {selectedEndpoint.SecurityPolicyUri.Substring(selectedEndpoint.SecurityPolicyUri.LastIndexOf('#') + 1)}");

            this.statusBarControl.Append("Openning a session with the OPC UA server");

            this.OpcClientStatusCode = OpcClientStatusCode.ErrorCreateSession;
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);
            await this.sessionHandler.CreateSession(configuration, endpoint, false, "DEHPSTEPAP242 OPC UA Client", 60000, credential, null);

            this.sessionHandler.KeepAlive += this.OnClientKeepAlive;

            this.statusBarControl.Append("Browsing the OPC UA server namespace");
            this.OpcClientStatusCode = OpcClientStatusCode.ErrorBrowseNamespace;

            this.References = this.sessionHandler.FetchReferences(ObjectIds.ObjectsFolder);

            this.sessionHandler.Browse(ObjectIds.ObjectsFolder, ReferenceTypeIds.HierarchicalReferences, true,
                (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _, out var references);

            this.References = references;
            var additionalReferences = new ReferenceDescriptionCollection();

            // REM OUTPUT
            foreach (var reference in this.References)
            {
                this.statusBarControl.Append($"{reference.DisplayName}, {reference.BrowseName}, {reference.NodeClass}");

                this.sessionHandler.Browse(ExpandedNodeId.ToNodeId(reference.NodeId, this.sessionHandler.NamespaceUris),
                    ReferenceTypeIds.HierarchicalReferences, true, 
                    (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _, out var referenceDescriptions);

                additionalReferences.AddRange(referenceDescriptions);

                foreach (var referenceDescription in this.References)
                {
                    this.statusBarControl.Append($"{referenceDescription.DisplayName}, {referenceDescription.BrowseName}, {referenceDescription.NodeClass}");
                }
            }

            this.References.AddRange(additionalReferences);
            this.AddSubscription(this.sessionHandler.DefaultSubscription);
        }

        /// <summary>
        /// Adds a subscription based on the attributeId to monitor
        /// </summary>
        /// <param name="attributeId">The attribute Id of the node to monitor</param>
        /// <param name="onNotification">A event handler to call back on Notification</param>
        public void AddSubscription(uint attributeId, MonitoredItemNotificationEventHandler onNotification = null)
        {
            var subscription = new Subscription() { PublishingInterval = this.RefreshInterval, DefaultItem = new MonitoredItem() { AttributeId = attributeId} };
            this.AddSubscription(subscription, onNotification);
        }

        /// <summary>
        /// Adds a subscription to the <see cref="Session"/>
        /// </summary>
        /// <param name="subscription">A <see cref="Subscription"/> to be added to the <see cref="Session"/></param>
        /// <param name="onNotification">A event handler to call back on Notification</param>
        private void AddSubscription(Subscription subscription, MonitoredItemNotificationEventHandler onNotification = null)
        {
            try
            {
                this.statusBarControl.Append($"Create a subscription with publishing interval of {this.RefreshInterval} ms");

                this.statusBarControl.Append("Add a list of items (server current time and status) to the subscription.");

                var list = new List<MonitoredItem>
                {
                    new MonitoredItem(subscription.DefaultItem)
                    {
                        DisplayName = "ServerStatusCurrentTime", StartNodeId = $"i={Variables.Server_ServerStatus_CurrentTime}"
                    }
                };

                list.ForEach(i => i.Notification += onNotification ?? this.OnNotification);
                subscription.AddItems(list);

                this.statusBarControl.Append("Add the subscription to the session.");
                this.sessionHandler.AddSubscription(subscription);
                this.OpcClientStatusCode = OpcClientStatusCode.Connected;
            }
            catch (Exception exception)
            {
                Logger.Warn($"Error creating subscription for attribute id = {subscription.DefaultItem.AttributeId}. Exception: {exception.Message}");
                this.statusBarControl.Append($"Error creating subscription for attribute id = {subscription.DefaultItem.AttributeId}", StatusBarMessageSeverity.Error);
                this.OpcClientStatusCode = OpcClientStatusCode.ErrorAddSubscription;
            }
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
            return this.sessionHandler.CallMethod(objectId, methodId, arguments);
        }

        /// <summary>
        /// The <see cref="KeepAliveEventHandler"/> that is used to keep the <see cref="Session"/> alive
        /// </summary>
        /// <param name="sender">The <see cref="Session"/> object</param>
        /// <param name="e">The <see cref="KeepAliveEventArgs"/></param>
        [ExcludeFromCodeCoverage]
        private void OnClientKeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (this.sessionHandler.KeepAliveStopped)
            {
                this.OpcClientStatusCode = OpcClientStatusCode.KeepAliveStopped;
                return;
            }

            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                this.statusBarControl.Append($"{e.Status} {sender.OutstandingRequestCount}/{sender.DefunctRequestCount}");

                if (this.reconnectHandler.SessionReconnectHandler == null)
                {
                    this.statusBarControl.Append("Reconnecting to the Opc session", StatusBarMessageSeverity.Warning);
                    this.reconnectHandler.Activate();
                    this.reconnectHandler.BeginReconnect(sender, this.OnReconnectComplete, 1000);
                }
            }
        }

        /// <summary>
        /// The <see cref="EventHandler"/> that is called when the session has reconnected
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="EventArgs"/></param>
        [ExcludeFromCodeCoverage]
        private void OnReconnectComplete(object sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, this.reconnectHandler))
            {
                return;
            }

            this.sessionHandler.SetSession(this.reconnectHandler);
            this.reconnectHandler?.Deactivate();

            this.statusBarControl.Append("Reconnected to the Opc session ");
        }

        /// <summary>
        /// The default <see cref="MonitoredItemNotificationEventHandler"/> that is called when a notification is caught
        /// </summary>
        /// <param name="item">The <see cref="MonitoredItem"/></param>
        /// <param name="e">The <see cref="MonitoredItemNotificationEventArgs"/></param>
        [ExcludeFromCodeCoverage]
        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                this.statusBarControl.Append($"{item.DisplayName}: {value.Value}, {value.SourceTimestamp}, {value.StatusCode}, {e.NotificationValue.TypeId}");
            }
        }

        /// <summary>
        /// The <see cref="CertificateValidator"/> validates
        /// </summary>
        /// <param name="validator">A <see cref="CertificateValidator"/></param>
        /// <param name="e">The <see cref="CertificateValidationEventArgs"/></param>
        [ExcludeFromCodeCoverage]
        public void CertificateValidator(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode != StatusCodes.BadCertificateUntrusted)
            {
                return;
            }
            
            e.Accept = this.autoAccept;

            this.statusBarControl.Append(this.autoAccept ? $"Accepted Certificate: {e.Certificate.Subject}" : $"Rejected Certificate: {e.Certificate.Subject}");
        }
    }
}
