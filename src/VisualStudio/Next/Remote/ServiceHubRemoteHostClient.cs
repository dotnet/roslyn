// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly HubClient _hubClient;
        private readonly Stream _remoteHostStream;
        private readonly JsonRpc _rpc;
        // communication channel related to snapshot information
        private readonly SnapshotJsonRpcClient _snapshotClient;

        public static async Task<ServiceHubRemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            var hubClient = new HubClient("RoslynPrimaryHubClient");
            var remoteHostStream = await hubClient.RequestServiceAsync(WellKnownServiceHubServices.RemoteHostService, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host
            var snapshotStream = await hubClient.RequestServiceAsync(WellKnownServiceHubServices.ServiceHubSnapshotService, cancellationToken).ConfigureAwait(false);

            var instance = new ServiceHubRemoteHostClient(workspace, hubClient, remoteHostStream, snapshotStream);

            // make sure connection is done right
            var current = $"VS ({Process.GetCurrentProcess().Id})";
            var host = await instance._rpc.InvokeAsync<string>(WellKnownServiceHubServices.RemoteHostService_Connect, current).ConfigureAwait(false);

            // TODO: change this to non fatal watson and make VS to use inproc implementation
            Contract.ThrowIfFalse(host == current.ToString());

            instance.Connected();

            // return instance
            return instance;
        }

        private ServiceHubRemoteHostClient(Workspace workspace, HubClient hubClient, Stream remoteHostStream, Stream snapshotStream) :
            base(workspace)
        {
            _hubClient = hubClient;
            _remoteHostStream = remoteHostStream;

            _rpc = JsonRpc.Attach(remoteHostStream, target: this);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;

            _snapshotClient = new SnapshotJsonRpcClient(workspace.Services.GetRequiredService<ISolutionChecksumService>(), snapshotStream);
        }

        protected override async Task<Session> CreateCodeAnalysisServiceSessionAsync(ChecksumScope snapshot, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await _hubClient.RequestServiceAsync(WellKnownServiceHubServices.CodeAnalysisService, cancellationToken).ConfigureAwait(false);

            return new JsonRpcSession(snapshot, callbackTarget, serviceStream, cancellationToken);
        }

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
            _rpc.Dispose();
            _remoteHostStream.Dispose();
            _snapshotClient.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected();
        }
    }
}
