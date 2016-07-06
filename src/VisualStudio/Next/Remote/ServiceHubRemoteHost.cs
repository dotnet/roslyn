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
    internal partial class ServiceHubRemoteHost : RemoteHost
    {
        private readonly HubClient _hubClient;
        private readonly Stream _stream;
        private readonly JsonRpc _rpc;

        public static async Task<ServiceHubRemoteHost> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            var primary = new HubClient("Primary");
            var remoteHostStream = await primary.RequestServiceAsync(WellKnownServiceHubServices.RemoteHostService, cancellationToken).ConfigureAwait(false);

            var instance = new ServiceHubRemoteHost(workspace, primary, remoteHostStream);

            // make sure connection is done right
            var current = $"VS ({Process.GetCurrentProcess().Id})";
            var host = await instance._rpc.InvokeAsync<string>(WellKnownServiceHubServices.RemoteHostService_Connect, current).ConfigureAwait(false);

            // TODO: change this to non fatal watson and make VS to use inproc implementation
            Contract.ThrowIfFalse(host == current.ToString());

            instance.Connected();

            // return instance
            return instance;
        }

        private ServiceHubRemoteHost(Workspace workspace, HubClient hubClient, Stream stream) :
            base(workspace)
        {
            _hubClient = hubClient;
            _stream = stream;

            _rpc = JsonRpc.Attach(stream, this);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;
        }

        public override Task<Stream> CreateCodeAnalysisServiceStreamAsync(CancellationToken cancellationToken)
        {
            return _hubClient.RequestServiceAsync(WellKnownServiceHubServices.CodeAnalysisService, cancellationToken);
        }

        protected override async Task<Session> CreateSnapshotSessionAsync(SolutionSnapshot snapshot, CancellationToken cancellationToken)
        {
            return new JsonRpcSnapshotSession(snapshot, await _hubClient.RequestServiceAsync(WellKnownServiceHubServices.SolutionSnapshotService, cancellationToken).ConfigureAwait(false));
        }

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
            _rpc.Dispose();
            _stream.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected();
        }
    }
}
