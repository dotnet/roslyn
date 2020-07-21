// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient, IRemoteHostServiceCallback
    {
        private const int ConnectionPoolCapacity = 15;

        private readonly HostWorkspaceServices _services;
        private readonly IRemotableDataService _remotableDataService;
        private readonly RemoteEndPoint _endPoint;
        private readonly HubClient _hubClient;
        private readonly HostGroup _hostGroup;

        private readonly ConnectionPools? _connectionPools;

        private ServiceHubRemoteHostClient(
            HostWorkspaceServices services,
            HubClient hubClient,
            HostGroup hostGroup,
            Stream stream)
        {
            _connectionPools = new ConnectionPools(
                connectionFactory: (serviceName, pool, cancellationToken) => CreateConnectionImplAsync(serviceName, callbackTarget: null, pool, cancellationToken),
                capacity: ConnectionPoolCapacity);

            _services = services;
            _hubClient = hubClient;
            _hostGroup = hostGroup;

            _endPoint = new RemoteEndPoint(stream, hubClient.Logger, incomingCallTarget: this);
            _endPoint.Disconnected += OnDisconnected;
            _endPoint.UnexpectedExceptionThrown += OnUnexpectedExceptionThrown;
            _endPoint.StartListening();

            _remotableDataService = services.GetRequiredService<IRemotableDataService>();
        }

        private void OnUnexpectedExceptionThrown(Exception unexpectedException)
            => RemoteHostCrashInfoBar.ShowInfoBar(_services, unexpectedException);

        public static async Task<RemoteHostClient> CreateAsync(HostWorkspaceServices services, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, KeyValueLogMessage.NoProperty, cancellationToken))
            {
                Logger.Log(FunctionId.RemoteHost_Bitness, KeyValueLogMessage.Create(LogType.Trace, m => m["64bit"] = RemoteHostOptions.IsServiceHubProcess64Bit(services)));

                // let each client to have unique id so that we can distinguish different clients when service is restarted
                var clientId = $"VS ({Process.GetCurrentProcess().Id}) ({Guid.NewGuid()})";

                var hostGroup = new HostGroup(clientId);
                var hubClient = new HubClient("ManagedLanguage.IDE.RemoteHostClient");

                // use the hub client logger for unexpected exceptions from devenv as well, so we have complete information in the log:
                WatsonReporter.InitializeLogger(hubClient.Logger);

                var remoteHostStream = await RequestServiceAsync(services, hubClient, WellKnownServiceHubService.RemoteHost, hostGroup, cancellationToken).ConfigureAwait(false);

                var client = new ServiceHubRemoteHostClient(services, hubClient, hostGroup, remoteHostStream);

                var uiCultureLCID = CultureInfo.CurrentUICulture.LCID;
                var cultureLCID = CultureInfo.CurrentCulture.LCID;

                var success = false;
                try
                {
                    // initialize the remote service
                    await client._endPoint.InvokeAsync<string>(
                        nameof(IRemoteHostService.InitializeGlobalState),
                        new object[] { clientId, uiCultureLCID, cultureLCID, TelemetryService.DefaultSession.SerializeSettings() },
                        cancellationToken).ConfigureAwait(false);

                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        client.Dispose();
                    }
                }

                client.Started();
                return client;
            }
        }

        public static async Task<Stream> RequestServiceAsync(
            HostWorkspaceServices services,
            HubClient client,
            RemoteServiceName serviceName,
            HostGroup hostGroup,
            CancellationToken cancellationToken)
        {
            var is64bit = RemoteHostOptions.IsServiceHubProcess64Bit(services);

            // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true)
            await TaskScheduler.Default;

            var descriptor = new ServiceDescriptor(serviceName.ToString(is64bit)) { HostGroup = hostGroup };
            try
            {
                return await client.RequestServiceAsync(descriptor, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (ReportNonFatalWatson(e, cancellationToken))
            {
                // TODO: Once https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1040692.
                // ServiceHub may throw non-cancellation exceptions if it is called after VS started to shut down,
                // even if our cancellation token is signaled. Cancel the operation and do not report an error in these cases.
                //
                // If ServiceHub did not throw non-cancellation exceptions when cancellation token is signaled,
                // we can assume that these exceptions indicate a failure and should be reported to the user.
                cancellationToken.ThrowIfCancellationRequested();

                RemoteHostCrashInfoBar.ShowInfoBar(services, e);

                // TODO: Propagate the original exception (see https://github.com/dotnet/roslyn/issues/40476)
                throw new SoftCrashException("Unexpected exception from HubClient", e, cancellationToken);
            }

            static bool ReportNonFatalWatson(Exception e, CancellationToken cancellationToken)
            {
                // ServiceHub may throw non-cancellation exceptions if it is called after VS started to shut down,
                // even if our cancellation token is signaled. Do not report Watson in such cases to reduce noice.
                if (!cancellationToken.IsCancellationRequested)
                {
                    FatalError.ReportWithoutCrash(e);
                }

                return true;
            }
        }

        public HostGroup HostGroup => _hostGroup;

        public override string ClientId => _hostGroup.Id;

        public override Task<RemoteServiceConnection> CreateConnectionAsync(RemoteServiceName serviceName, object? callbackTarget, CancellationToken cancellationToken)
        {
            // When callbackTarget is given, we can't share/pool connection since callbackTarget attaches a state to connection.
            // so connection is only valid for that specific callbackTarget. it is up to the caller to keep connection open
            // if he wants to reuse same connection.

            if (callbackTarget == null && _connectionPools != null)
            {
                return _connectionPools.GetOrCreateConnectionAsync(serviceName, cancellationToken);
            }

            return CreateConnectionImplAsync(serviceName, callbackTarget, poolReclamation: null, cancellationToken);
        }

        private async Task<RemoteServiceConnection> CreateConnectionImplAsync(RemoteServiceName serviceName, object? callbackTarget, IPooledConnectionReclamation? poolReclamation, CancellationToken cancellationToken)
        {
            var serviceStream = await RequestServiceAsync(_services, _hubClient, serviceName, _hostGroup, cancellationToken).ConfigureAwait(false);
            return new JsonRpcConnection(_services, _hubClient.Logger, callbackTarget, serviceStream, poolReclamation);
        }

        public override void Dispose()
        {
            _endPoint.Disconnected -= OnDisconnected;
            _endPoint.UnexpectedExceptionThrown -= OnUnexpectedExceptionThrown;
            _endPoint.Dispose();

            _connectionPools?.Dispose();
            _hubClient.Dispose();

            base.Dispose();
        }

        private void OnDisconnected(JsonRpcDisconnectedEventArgs e)
            => Dispose();

        #region Assets

        /// <summary>
        /// Remote API.
        /// </summary>
        public async Task GetAssetsAsync(int scopeId, Checksum[] checksums, string pipeName, CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.JsonRpcSession_RequestAssetAsync, pipeName, cancellationToken))
                {
                    await RemoteEndPoint.WriteDataToNamedPipeAsync(
                        pipeName,
                        (scopeId, checksums),
                        (writer, data, cancellationToken) => RemoteHostAssetSerialization.WriteDataAsync(writer, _remotableDataService, data.scopeId, data.checksums, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(experimentName));
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        #endregion
    }
}
