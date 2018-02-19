// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private enum GlobalNotificationState
        {
            NotStarted,
            Started,
            Finished
        }

        private static int s_instanceId = 0;

        private readonly JsonRpc _rpc;
        private readonly ConnectionManager _connectionManager;

        /// <summary>
        /// Lock for the <see cref="_globalNotificationsTask"/> task chain.  Each time we hear 
        /// about a global operation starting or stopping (i.e. a build) we will '.ContinueWith'
        /// this task chain with a new notification to the OOP side.  This way all the messages
        /// are properly serialized and appera in the right order (i.e. we don't hear about a 
        /// stop prior to hearing about the relevant start).
        /// </summary>
        private readonly object _globalNotificationsGate = new object();
        private Task<GlobalNotificationState> _globalNotificationsTask = Task.FromResult(GlobalNotificationState.NotStarted);

        public static async Task<RemoteHostClient> CreateAsync(
            Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                var primary = new HubClient("ManagedLanguage.IDE.RemoteHostClient");
                var timeout = TimeSpan.FromMilliseconds(workspace.Options.GetOption(RemoteHostOptions.RequestServiceTimeoutInMS));

                // Retry (with timeout) until we can connect to RemoteHost (service hub process). 
                // we are seeing cases where we failed to connect to service hub process when a machine is under heavy load.
                // (see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/481103 as one of example)
                var instance = await Connections.RetryRemoteCallAsync<IOException, ServiceHubRemoteHostClient>(
                    () => CreateWorkerAsync(workspace, primary, timeout, cancellationToken), timeout, cancellationToken).ConfigureAwait(false);

                instance.Started();

                // Create a workspace host to hear about workspace changes.  We'll 
                // remote those changes over to the remote side when they happen.
                await RegisterWorkspaceHostAsync(workspace, instance).ConfigureAwait(false);

                // return instance
                return instance;
            }
        }

        public static async Task<ServiceHubRemoteHostClient> CreateWorkerAsync(Workspace workspace, HubClient primary, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ServiceHubRemoteHostClient client = null;
            try
            {
                // let each client to have unique id so that we can distinguish different clients when service is restarted
                var currentInstanceId = Interlocked.Add(ref s_instanceId, 1);

                var current = $"VS ({Process.GetCurrentProcess().Id}) ({currentInstanceId})";

                var hostGroup = new HostGroup(current);
                var remoteHostStream = await Connections.RequestServiceAsync(primary, WellKnownRemoteHostServices.RemoteHostService, hostGroup, timeout, cancellationToken).ConfigureAwait(false);

                var remotableDataRpc = new RemotableDataJsonRpc(
                                          workspace, primary.Logger,
                                          await Connections.RequestServiceAsync(primary, WellKnownServiceHubServices.SnapshotService, hostGroup, timeout, cancellationToken).ConfigureAwait(false));

                var enableConnectionPool = workspace.Options.GetOption(RemoteHostOptions.EnableConnectionPool);
                var maxConnection = workspace.Options.GetOption(RemoteHostOptions.MaxPoolConnection);

                var connectionManager = new ConnectionManager(primary, hostGroup, enableConnectionPool, maxConnection, timeout, new ReferenceCountedDisposable<RemotableDataJsonRpc>(remotableDataRpc));

                client = new ServiceHubRemoteHostClient(workspace, connectionManager, remoteHostStream);

                var uiCultureLCID = CultureInfo.CurrentUICulture.LCID;
                var cultureLCID = CultureInfo.CurrentCulture.LCID;

                // make sure connection is done right
                var host = await client._rpc.InvokeWithCancellationAsync<string>(
                    nameof(IRemoteHostService.Connect), new object[] { current, uiCultureLCID, cultureLCID, TelemetryService.DefaultSession.SerializeSettings() }, cancellationToken).ConfigureAwait(false);

                return client;
            }
            catch (Exception ex)
            {
                // make sure we shutdown client if initializing client has failed.
                client?.Shutdown();

                // translate to our own cancellation if it is raised.
                cancellationToken.ThrowIfCancellationRequested();

                // otherwise, report watson and throw original exception
                ex.ReportServiceHubNFW("ServiceHub creation failed");
                throw;
            }
        }

        // internal for debugging purpose
        internal static async Task RegisterWorkspaceHostAsync(Workspace workspace, RemoteHostClient client)
        {
            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return;
            }

            // Create a connection to the host in the BG to avoid taking the hit of loading service 
            // hub on the UI thread.  We'll initially set its ref count to 1, and we will decrement 
            // that ref-count at the end of the using block.  During this time though, when the 
            // projectTracker is sending events, the workspace host can then use that connection 
            // instead of having to expensively spin up a fresh one.
            var session = await client.TryCreateKeepAliveSessionAsync(WellKnownRemoteHostServices.RemoteHostService, CancellationToken.None).ConfigureAwait(false);
            var host = new WorkspaceHost(vsWorkspace, session);

            // RegisterWorkspaceHost is required to be called from UI thread so push the code
            // to UI thread to run. 
            await Task.Factory.SafeStartNew(() =>
            {
                var projectTracker = vsWorkspace.GetProjectTrackerAndInitializeIfNecessary(Shell.ServiceProvider.GlobalProvider);

                projectTracker.RegisterWorkspaceHost(host);
                projectTracker.StartSendingEventsToWorkspaceHost(host);
            }, CancellationToken.None, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.TaskScheduler).ConfigureAwait(false);
        }

        private ServiceHubRemoteHostClient(
            Workspace workspace,
            ConnectionManager connectionManager,
            Stream stream)
            : base(workspace)
        {
            _connectionManager = connectionManager;

            _rpc = new JsonRpc(new JsonRpcMessageHandler(stream, stream), target: this);
            _rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;

            _rpc.StartListening();
        }

        public override Task<Connection> TryCreateConnectionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
        {
            return _connectionManager.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken);
        }

        protected override void OnStarted()
        {
            RegisterGlobalOperationNotifications();
        }

        protected override void OnStopped()
        {
            // we are asked to stop. unsubscribe and dispose to disconnect.
            // there are 2 ways to get disconnected. one is Roslyn decided to disconnect with RemoteHost (ex, cancellation or recycle OOP) and
            // the other is external thing disconnecting remote host from us (ex, user killing OOP process).
            // the Disconnected event we subscribe is to detect #2 case. and this method is for #1 case. so when we are willingly disconnecting
            // we don't need the event, otherwise, Disconnected event will be called twice.
            UnregisterGlobalOperationNotifications();
            _rpc.Disconnected -= OnRpcDisconnected;
            _rpc.Dispose();
            _connectionManager.Shutdown();
        }

        private void RegisterGlobalOperationNotifications()
        {
            var globalOperationService = this.Workspace.Services.GetService<IGlobalOperationNotificationService>();
            if (globalOperationService != null)
            {
                globalOperationService.Started += OnGlobalOperationStarted;
                globalOperationService.Stopped += OnGlobalOperationStopped;
            }
        }

        private void UnregisterGlobalOperationNotifications()
        {
            var globalOperationService = this.Workspace.Services.GetService<IGlobalOperationNotificationService>();
            if (globalOperationService != null)
            {
                globalOperationService.Started -= OnGlobalOperationStarted;
                globalOperationService.Stopped -= OnGlobalOperationStopped;
            }

            Task localTask;
            lock (_globalNotificationsGate)
            {
                // Unilaterally transition us to the finished state.  Once we're finished
                // we cannot start or stop anymore.
                _globalNotificationsTask = _globalNotificationsTask.ContinueWith(
                    _ => GlobalNotificationState.Finished, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                localTask = _globalNotificationsTask;
            }

            // Have to wait for all the notifications to make it to the OOP side so we keep
            // it in a consistent state.  Also, if we don't do this, our _rpc object will
            // get disposed while we're remoting over the messages to the oop side.
            localTask.Wait();
        }

        private void OnGlobalOperationStarted(object sender, EventArgs e)
        {
            lock (_globalNotificationsGate)
            {
                _globalNotificationsTask = _globalNotificationsTask.ContinueWith(
                    continuation, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            }

            async Task<GlobalNotificationState> continuation(Task<GlobalNotificationState> previousTask)
            {
                // Can only transition from NotStarted->Started.  If we hear about
                // anything else, do nothing.
                if (previousTask.Result != GlobalNotificationState.NotStarted)
                {
                    return previousTask.Result;
                }

                await _rpc.InvokeAsync(
                    nameof(IRemoteHostService.OnGlobalOperationStarted), "").ConfigureAwait(false);

                return GlobalNotificationState.Started;
            }
        }

        private void OnGlobalOperationStopped(object sender, GlobalOperationEventArgs e)
        {
            lock (_globalNotificationsGate)
            {
                _globalNotificationsTask = _globalNotificationsTask.ContinueWith(
                    continuation, CancellationToken.None,
                    TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            }

            async Task<GlobalNotificationState> continuation(Task<GlobalNotificationState> previousTask)
            {
                // Can only transition from Started->NotStarted.  If we hear about
                // anything else, do nothing.
                if (previousTask.Result != GlobalNotificationState.Started)
                {
                    return previousTask.Result;
                }

                await _rpc.InvokeAsync(
                    nameof(IRemoteHostService.OnGlobalOperationStopped),
                    e.Operations, e.Cancelled).ConfigureAwait(false);

                // Mark that we're stopped now.
                return GlobalNotificationState.NotStarted;
            }
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Stopped();
        }
    }
}
