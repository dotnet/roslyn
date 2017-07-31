﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        public class RemoteHostClientService : ForegroundThreadAffinitizedObject, IRemoteHostClientService
        {
            // OOP killed more info page link
            private const string OOPKilledMoreInfoLink = "https://go.microsoft.com/fwlink/?linkid=842308";

            /// <summary>
            /// this hold onto last remoteHostClient to make debugging easier
            /// </summary>
            private static Task<RemoteHostClient> s_lastRemoteClientTask;

            private readonly IAsynchronousOperationListener _listener;
            private readonly Workspace _workspace;
            private readonly IDiagnosticAnalyzerService _analyzerService;

            private readonly object _gate;

            private SolutionChecksumUpdater _checksumUpdater;
            private CancellationTokenSource _shutdownCancellationTokenSource;
            private Task<RemoteHostClient> _remoteClientTask;

            public RemoteHostClientService(
                IAsynchronousOperationListener listener,
                Workspace workspace,
                IDiagnosticAnalyzerService analyzerService) :
                base()
            {
                _gate = new object();

                _listener = listener;
                _workspace = workspace;
                _analyzerService = analyzerService;
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

                    // make sure we run it on background thread
                    _shutdownCancellationTokenSource = new CancellationTokenSource();

                    var token = _shutdownCancellationTokenSource.Token;

                    // create solution checksum updater
                    _checksumUpdater = new SolutionChecksumUpdater(this, token);

                    _remoteClientTask = Task.Run(() => EnableAsync(token), token);
                }
            }

            public void Disable()
            {
                RemoteHostClient client = null;

                lock (_gate)
                {
                    if (_remoteClientTask == null)
                    {
                        // already disabled
                        return;
                    }

                    var remoteClientTask = _remoteClientTask;
                    _remoteClientTask = null;

                    RemoveGlobalAssets();

                    _shutdownCancellationTokenSource.Cancel();

                    _checksumUpdater.Shutdown();
                    _checksumUpdater = null;

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

                // shut it down outside of lock so that
                // we don't call into different component while
                // holding onto a lock
                client?.Shutdown();
            }

            public Task<RemoteHostClient> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Task<RemoteHostClient> remoteClientTask;
                lock (_gate)
                {
                    remoteClientTask = _remoteClientTask;
                }

                if (remoteClientTask == null)
                {
                    // service is in shutdown mode or not enabled
                    return SpecializedTasks.Default<RemoteHostClient>();
                }

                return remoteClientTask;
            }

            private async Task<RemoteHostClient> EnableAsync(CancellationToken cancellationToken)
            {
                // if we reached here, IRemoteHostClientFactory must exist.
                // this will make VS.Next dll to be loaded
                var client = await _workspace.Services.GetRequiredService<IRemoteHostClientFactory>().CreateAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return null;
                }

                client.StatusChanged += OnStatusChanged;

                // set global assets on remote host
                var checksums = AddGlobalAssets(cancellationToken);

                // send over global asset
                await client.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService, _workspace.CurrentSolution,
                    nameof(IRemoteHostService.SynchronizeGlobalAssetsAsync),
                    (object)checksums, cancellationToken).ConfigureAwait(false);

                return client;
            }

            private Checksum[] AddGlobalAssets(CancellationToken cancellationToken)
            {
                var builder = ArrayBuilder<Checksum>.GetInstance();

                using (Logger.LogBlock(FunctionId.RemoteHostClientService_AddGlobalAssetsAsync, cancellationToken))
                {
                    var snapshotService = _workspace.Services.GetService<IRemotableDataService>();
                    var assetBuilder = new CustomAssetBuilder(_workspace);

                    foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                    {
                        var asset = assetBuilder.Build(reference, cancellationToken);

                        builder.Add(asset.Checksum);
                        snapshotService.AddGlobalAsset(reference, asset, cancellationToken);
                    }
                }

                return builder.ToArrayAndFree();
            }

            private void RemoveGlobalAssets()
            {
                using (Logger.LogBlock(FunctionId.RemoteHostClientService_RemoveGlobalAssets, CancellationToken.None))
                {
                    var snapshotService = _workspace.Services.GetService<IRemotableDataService>();

                    foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                    {
                        snapshotService.RemoveGlobalAsset(reference, CancellationToken.None);
                    }
                }
            }

            private void OnStatusChanged(object sender, bool started)
            {
                if (started)
                {
                    return;
                }

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
                        _remoteClientTask = Task.FromResult<RemoteHostClient>(new RemoteHostClient.NoOpClient(_workspace));
                    }

                    // s_lastRemoteClientTask info should be saved in the dump
                    // report NFW when connection is closed unless it is proper shutdown
                    FatalError.ReportWithoutCrash(new Exception("Connection to remote host closed"));

                    // use info bar to show warning to users
                    var infoBarUIs = new List<InfoBarUI>();

                    infoBarUIs.Add(
                        new InfoBarUI(ServicesVSResources.Learn_more, InfoBarUI.UIKind.HyperLink, () =>
                            BrowserHelper.StartBrowser(new Uri(OOPKilledMoreInfoLink)), closeAfterAction: false));

                    var allowRestarting = _workspace.Options.GetOption(RemoteHostOptions.RestartRemoteHostAllowed);
                    if (allowRestarting)
                    {
                        // this is hidden restart option. by default, user can't restart remote host that got killed
                        // by users
                        infoBarUIs.Add(
                            new InfoBarUI("Restart external process", InfoBarUI.UIKind.Button, () =>
                            {
                                // start off new remote host
                                var unused = RequestNewRemoteHostAsync(CancellationToken.None);
                            }, closeAfterAction: true));
                    }

                    _workspace.Services.GetService<IErrorReportingService>().ShowGlobalErrorInfo(
                        ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio,
                        infoBarUIs.ToArray());
                }
            }

            public async Task RequestNewRemoteHostAsync(CancellationToken cancellationToken)
            {
                var existingClient = await TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (existingClient == null)
                {
                    return;
                }

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
                existingClient.Shutdown();
            }
        }
    }
}
