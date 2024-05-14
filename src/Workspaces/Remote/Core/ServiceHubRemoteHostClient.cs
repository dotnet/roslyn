// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.ServiceHub.Client;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly SolutionServices _services;
        private readonly SolutionAssetStorage _assetStorage;
        private readonly ReferenceCountedDisposable<HubClient> _hubClient;
        private readonly ReferenceCountedDisposable<ServiceBrokerClient> _serviceBrokerClient;
        private readonly IErrorReportingService? _errorReportingService;
        private readonly IRemoteHostClientShutdownCancellationService? _shutdownCancellationService;
        private readonly IRemoteServiceCallbackDispatcherProvider _callbackDispatcherProvider;

        public readonly RemoteProcessConfiguration Configuration;

        private Process? _remoteProcess;

        private ServiceHubRemoteHostClient(
            SolutionServices services,
            RemoteProcessConfiguration configuration,
            ReferenceCountedDisposable<ServiceBrokerClient> serviceBrokerClient,
            ReferenceCountedDisposable<HubClient> hubClient,
            IRemoteServiceCallbackDispatcherProvider callbackDispatcherProvider)
        {
            // use the hub client logger for unexpected exceptions from devenv as well, so we have complete information in the log:
            services.GetService<IWorkspaceTelemetryService>()?.RegisterUnexpectedExceptionLogger(hubClient.Target.Logger);

            _services = services;
            _serviceBrokerClient = serviceBrokerClient.TryAddReference() ?? throw ExceptionUtilities.Unreachable();
            _hubClient = hubClient.TryAddReference() ?? throw ExceptionUtilities.Unreachable();
            _callbackDispatcherProvider = callbackDispatcherProvider;

            _assetStorage = services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            _errorReportingService = services.GetService<IErrorReportingService>();
            _shutdownCancellationService = services.GetService<IRemoteHostClientShutdownCancellationService>();
            Configuration = configuration;
        }

        public static async Task<ReferenceCountedDisposable<RemoteHostClient>> CreateAsync(
            SolutionServices services,
            RemoteProcessConfiguration configuration,
            AsynchronousOperationListenerProvider listenerProvider,
            IServiceBroker serviceBroker,
            RemoteServiceCallbackDispatcherRegistry callbackDispatchers,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, KeyValueLogMessage.NoProperty, cancellationToken))
            {
                // Create the ServiceBrokerClient and HubClient used to communicate with the remote process.  Initially,
                // both have a ref-count of 1, and will be disposed if we return out of this method unsuccessfully.  If
                // we are able to successfully create a RemoteHostClient, we will add its own ref-counts to those
                // objects ensuring they stay alive as long as the RemoteHostClient is alive.

#pragma warning disable ISB001    // Dispose of proxies
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
                using var serviceBrokerClient = new ReferenceCountedDisposable<ServiceBrokerClient>(new(serviceBroker));
#pragma warning restore

                using var hubClient = new ReferenceCountedDisposable<HubClient>(new("ManagedLanguage.IDE.RemoteHostClient"));

                // Now, also create the client.  Ensuring that it gets cleaned up if any of the code below failed.
                using var client = new ReferenceCountedDisposable<RemoteHostClient>(new ServiceHubRemoteHostClient(services, configuration, serviceBrokerClient, hubClient, callbackDispatchers));

                var workspaceConfigurationService = services.GetRequiredService<IWorkspaceConfigurationService>();

                var remoteProcessId = await client.Target.TryInvokeAsync<IRemoteProcessTelemetryService, int>(
                    (service, cancellationToken) => service.InitializeAsync(workspaceConfigurationService.Options, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (remoteProcessId.HasValue)
                {
                    try
                    {
                        ((ServiceHubRemoteHostClient)client.Target)._remoteProcess = Process.GetProcessById(remoteProcessId.Value);
                    }
                    catch (Exception e)
                    {
                        hubClient.Target.Logger.TraceEvent(TraceEventType.Error, 1, $"Unable to find Roslyn ServiceHub process: {e.Message}");
                    }
                }
                else
                {
                    hubClient.Target.Logger.TraceEvent(TraceEventType.Error, 1, "Roslyn ServiceHub process initialization failed.");
                }

                await client.Target.TryInvokeAsync<IRemoteAsynchronousOperationListenerService>(
                    (service, cancellationToken) => service.EnableAsync(AsynchronousOperationListenerProvider.IsEnabled, listenerProvider.DiagnosticTokensEnabled, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                // We've succeeded in fully creating the RemoteHostClient.  Ensure we have an extra ref-count to offset
                // the refcount+Dispose we setup above.
                return client.TryAddReference() ?? throw ExceptionUtilities.Unreachable();
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
            var descriptor = descriptors.GetServiceDescriptor(typeof(T), Configuration);
            var callbackDispatcher = (descriptor.ClientInterface != null) ? callbackDispatcherProvider.GetDispatcher(typeof(T)) : null;

            // Add additional refs to the client and service broker so they stay alive as long as the connection is
            // alive.  When the last connection is disposed and we are disposed, these will finally get truly disposed.
            using var hubClient = _hubClient.TryAddReference();
            using var serviceBrokerClient = _serviceBrokerClient.TryAddReference();

            // We've been disposed.  Just return a connection that fails on every operation. All code that calls into
            // the client or connections already has to be written to handle failures on oop calls, so they should
            // already be fine with this.
            if (hubClient is null || serviceBrokerClient is null)
                return NoOpRemoteServiceConnection<T>.Instance;

            return new BrokeredServiceConnection<T>(
                descriptor,
                callbackTarget,
                callbackDispatcher,
                hubClient,
                serviceBrokerClient,
                _assetStorage,
                _errorReportingService,
                _shutdownCancellationService,
                _remoteProcess);
        }

        public override void Dispose()
        {
            _services.GetService<IWorkspaceTelemetryService>()?.UnregisterUnexpectedExceptionLogger(_hubClient.Target.Logger);

            _hubClient.Dispose();
            _serviceBrokerClient.Dispose();
        }
    }
}
