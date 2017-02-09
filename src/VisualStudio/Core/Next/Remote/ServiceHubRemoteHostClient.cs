// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly HubClient _hubClient;
        private readonly JsonRpc _rpc;
        private readonly HostGroup _hostGroup;

        public static async Task<RemoteHostClient> CreateAsync(
            Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                var primary = new HubClient("ManagedLanguage.IDE.RemoteHostClient");
                var current = $"VS ({Process.GetCurrentProcess().Id})";

                var hostGroup = new HostGroup(current);
                var remoteHostStream = await RequestServiceAsync(primary, WellKnownRemoteHostServices.RemoteHostService, hostGroup, cancellationToken).ConfigureAwait(false);

                var instance = new ServiceHubRemoteHostClient(workspace, primary, hostGroup, remoteHostStream);

                // make sure connection is done right
                var host = await instance._rpc.InvokeAsync<string>(WellKnownRemoteHostServices.RemoteHostService_Connect, current).ConfigureAwait(false);

                // TODO: change this to non fatal watson and make VS to use inproc implementation
                Contract.ThrowIfFalse(host == current.ToString());

                instance.Connected();

                // Create a workspace host to hear about workspace changes.  We'll 
                // remote those changes over to the remote side when they happen.
                await RegisterWorkspaceHostAsync(workspace, instance).ConfigureAwait(false);

                // return instance
                return instance;
            }
        }

        private static async Task RegisterWorkspaceHostAsync(Workspace workspace, RemoteHostClient client)
        {
            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return;
            }

            // don't block UI thread while initialize workspace host
            var host = new WorkspaceHost(vsWorkspace, client);
            await host.InitializeAsync().ConfigureAwait(false);

            // RegisterWorkspaceHost is required to be called from UI thread so push the code
            // to UI thread to run. 
            await Task.Factory.SafeStartNew(() =>
            {
                vsWorkspace.GetProjectTrackerAndInitializeIfNecessary(Shell.ServiceProvider.GlobalProvider).RegisterWorkspaceHost(host);
            }, CancellationToken.None, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.TaskScheduler).ConfigureAwait(false);
        }

        private ServiceHubRemoteHostClient(
            Workspace workspace, HubClient hubClient, HostGroup hostGroup, Stream stream) :
            base(workspace)
        {
            _hubClient = hubClient;
            _hostGroup = hostGroup;

            _rpc = new JsonRpc(stream, stream, target: this);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;

            _rpc.StartListening();
        }

        protected override async Task<Session> CreateServiceSessionAsync(string serviceName, PinnedRemotableDataScope snapshot, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host
            var snapshotStream = await RequestServiceAsync(_hubClient, WellKnownServiceHubServices.SnapshotService, _hostGroup, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await RequestServiceAsync(_hubClient, serviceName, _hostGroup, cancellationToken).ConfigureAwait(false);

            return await JsonRpcSession.CreateAsync(snapshot, snapshotStream, callbackTarget, serviceStream, cancellationToken).ConfigureAwait(false);
        }

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
            _rpc.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected();
        }

        private static async Task<Stream> RequestServiceAsync(HubClient client, string serviceName, HostGroup hostGroup, CancellationToken cancellationToken = default(CancellationToken))
        {
            const int max_retry = 10;
            const int retry_delayInMS = 50;

            // call to get service can fail due to this bug - devdiv#288961
            // until root cause is fixed, we decide to have retry rather than fail right away
            for (var i = 0; i < max_retry; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };
                    return await client.RequestServiceAsync(descriptor, cancellationToken).ConfigureAwait(false);
                }
                catch (RemoteInvocationException ex)
                {
                    // RequestServiceAsync should never fail unless service itself is actually broken.
                    // right now, we know only 1 case where it can randomly fail. but there might be more cases so 
                    // adding non fatal watson here.
                    FatalError.ReportWithoutCrash(ex);
                }

                // wait for retry_delayInMS before next try
                await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);
            }

            return Contract.FailWithReturn<Stream>("Fail to get service. look FatalError.s_reportedException for more detail");
        }
    }
}
