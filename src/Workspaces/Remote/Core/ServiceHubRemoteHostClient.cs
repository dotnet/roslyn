﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.ServiceHub.Client;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly HostWorkspaceServices _services;
        private readonly SolutionAssetStorage _assetStorage;
        private readonly HubClient _hubClient;
        private readonly ServiceBrokerClient _serviceBrokerClient;
        private readonly IErrorReportingService? _errorReportingService;
        private readonly IRemoteHostClientShutdownCancellationService? _shutdownCancellationService;
        private readonly IRemoteServiceCallbackDispatcherProvider _callbackDispatcherProvider;

        private readonly bool _isRemoteHostServerGC;
        private readonly bool _isRemoteHostCoreClr;

        private ServiceHubRemoteHostClient(
            HostWorkspaceServices services,
            IGlobalOptionService globalOptions,
            ServiceBrokerClient serviceBrokerClient,
            HubClient hubClient,
            IRemoteServiceCallbackDispatcherProvider callbackDispatcherProvider)
        {
            // use the hub client logger for unexpected exceptions from devenv as well, so we have complete information in the log:
            services.GetService<IWorkspaceTelemetryService>()?.RegisterUnexpectedExceptionLogger(hubClient.Logger);

            _services = services;
            _serviceBrokerClient = serviceBrokerClient;
            _hubClient = hubClient;
            _callbackDispatcherProvider = callbackDispatcherProvider;

            _assetStorage = services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            _errorReportingService = services.GetService<IErrorReportingService>();
            _shutdownCancellationService = services.GetService<IRemoteHostClientShutdownCancellationService>();
            _isRemoteHostServerGC = RemoteHostOptions.IsServiceHubProcessServerGC(globalOptions);
            _isRemoteHostCoreClr = RemoteHostOptions.IsServiceHubProcessCoreClr(globalOptions);
        }

        public static async Task<RemoteHostClient> CreateAsync(
            HostWorkspaceServices services,
            IGlobalOptionService globalOptions,
            AsynchronousOperationListenerProvider listenerProvider,
            IServiceBroker serviceBroker,
            RemoteServiceCallbackDispatcherRegistry callbackDispatchers,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, KeyValueLogMessage.NoProperty, cancellationToken))
            {
#pragma warning disable ISB001    // Dispose of proxies
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
                var serviceBrokerClient = new ServiceBrokerClient(serviceBroker);
#pragma warning restore

                var hubClient = new HubClient("ManagedLanguage.IDE.RemoteHostClient");

                var client = new ServiceHubRemoteHostClient(services, globalOptions, serviceBrokerClient, hubClient, callbackDispatchers);

                await client.TryInvokeAsync<IRemoteAsynchronousOperationListenerService>(
                    (service, cancellationToken) => service.EnableAsync(AsynchronousOperationListenerProvider.IsEnabled, listenerProvider.DiagnosticTokensEnabled, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                client.Started();
                return client;
            }
        }

        /// <summary>
        /// Creates connection to built-in remote service.
        /// </summary>
        public override RemoteServiceConnection<T> CreateConnection<T>(object? callbackTarget)
            => CreateConnection<T>(ServiceDescriptors.Instance, _callbackDispatcherProvider, callbackTarget);

        /// <summary>
        /// This overload is meant to be used by partner teams from their External Access layer.
        /// </summary>
        internal RemoteServiceConnection<T> CreateConnection<T>(ServiceDescriptors descriptors, IRemoteServiceCallbackDispatcherProvider callbackDispatcherProvider, object? callbackTarget) where T : class
        {
            var descriptor = descriptors.GetServiceDescriptor(typeof(T), _isRemoteHostServerGC, _isRemoteHostCoreClr);
            var callbackDispatcher = (descriptor.ClientInterface != null) ? callbackDispatcherProvider.GetDispatcher(typeof(T)) : null;

            return new BrokeredServiceConnection<T>(
                descriptor,
                callbackTarget,
                callbackDispatcher,
                _serviceBrokerClient,
                _assetStorage,
                _errorReportingService,
                _shutdownCancellationService);
        }

        public override void Dispose()
        {
            _services.GetService<IWorkspaceTelemetryService>()?.UnregisterUnexpectedExceptionLogger(_hubClient.Logger);
            _hubClient.Dispose();

            _serviceBrokerClient.Dispose();

            base.Dispose();
        }
    }
}
