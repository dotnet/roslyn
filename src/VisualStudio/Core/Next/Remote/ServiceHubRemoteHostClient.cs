// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly HubClient _hubClient;
        private readonly Stream _stream;
        private readonly JsonRpc _rpc;

        public static async Task<RemoteHostClient> CreateAsync(
            Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                var primary = new HubClient("ManagedLanguage.IDE.RemoteHostClient");
                var remoteHostStream = await primary.RequestServiceAsync(WellKnownRemoteHostServices.RemoteHostService, cancellationToken).ConfigureAwait(false);

                var instance = new ServiceHubRemoteHostClient(workspace, primary, remoteHostStream);

                // make sure connection is done right
                var current = $"VS ({Process.GetCurrentProcess().Id})";
                var host = await instance._rpc.InvokeAsync<string>(WellKnownRemoteHostServices.RemoteHostService_Connect, current).ConfigureAwait(false);

                // TODO: change this to non fatal watson and make VS to use inproc implementation
                Contract.ThrowIfFalse(host == current.ToString());

                instance.Connected();

                // Create a workspace host to hear about workspace changes.  We'll 
                // remote those changes over to the remote side when they happen.
                RegisterWorkspaceHost(workspace, instance);

                // return instance
                return instance;
            }
        }

        private static void RegisterWorkspaceHost(Workspace workspace, RemoteHostClient client)
        {
            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return;
            }

            vsWorkspace.ProjectTracker.RegisterWorkspaceHost(
                new WorkspaceHost(vsWorkspace, client));
        }

        private ServiceHubRemoteHostClient(Workspace workspace, HubClient hubClient, Stream stream) :
            base(workspace)
        {
            _hubClient = hubClient;
            _stream = stream;

            _rpc = JsonRpc.Attach(stream, target: this);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;
        }

        protected override async Task<Session> CreateServiceSessionAsync(string serviceName, ChecksumScope snapshot, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host
            var snapshotStream = await _hubClient.RequestServiceAsync(WellKnownServiceHubServices.SnapshotService, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await _hubClient.RequestServiceAsync(serviceName, cancellationToken).ConfigureAwait(false);

            return new JsonRpcSession(snapshot, snapshotStream, callbackTarget, serviceStream, cancellationToken);
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