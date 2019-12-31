﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private enum GlobalNotificationState
        {
            NotStarted,
            Started,
            Finished
        }

        private readonly JsonRpc _rpc;
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

        public static async Task<RemoteHostClient?> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                var timeout = TimeSpan.FromMilliseconds(workspace.Options.GetOption(RemoteHostOptions.RequestServiceTimeoutInMS));
                var enableConnectionPool = workspace.Options.GetOption(RemoteHostOptions.EnableConnectionPool);
                var maxConnection = workspace.Options.GetOption(RemoteHostOptions.MaxPoolConnection);

                // let each client to have unique id so that we can distinguish different clients when service is restarted
                var clientId = CreateClientId(Process.GetCurrentProcess().Id.ToString());

                var hostGroup = new HostGroup(clientId);
                var primary = new HubClient("ManagedLanguage.IDE.RemoteHostClient");

                ServiceHubRemoteHostClient? client = null;
                try
                {
                    // Create the RemotableDataJsonRpc before we create the remote host: this call implicitly sets up the remote IExperimentationService so that will be available for later calls
                    var snapshotServiceStream = await Connections.RequestServiceAsync(workspace, primary, WellKnownServiceHubServices.SnapshotService, hostGroup, timeout, cancellationToken).ConfigureAwait(false);
                    var remoteHostStream = await Connections.RequestServiceAsync(workspace, primary, WellKnownRemoteHostServices.RemoteHostService, hostGroup, timeout, cancellationToken).ConfigureAwait(false);

                    var remotableDataRpc = new RemotableDataJsonRpc(workspace, primary.Logger, snapshotServiceStream);
                    var connectionManager = new ConnectionManager(primary, hostGroup, enableConnectionPool, maxConnection, timeout, new ReferenceCountedDisposable<RemotableDataJsonRpc>(remotableDataRpc));

                    client = new ServiceHubRemoteHostClient(workspace, primary.Logger, connectionManager, remoteHostStream);

                    var uiCultureLCID = CultureInfo.CurrentUICulture.LCID;
                    var cultureLCID = CultureInfo.CurrentCulture.LCID;

                    // make sure connection is done right
                    var host = await client._rpc.InvokeWithCancellationAsync<string>(
                        nameof(IRemoteHostService.Connect), new object[] { clientId, uiCultureLCID, cultureLCID, TelemetryService.DefaultSession.SerializeSettings() }, cancellationToken).ConfigureAwait(false);

                    client.Started();

                    return client;
                }
                catch (ConnectionLostException ex)
                {
                    RemoteHostCrashInfoBar.ShowInfoBar(workspace, ex);

                    Shutdown(ex);

                    // dont crash VS because OOP is failed to start. we will show info bar telling users to restart
                    // but never physically crash VS.
                    return null;
                }
                catch (SoftCrashException ex)
                {
                    Shutdown(ex);

                    // at this point, we should have shown info bar (RemoteHostCrashInfoBar.ShowInfoBar) to users
                    // returning null here will disable OOP for this VS session. 
                    // * Note * this is not trying to recover the exception. but giving users to time
                    // to clean up before restart VS
                    return null;
                }
                catch (Exception ex)
                {
                    Shutdown(ex);
                    throw;
                }

                void Shutdown(Exception ex)
                {
                    // make sure we shutdown client if initializing client has failed.
                    client?.Shutdown();

                    // translate to our own cancellation if it is raised.
                    cancellationToken.ThrowIfCancellationRequested();

                    // otherwise, report watson
                    ex.ReportServiceHubNFW("ServiceHub creation failed");
                }
            }
        }

        private ServiceHubRemoteHostClient(
            Workspace workspace,
            TraceSource logger,
            ConnectionManager connectionManager,
            Stream stream)
            : base(workspace)
        {
            _shutdownCancellationTokenSource = new CancellationTokenSource();

            _connectionManager = connectionManager;

            _rpc = stream.CreateStreamJsonRpc(target: this, logger);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;

            _rpc.StartListening();
        }

        public override string ClientId => _connectionManager.HostGroup.Id;

        public override Task<Connection?> TryCreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
        {
            return _connectionManager.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken);
        }

        protected override void OnStarted()
        {
            RegisterGlobalOperationNotifications();
        }

        protected override void OnStopped()
        {
            // cancel all pending async work
            _shutdownCancellationTokenSource.Cancel();

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

        private async Task RpcInvokeAsync(string targetName, params object[] arguments)
        {
            // handle exception gracefully. don't crash VS due to this.
            // especially on shutdown time. because of pending async BG work such as 
            // OnGlobalOperationStarted and more, we can get into a situation where either
            // we are in the middle of call when we are disconnected, or we runs
            // after shutdown.
            try
            {
                await _rpc.InvokeWithCancellationAsync(targetName, arguments?.AsArray(), _shutdownCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex))
            {
                if (!_shutdownCancellationTokenSource.IsCancellationRequested)
                {
                    RemoteHostCrashInfoBar.ShowInfoBar(Workspace, ex);
                }
            }
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

                await RpcInvokeAsync(nameof(IRemoteHostService.OnGlobalOperationStarted), "").ConfigureAwait(false);

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

                await RpcInvokeAsync(nameof(IRemoteHostService.OnGlobalOperationStopped), e.Operations, e.Cancelled).ConfigureAwait(false);

                // Mark that we're stopped now.
                return GlobalNotificationState.NotStarted;
            }
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Stopped();
        }

        private bool ReportUnlessCanceled(Exception ex)
        {
            if (_shutdownCancellationTokenSource.IsCancellationRequested)
            {
                return true;
            }

            ex.ReportServiceHubNFW("JsonRpc invoke Failed");
            return true;
        }
    }
}
