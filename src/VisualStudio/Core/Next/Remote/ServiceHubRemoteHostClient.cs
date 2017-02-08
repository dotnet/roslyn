// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly HubClient _hubClient;
        private readonly JsonRpc _rpc;
        private readonly HostGroup _hostGroup;
        private readonly TimeSpan _timeout;

        public static async Task<RemoteHostClient> CreateAsync(
            Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                var primary = new HubClient("ManagedLanguage.IDE.RemoteHostClient");
                var current = $"VS ({Process.GetCurrentProcess().Id})";

                var hostGroup = new HostGroup(current);
                var timeout = TimeSpan.FromMilliseconds(workspace.Options.GetOption(RemoteHostOptions.RequestServiceTimeoutInMS));
                var remoteHostStream = await RequestServiceAsync(primary, WellKnownRemoteHostServices.RemoteHostService, hostGroup, timeout, cancellationToken).ConfigureAwait(false);

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
                vsWorkspace.ProjectTracker.RegisterWorkspaceHost(host);
            }, CancellationToken.None, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.TaskScheduler).ConfigureAwait(false);
        }

        private ServiceHubRemoteHostClient(
            Workspace workspace, HubClient hubClient, HostGroup hostGroup, Stream stream) :
            base(workspace)
        {
            _hubClient = hubClient;
            _hostGroup = hostGroup;
            _timeout = TimeSpan.FromMilliseconds(workspace.Options.GetOption(RemoteHostOptions.RequestServiceTimeoutInMS));

            _rpc = JsonRpc.Attach(stream, target: this);
            _rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;
        }

        protected override async Task<Session> CreateServiceSessionAsync(string serviceName, PinnedRemotableDataScope snapshot, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host
            var snapshotStream = await RequestServiceAsync(_hubClient, WellKnownServiceHubServices.SnapshotService, _hostGroup, _timeout, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await RequestServiceAsync(_hubClient, serviceName, _hostGroup, _timeout, cancellationToken).ConfigureAwait(false);

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

        private static async Task<Stream> RequestServiceAsync(
            HubClient client,
            string serviceName,
            HostGroup hostGroup,
            TimeSpan timeout,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            const int max_retry = 10;
            const int retry_delayInMS = 50;

            Exception lastException = null;

            var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };

            // call to get service can fail due to this bug - devdiv#288961 or more.
            // until root cause is fixed, we decide to have retry rather than fail right away
            for (var i = 0; i < max_retry; i++)
            {
                try
                {
                    return await RequestServiceAsync(client, descriptor, timeout, cancellationToken).ConfigureAwait(false);
                }
                catch (RemoteInvocationException ex)
                {
                    // RequestServiceAsync should never fail unless service itself is actually broken.
                    // So far, we catched multiple issues from this NFW. so we will keep this NFW.
                    // one request from service hub team is adding service hub logs when this happen.
                    // tracked by https://github.com/dotnet/roslyn/issues/17012
                    FatalError.ReportWithoutCrash(ex);
                    lastException = ex;
                }

                // wait for retry_delayInMS before next try
                await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);
            }

            // crash right away to get better dump. otherwise, we will get dump from async exception
            // which most likely lost all valuable data
            FatalError.ReportUnlessCanceled(lastException);
            GC.KeepAlive(lastException);

            // unreachable
            throw ExceptionUtilities.Unreachable;
        }

        private static async Task<Stream> RequestServiceAsync(HubClient client, ServiceDescriptor descriptor, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            // we are wrapping HubClient.RequestServiceAsync since we can't control its internal timeout value ourselves.
            // we have bug opened to track the issue.
            // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/Editor/_workitems?id=378757&fullScreen=false&_a=edit
            const int retry_delayInMS = 50;

            var start = DateTime.UtcNow;
            while (start - DateTime.UtcNow < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await client.RequestServiceAsync(descriptor, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // if it is our own cancellation token, then rethrow
                    // otherwise, let us retry.
                    //
                    // we do this since HubClient itself can throw its own cancellation token
                    // when it couldn't connect to service hub service for some reasons
                    // (ex, OOP process GC blocked and not responding to request)
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // wait for retry_delayInMS before next try
                await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);
            }

            // request service to HubClient timed out, more than we are willing to wait
            throw new TimeoutException("RequestServiceAsync timed out");
        }
    }
}