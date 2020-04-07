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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private enum GlobalNotificationState
        {
            NotStarted,
            Started,
            Finished
        }

        private readonly RemoteEndPoint _endPoint;
        private readonly ConnectionManager _connectionManager;
        private readonly CancellationTokenSource _shutdownCancellationTokenSource;

        /// <summary>
        /// Lock for the <see cref="_globalNotificationsTask"/> task chain.  Each time we hear 
        /// about a global operation starting or stopping (i.e. a build) we will '.ContinueWith'
        /// this task chain with a new notification to the OOP side.  This way all the messages
        /// are properly serialized and appera in the right order (i.e. we don't hear about a 
        /// stop prior to hearing about the relevant start).
        /// </summary>
        private readonly object _globalNotificationsGate = new object();
        private Task<GlobalNotificationState> _globalNotificationsTask = Task.FromResult(GlobalNotificationState.NotStarted);

        private ServiceHubRemoteHostClient(
            Workspace workspace,
            TraceSource logger,
            ConnectionManager connectionManager,
            Stream stream)
            : base(workspace)
        {
            _shutdownCancellationTokenSource = new CancellationTokenSource();

            _connectionManager = connectionManager;

            _endPoint = new RemoteEndPoint(stream, logger, incomingCallTarget: this);
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
                var enableConnectionPool = workspace.Options.GetOption(RemoteHostOptions.EnableConnectionPool);
                var maxConnection = workspace.Options.GetOption(RemoteHostOptions.MaxPoolConnection);

                // let each client to have unique id so that we can distinguish different clients when service is restarted
                var clientId = CreateClientId(Process.GetCurrentProcess().Id.ToString());

                var hostGroup = new HostGroup(clientId);
                var hubClient = new HubClient("ManagedLanguage.IDE.RemoteHostClient");

                // use the hub client logger for unexpected exceptions from devenv as well, so we have complete information in the log:
                WatsonReporter.InitializeLogger(hubClient.Logger);

                // Create the RemotableDataJsonRpc before we create the remote host: this call implicitly sets up the remote IExperimentationService so that will be available for later calls
                var snapshotServiceStream = await RequestServiceAsync(workspace, hubClient, WellKnownServiceHubServices.SnapshotService, hostGroup, cancellationToken).ConfigureAwait(false);
                var remoteHostStream = await RequestServiceAsync(workspace, hubClient, WellKnownRemoteHostServices.RemoteHostService, hostGroup, cancellationToken).ConfigureAwait(false);

                var remotableDataProvider = new RemotableDataProvider(workspace, hubClient.Logger, snapshotServiceStream);
                var connectionManager = new ConnectionManager(workspace, hubClient, hostGroup, enableConnectionPool, maxConnection, new ReferenceCountedDisposable<RemotableDataProvider>(remotableDataProvider));

                var client = new ServiceHubRemoteHostClient(workspace, hubClient.Logger, connectionManager, remoteHostStream);

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

        public override string ClientId => _connectionManager.HostGroup.Id;
        public override bool IsRemoteHost64Bit => RemoteHostOptions.IsServiceHubProcess64Bit(Workspace);

        public override Task<Connection?> TryCreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
            => _connectionManager.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken);

        protected override void OnStarted()
            => RegisterGlobalOperationNotifications();

        public override void Dispose()
        {
            // cancel all pending async work
            _shutdownCancellationTokenSource.Cancel();

            // we are asked to stop. unsubscribe and dispose to disconnect.
            // there are 2 ways to get disconnected. one is Roslyn decided to disconnect with RemoteHost (ex, cancellation or recycle OOP) and
            // the other is external thing disconnecting remote host from us (ex, user killing OOP process).
            // the Disconnected event we subscribe is to detect #2 case. and this method is for #1 case. so when we are willingly disconnecting
            // we don't need the event, otherwise, Disconnected event will be called twice.
            UnregisterGlobalOperationNotifications();

            _endPoint.Disconnected -= OnDisconnected;
            _endPoint.UnexpectedExceptionThrown -= OnUnexpectedExceptionThrown;
            _endPoint.Dispose();

            _connectionManager.Dispose();

            base.Dispose();
        }

        public HostGroup HostGroup
        {
            get
            {
                Debug.Assert(_connectionManager.HostGroup.Id == ClientId);
                return _connectionManager.HostGroup;
            }
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
                _globalNotificationsTask = _globalNotificationsTask.SafeContinueWithFromAsync(
                    continuation, _shutdownCancellationTokenSource.Token, TaskContinuationOptions.None, TaskScheduler.Default);
            }

            async Task<GlobalNotificationState> continuation(Task<GlobalNotificationState> previousTask)
            {
                // Can only transition from NotStarted->Started.  If we hear about
                // anything else, do nothing.
                if (previousTask.Result != GlobalNotificationState.NotStarted)
                {
                    return previousTask.Result;
                }

                await _endPoint.InvokeAsync(
                    nameof(IRemoteHostService.OnGlobalOperationStarted),
                    new object[] { "" },
                    _shutdownCancellationTokenSource.Token).ConfigureAwait(false);

                return GlobalNotificationState.Started;
            }
        }

        private void OnGlobalOperationStopped(object sender, GlobalOperationEventArgs e)
        {
            lock (_globalNotificationsGate)
            {
                _globalNotificationsTask = _globalNotificationsTask.SafeContinueWithFromAsync(
                    continuation, _shutdownCancellationTokenSource.Token, TaskContinuationOptions.None, TaskScheduler.Default);
            }

            async Task<GlobalNotificationState> continuation(Task<GlobalNotificationState> previousTask)
            {
                // Can only transition from Started->NotStarted.  If we hear about
                // anything else, do nothing.
                if (previousTask.Result != GlobalNotificationState.Started)
                {
                    return previousTask.Result;
                }

                await _endPoint.InvokeAsync(
                    nameof(IRemoteHostService.OnGlobalOperationStopped),
                    new object[] { e.Operations, e.Cancelled },
                    _shutdownCancellationTokenSource.Token).ConfigureAwait(false);

                // Mark that we're stopped now.
                return GlobalNotificationState.NotStarted;
            }
        }

        private void OnDisconnected(JsonRpcDisconnectedEventArgs e)
            => Dispose();
    }
}
