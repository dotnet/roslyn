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
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
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

            Exception lastException = null;

            var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };

            // call to get service can fail due to this bug - devdiv#288961
            // until root cause is fixed, we decide to have retry rather than fail right away
            for (var i = 0; i < max_retry; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await client.RequestServiceAsync(descriptor, cancellationToken).ConfigureAwait(false);
                }
                catch (RemoteInvocationException ex)
                {
                    // save info only if it failed with different issue than before.
                    if (lastException?.Message != ex.Message)
                    {
                        // RequestServiceAsync should never fail unless service itself is actually broken.
                        // So far, we catched multiple issues from this NFW. so we will keep this NFW.
                        WatsonReporter.Report("RequestServiceAsync Failed", ex, ReportDetailInfo);

                        lastException = ex;
                    }
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

        private static int ReportDetailInfo(IFaultUtility faultUtility)
        {
            // 0 means send watson, otherwise, cancel watson
            // we always send watson since dump itself can have valuable data
            var exitCode = 0;

            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "servicehub", "logs");
                if (!Directory.Exists(logPath))
                {
                    return exitCode;
                }

                // attach all log files that are modified less than 1 day before.
                var now = DateTime.UtcNow;
                var oneDay = TimeSpan.FromDays(1);

                foreach (var file in Directory.EnumerateFiles(logPath, "*.log"))
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    if (now - lastWrite > oneDay)
                    {
                        continue;
                    }

                    faultUtility.AddFile(file);
                }
            }
            catch (Exception ex) when (ReportNonIOException(ex))
            {
            }

            return exitCode;
        }

        private static bool ReportNonIOException(Exception ex)
        {
            // IOException is expected. log other exceptions
            if (!(ex is IOException))
            {
                WatsonReporter.Report(ex);
            }

            // catch all exception. not worth crashing VS.
            return true;
        }
    }
}
