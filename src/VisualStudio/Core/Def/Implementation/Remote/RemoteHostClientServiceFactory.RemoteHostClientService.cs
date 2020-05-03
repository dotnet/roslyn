// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        public sealed class RemoteHostClientService : ForegroundThreadAffinitizedObject, IRemoteHostClientService
        {
            /// <summary>
            /// this hold onto last remoteHostClient to make debugging easier
            /// </summary>
            private static Task<RemoteHostClient?>? s_lastRemoteClientTask;

            private readonly IAsynchronousOperationListener _listener;
            private readonly Workspace _workspace;

            private GlobalNotificationRemoteDeliveryService? _globalNotificationDelivery;

            private readonly object _gate;

            private SolutionChecksumUpdater? _checksumUpdater;
            private CancellationTokenSource? _shutdownCancellationTokenSource;
            private Task<RemoteHostClient?>? _remoteClientTask;

            public RemoteHostClientService(
                IThreadingContext threadingContext,
                IAsynchronousOperationListener listener,
                Workspace workspace)
                : base(threadingContext)
            {
                _gate = new object();

                _listener = listener;
                _workspace = workspace;
            }

            public Workspace Workspace => _workspace;
            public IAsynchronousOperationListener Listener => _listener;

            public void Enable()
            {
                lock (_gate)
                {
                    if (_remoteClientTask != null)
                    {
                        // already enabled
                        return;
                    }

                    // We enable the remote host if either RemoteHostTest or RemoteHost are on.
                    if (!_workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest) &&
                        !_workspace.Options.GetOption(RemoteHostOptions.RemoteHost))
                    {
                        // not turned on
                        return;
                    }

                    // log that remote host is enabled
                    Logger.Log(FunctionId.RemoteHostClientService_Enabled, KeyValueLogMessage.NoProperty);

                    var remoteHostClientFactory = _workspace.Services.GetService<IRemoteHostClientFactory>();
                    if (remoteHostClientFactory == null)
                    {
                        // dev14 doesn't have remote host client factory
                        return;
                    }

                    // set bitness
                    SetRemoteHostBitness();

                    // make sure we run it on background thread
                    _shutdownCancellationTokenSource = new CancellationTokenSource();

                    var token = _shutdownCancellationTokenSource.Token;

                    _checksumUpdater = new SolutionChecksumUpdater(Workspace, Listener, token);
                    _globalNotificationDelivery = new GlobalNotificationRemoteDeliveryService(Workspace.Services, token);

                    _remoteClientTask = Task.Run(() => EnableAsync(token), token);
                }
            }

            public void Disable()
            {
                RemoteHostClient? client = null;

                lock (_gate)
                {
                    var remoteClientTask = _remoteClientTask;
                    if (remoteClientTask == null)
                    {
                        // already disabled
                        return;
                    }

                    _remoteClientTask = null;

                    Contract.ThrowIfNull(_shutdownCancellationTokenSource);
                    Contract.ThrowIfNull(_checksumUpdater);
                    Contract.ThrowIfNull(_globalNotificationDelivery);

                    _shutdownCancellationTokenSource.Cancel();

                    _checksumUpdater.Shutdown();
                    _checksumUpdater = null;

                    _globalNotificationDelivery.Dispose();

                    try
                    {
                        remoteClientTask.Wait(_shutdownCancellationTokenSource.Token);

                        // result can be null if service hub failed to launch
                        client = remoteClientTask.Result;
                    }
                    catch (OperationCanceledException)
                    {
                        // remoteClientTask wasn't finished running yet.
                    }
                }

                if (client != null)
                {
                    client.StatusChanged -= OnStatusChanged;

                    // shut it down outside of lock so that
                    // we don't call into different component while
                    // holding onto a lock
                    client.Dispose();
                }
            }

            bool IRemoteHostClientService.IsEnabled()
            {
                // We enable the remote host if either RemoteHostTest or RemoteHost are on.
                if (!_workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest)
                    && !_workspace.Options.GetOption(RemoteHostOptions.RemoteHost))
                {
                    // not turned on
                    return false;
                }

                var remoteHostClientFactory = _workspace.Services.GetService<IRemoteHostClientFactory>();
                if (remoteHostClientFactory is null)
                {
                    // not available
                    return false;
                }

                return true;
            }

            public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Task<RemoteHostClient?>? remoteClientTask;
                lock (_gate)
                {
                    remoteClientTask = _remoteClientTask;
                }

                if (remoteClientTask == null)
                {
                    // service is in shutdown mode or not enabled
                    return SpecializedTasks.Null<RemoteHostClient>();
                }

                return remoteClientTask;
            }

            private void SetRemoteHostBitness()
            {
                bool x64 = RemoteHostOptions.IsServiceHubProcess64Bit(_workspace);

                // log OOP bitness
                Logger.Log(FunctionId.RemoteHost_Bitness, KeyValueLogMessage.Create(LogType.Trace, m => m["64bit"] = x64));

                // set service bitness
                WellKnownServiceHubServices.Set64bit(x64);
            }

            private async Task<RemoteHostClient?> EnableAsync(CancellationToken cancellationToken)
            {
                // if we reached here, IRemoteHostClientFactory must exist.
                // this will make VS.Next dll to be loaded
                var client = await _workspace.Services.GetRequiredService<IRemoteHostClientFactory>().CreateAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    client.StatusChanged += OnStatusChanged;
                    return client;
                }

                return null;
            }

            private void OnStatusChanged(object sender, bool started)
            {
                if (started)
                {
                    return;
                }

                Contract.ThrowIfNull(_shutdownCancellationTokenSource);

                if (_shutdownCancellationTokenSource.IsCancellationRequested)
                {
                    lock (_gate)
                    {
                        // RemoteHost has been disabled
                        _remoteClientTask = null;
                    }
                }
                else
                {
                    lock (_gate)
                    {
                        // save last remoteHostClient
                        s_lastRemoteClientTask = _remoteClientTask;

                        // save NoOpRemoteHostClient to remoteClient so that all RemoteHost call becomes
                        // No Op. this basically have same effect as disabling all RemoteHost features
                        _remoteClientTask = Task.FromResult<RemoteHostClient?>(new RemoteHostClient.NoOpClient(_workspace));
                    }

                    // s_lastRemoteClientTask info should be saved in the dump
                    // report NFW when connection is closed unless it is proper shutdown
                    FatalError.ReportWithoutCrash(new InvalidOperationException("Connection to remote host closed"));

                    RemoteHostCrashInfoBar.ShowInfoBar(_workspace);
                }
            }

            public async Task RequestNewRemoteHostAsync(CancellationToken cancellationToken)
            {
                var existingClient = await TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (existingClient == null)
                {
                    return;
                }

                Contract.ThrowIfNull(_shutdownCancellationTokenSource);

                // log that remote host is restarted
                Logger.Log(FunctionId.RemoteHostClientService_Restarted, KeyValueLogMessage.NoProperty);

                // we are going to kill the existing remote host, connection change is expected
                existingClient.StatusChanged -= OnStatusChanged;

                lock (_gate)
                {
                    // create new remote host client
                    var token = _shutdownCancellationTokenSource.Token;
                    _remoteClientTask = Task.Run(() => EnableAsync(token), token);
                }

                // shutdown 
                existingClient.Dispose();
            }
        }
    }
}
