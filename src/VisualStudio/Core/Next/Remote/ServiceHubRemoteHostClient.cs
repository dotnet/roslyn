// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Extensions;
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
        private static int s_instanceId = 0;

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
                var timeout = TimeSpan.FromMilliseconds(workspace.Options.GetOption(RemoteHostOptions.RequestServiceTimeoutInMS));

                // Retry (with timeout) until we can connect to RemoteHost (service hub process). 
                // we are seeing cases where we failed to connect to service hub process when a machine is under heavy load.
                // (see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/481103 as one of example)
                var instance = await RetryRemoteCallAsync<IOException, ServiceHubRemoteHostClient>(
                    () => CreateWorkerAsync(workspace, primary, timeout, cancellationToken), timeout, cancellationToken).ConfigureAwait(false);

                instance.Connected();

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
                var remoteHostStream = await RequestServiceAsync(
                    primary, WellKnownRemoteHostServices.RemoteHostService, hostGroup, timeout, cancellationToken).ConfigureAwait(false);

                client = new ServiceHubRemoteHostClient(workspace, primary, hostGroup, remoteHostStream);

                await client._rpc.InvokeWithCancellationAsync<string>(
                    nameof(IRemoteHostService.Connect),
                    new object[] { current, TelemetryService.DefaultSession.SerializeSettings() },
                    cancellationToken).ConfigureAwait(false);

                return client;
            }
            catch (Exception ex)
            {
                // make sure we shutdown client if initializing client has failed.
                client?.Shutdown();

                // translate to our own cancellation if it is raised.
                cancellationToken.ThrowIfCancellationRequested();

                // otherwise, report watson and throw original exception
                WatsonReporter.Report("ServiceHub creation failed", ex, ReportDetailInfo);
                throw;
            }
        }

        private static async Task RegisterWorkspaceHostAsync(Workspace workspace, RemoteHostClient client)
        {
            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return;
            }

            // RegisterWorkspaceHost is required to be called from UI thread so push the code
            // to UI thread to run. 
            await Task.Factory.SafeStartNew(() =>
            {
                var projectTracker = vsWorkspace.GetProjectTrackerAndInitializeIfNecessary(Shell.ServiceProvider.GlobalProvider);

                var host = new WorkspaceHost(vsWorkspace, client);

                projectTracker.RegisterWorkspaceHost(host);
                projectTracker.StartSendingEventsToWorkspaceHost(host);
            }, CancellationToken.None, ForegroundThreadAffinitizedObject.CurrentForegroundThreadData.TaskScheduler).ConfigureAwait(false);
        }

        private ServiceHubRemoteHostClient(
            Workspace workspace, HubClient hubClient, HostGroup hostGroup, Stream stream) :
                base(workspace)
        {
            _hubClient = hubClient;
            _hostGroup = hostGroup;
            _timeout = TimeSpan.FromMilliseconds(workspace.Options.GetOption(RemoteHostOptions.RequestServiceTimeoutInMS));

            _rpc = new JsonRpc(new JsonRpcMessageHandler(stream, stream), target: this);
            _rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;

            _rpc.StartListening();
        }

        protected override async Task<Session> TryCreateServiceSessionAsync(string serviceName, Optional<Func<CancellationToken, Task<PinnedRemotableDataScope>>> getSnapshotAsync, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host for solution related information
            var snapshotStream = getSnapshotAsync.Value == null ? null : await RequestServiceAsync(_hubClient, WellKnownServiceHubServices.SnapshotService, _hostGroup, _timeout, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await RequestServiceAsync(_hubClient, serviceName, _hostGroup, _timeout, cancellationToken).ConfigureAwait(false);

            return await JsonRpcSession.CreateAsync(getSnapshotAsync, callbackTarget, serviceStream, snapshotStream, cancellationToken).ConfigureAwait(false);
        }

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
            // we are asked to disconnect. unsubscribe and dispose to disconnect.
            // there are 2 ways to get disconnected. one is Roslyn decided to disconnect with RemoteHost (ex, cancellation or recycle OOP) and
            // the other is external thing disconnecting remote host from us (ex, user killing OOP process).
            // the Disconnected event we subscribe is to detect #2 case. and this method is for #1 case. so when we are willingly disconnecting
            // we don't need the event, otherwise, Disconnected event will be called twice.
            _rpc.Disconnected -= OnRpcDisconnected;
            _rpc.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected();
        }

        /// <summary>
        /// call <paramref name="funcAsync"/> and retry up to <paramref name="timeout"/> if the call throws
        /// <typeparamref name="TException"/>. any other exception from the call won't be handled here.
        /// </summary>
        private static async Task<TResult> RetryRemoteCallAsync<TException, TResult>(
            Func<Task<TResult>> funcAsync,
            TimeSpan timeout,
            CancellationToken cancellationToken) where TException : Exception
        {
            const int retry_delayInMS = 50;

            using (var pooledStopwatch = SharedPools.Default<Stopwatch>().GetPooledObject())
            {
                var watch = pooledStopwatch.Object;
                watch.Start();

                while (watch.Elapsed < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        return await funcAsync().ConfigureAwait(false);
                    }
                    catch (TException)
                    {
                        // throw cancellation token if operation is cancelled
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    // wait for retry_delayInMS before next try
                    await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);

                    ReportTimeout(watch);
                }
            }

            // operation timed out, more than we are willing to wait
            ShowInfoBar();

            // user didn't ask for cancellation, but we can't fullfill this request. so we
            // create our own cancellation token and then throw it. this doesn't guarantee
            // 100% that we won't crash, but this is at least safest way we know until user
            // restart VS (with info bar)
            using (var ownCancellationSource = new CancellationTokenSource())
            {
                ownCancellationSource.Cancel();
                ownCancellationSource.Token.ThrowIfCancellationRequested();
            }

            throw ExceptionUtilities.Unreachable;
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
                    // we are wrapping HubClient.RequestServiceAsync since we can't control its internal timeout value ourselves.
                    // we have bug opened to track the issue.
                    // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/Editor/_workitems?id=378757&fullScreen=false&_a=edit

                    // retry on cancellation token since HubClient will throw its own cancellation token
                    // when it couldn't connect to service hub service for some reasons
                    // (ex, OOP process GC blocked and not responding to request)
                    return await RetryRemoteCallAsync<OperationCanceledException, Stream>(
                        () => client.RequestServiceAsync(descriptor, cancellationToken),
                        timeout,
                        cancellationToken).ConfigureAwait(false);
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

        #region code related to make diagnosis easier later
        private static int ReportDetailInfo(IFaultUtility faultUtility)
        {
            // 0 means send watson, otherwise, cancel watson
            // we always send watson since dump itself can have valuable data
            var exitCode = 0;

            try
            {
                // add service hub process.
                // we will record dumps for all service hub processes
                foreach (var p in Process.GetProcessesByName("ServiceHub.RoslynCodeAnalysisService32"))
                {
                    // include all remote host processes
                    faultUtility.AddProcessDump(p.Id);
                }

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

        private static readonly TimeSpan s_reportTimeout = TimeSpan.FromMinutes(10);
        private static bool s_timeoutReported = false;

        private static void ReportTimeout(Stopwatch watch)
        {
            // if we tried for 10 min and still couldn't connect. NFW (non fatal watson) some data
            if (!s_timeoutReported && watch.Elapsed > s_reportTimeout)
            {
                s_timeoutReported = true;

                // report service hub logs along with dump
                WatsonReporter.Report("RequestServiceAsync Timeout", new Exception("RequestServiceAsync Timeout"), ReportDetailInfo);
            }
        }

        private static bool s_infoBarReported = false;

        private static void ShowInfoBar()
        {
            // use info bar to show warning to users
            if (CodeAnalysis.PrimaryWorkspace.Workspace != null && !s_infoBarReported)
            {
                // do not report it multiple times
                s_infoBarReported = true;

                // use info bar to show warning to users
                CodeAnalysis.PrimaryWorkspace.Workspace.Services.GetService<IErrorReportingService>()?.ShowGlobalErrorInfo(
                    ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio);
            }
        }
        #endregion
    }
}
