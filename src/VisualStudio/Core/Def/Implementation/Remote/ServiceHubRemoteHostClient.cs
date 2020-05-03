// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using StreamJsonRpc;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient, IRemoteHostServiceCallback
    {
        private readonly RemoteEndPoint _endPoint;
        private readonly HubClient _hubClient;
        private readonly HostGroup _hostGroup;

        private readonly ConnectionPool? _connectionPool;

        private ServiceHubRemoteHostClient(
            Workspace workspace,
            HubClient hubClient,
            HostGroup hostGroup,
            Stream stream)
            : base(workspace)
        {
            if (workspace.Options.GetOption(RemoteHostOptions.EnableConnectionPool))
            {
                int maxPoolConnection = workspace.Options.GetOption(RemoteHostOptions.MaxPoolConnection);

                _connectionPool = new ConnectionPool(
                    connectionFactory: (serviceName, cancellationToken) => CreateConnectionAsync(serviceName, callbackTarget: null, cancellationToken),
                    maxPoolConnection);
            }

            _hubClient = hubClient;
            _hostGroup = hostGroup;

            _endPoint = new RemoteEndPoint(stream, hubClient.Logger, incomingCallTarget: this);
            _endPoint.Disconnected += OnDisconnected;
            _endPoint.UnexpectedExceptionThrown += OnUnexpectedExceptionThrown;
            _endPoint.StartListening();
        }

        private void OnUnexpectedExceptionThrown(Exception unexpectedException)
            => RemoteHostCrashInfoBar.ShowInfoBar(Workspace, unexpectedException);

        public static async Task<RemoteHostClient?> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                // let each client to have unique id so that we can distinguish different clients when service is restarted
                var clientId = CreateClientId(Process.GetCurrentProcess().Id.ToString());

                var hostGroup = new HostGroup(clientId);
                var hubClient = new HubClient("ManagedLanguage.IDE.RemoteHostClient");

                // use the hub client logger for unexpected exceptions from devenv as well, so we have complete information in the log:
                WatsonReporter.InitializeLogger(hubClient.Logger);

                var remoteHostStream = await RequestServiceAsync(workspace, hubClient, WellKnownServiceHubServices.RemoteHostService, hostGroup, cancellationToken).ConfigureAwait(false);

                var client = new ServiceHubRemoteHostClient(workspace, hubClient, hostGroup, remoteHostStream);

                var uiCultureLCID = CultureInfo.CurrentUICulture.LCID;
                var cultureLCID = CultureInfo.CurrentCulture.LCID;

                bool success = false;
                try
                {
                    // initialize the remote service
                    _ = await client._endPoint.InvokeAsync<string>(
                        nameof(IRemoteHostService.Connect),
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
            Workspace workspace,
            HubClient client,
            string serviceName,
            HostGroup hostGroup,
            CancellationToken cancellationToken)
        {
            var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };
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

                RemoteHostCrashInfoBar.ShowInfoBar(workspace, e);

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
        public override bool IsRemoteHost64Bit => RemoteHostOptions.IsServiceHubProcess64Bit(Workspace);

        protected override Task<Connection?> TryCreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
        {
            // When callbackTarget is given, we can't share/pool connection since callbackTarget attaches a state to connection.
            // so connection is only valid for that specific callbackTarget. it is up to the caller to keep connection open
            // if he wants to reuse same connection.

            if (callbackTarget == null && _connectionPool != null)
            {
                return _connectionPool.GetOrCreateConnectionAsync(serviceName, cancellationToken).AsNullable();
            }

            return CreateConnectionAsync(serviceName, callbackTarget, cancellationToken).AsNullable();
        }

        private async Task<Connection> CreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
        {
            var serviceStream = await RequestServiceAsync(Workspace, _hubClient, serviceName, _hostGroup, cancellationToken).ConfigureAwait(false);
            return new JsonRpcConnection(Workspace, _hubClient.Logger, callbackTarget, serviceStream);
        }

        public override void Dispose()
        {
            _endPoint.Disconnected -= OnDisconnected;
            _endPoint.UnexpectedExceptionThrown -= OnUnexpectedExceptionThrown;
            _endPoint.Dispose();

            _connectionPool?.Dispose();
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
                        (writer, data, cancellationToken) => RemoteHostAssetSerialization.WriteDataAsync(writer, RemotableDataService, data.scopeId, data.checksums, cancellationToken),
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
                return Task.FromResult(Workspace.Services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(experimentName));
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        #endregion
    }
}
