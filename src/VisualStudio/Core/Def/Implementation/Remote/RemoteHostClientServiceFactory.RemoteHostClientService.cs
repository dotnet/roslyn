// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private static Task<RemoteHostClient> s_lastInstanceTask;

            private readonly IAsynchronousOperationListener _listener;
            private readonly Workspace _workspace;
            private readonly IDiagnosticAnalyzerService _analyzerService;

            private readonly object _gate;

            private SolutionChecksumUpdater _checksumUpdater;
            private CancellationTokenSource _shutdownCancellationTokenSource;
            private Task<RemoteHostClient> _instanceTask;

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
                    if (_instanceTask != null)
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

                    _instanceTask = Task.Run(() => EnableAsync(token), token);
                }
            }

            public void Disable()
            {
                RemoteHostClient client = null;

                lock (_gate)
                {
                    if (_instanceTask == null)
                    {
                        // already disabled
                        return;
                    }

                    var instanceTask = _instanceTask;
                    _instanceTask = null;

                    RemoveGlobalAssets();

                    _shutdownCancellationTokenSource.Cancel();

                    _checksumUpdater.Shutdown();
                    _checksumUpdater = null;

                    try
                    {
                        instanceTask.Wait(_shutdownCancellationTokenSource.Token);

                        // result can be null if service hub failed to launch
                        client = instanceTask.Result;
                    }
                    catch (OperationCanceledException)
                    {
                        // _instance wasn't finished running yet.
                    }
                }

                // shut it down outside of lock so that
                // we don't call into different component while
                // holding onto a lock
                client?.Shutdown();
            }

            public Task<RemoteHostClient> GetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Task<RemoteHostClient> instanceTask;
                lock (_gate)
                {
                    instanceTask = _instanceTask;
                }

                if (instanceTask == null)
                {
                    // service is in shutdown mode or not enabled
                    return SpecializedTasks.Default<RemoteHostClient>();
                }

                return instanceTask;
            }

            private async Task<RemoteHostClient> EnableAsync(CancellationToken cancellationToken)
            {
                // if we reached here, IRemoteHostClientFactory must exist.
                // this will make VS.Next dll to be loaded
                var instance = await _workspace.Services.GetRequiredService<IRemoteHostClientFactory>().CreateAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (instance == null)
                {
                    return null;
                }

                instance.ConnectionChanged += OnConnectionChanged;

                // set global assets on remote host
                var checksums = AddGlobalAssets(cancellationToken);

                // send over global asset
                await instance.RunOnRemoteHostAsync(
                    WellKnownRemoteHostServices.RemoteHostService, _workspace.CurrentSolution,
                    nameof(IRemoteHostService.SynchronizeGlobalAssetsAsync),
                    (object)checksums, cancellationToken).ConfigureAwait(false);

                return instance;
            }

            private Checksum[] AddGlobalAssets(CancellationToken cancellationToken)
            {
                var builder = ArrayBuilder<Checksum>.GetInstance();

                using (Logger.LogBlock(FunctionId.RemoteHostClientService_AddGlobalAssetsAsync, cancellationToken))
                {
                    var snapshotService = _workspace.Services.GetService<ISolutionSynchronizationService>();
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
                    var snapshotService = _workspace.Services.GetService<ISolutionSynchronizationService>();

                    foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                    {
                        snapshotService.RemoveGlobalAsset(reference, CancellationToken.None);
                    }
                }
            }

            // use local token and lock on instance
            private void OnConnectionChanged(object sender, bool connected)
            {
                if (connected)
                {
                    return;
                }

                if (_shutdownCancellationTokenSource.IsCancellationRequested)
                {
                    lock (_gate)
                    {
                        // RemoteHost has been disabled
                        _instanceTask = null;
                    }
                }
                else
                {
                    lock (_gate)
                    {
                        // save last remoteHostClient
                        s_lastInstanceTask = _instanceTask;

                        // save NoOpRemoteHostClient to instance so that all RemoteHost call becomes
                        // No Op. this basically have same effect as disabling all RemoteHost features
                        _instanceTask = Task.FromResult<RemoteHostClient>(new RemoteHostClient.NoOpClient(_workspace));
                    }

                    // s_lastInstanceTask info should be saved in the dump
                    // report NFW when connection is closed unless it is proper shutdown
                    FatalError.ReportWithoutCrash(new Exception("Connection to remote host closed"));

                    // use info bar to show warning to users
                    var infoBarUIs = new List<ErrorReportingUI>();

                    infoBarUIs.Add(
                        new ErrorReportingUI(ServicesVSResources.Learn_more, ErrorReportingUI.UIKind.HyperLink, () =>
                            BrowserHelper.StartBrowser(new Uri(OOPKilledMoreInfoLink)), closeAfterAction: false));

                    var allowRestarting = _workspace.Options.GetOption(RemoteHostOptions.RestartRemoteHostAllowed);
                    if (allowRestarting)
                    {
                        infoBarUIs.Add(
                            new ErrorReportingUI("Restart OOP", ErrorReportingUI.UIKind.Button, async () =>
                            await RequestNewRemoteHostAsync(CancellationToken.None).ConfigureAwait(false), closeAfterAction: true));
                    }

                    _workspace.Services.GetService<IErrorReportingService>().ShowGlobalErrorInfo(
                        ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio,
                        infoBarUIs.ToArray());
                }
            }

            public async Task RequestNewRemoteHostAsync(CancellationToken cancellationToken)
            {
                var instance = await GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (instance == null)
                {
                    return;
                }


                // log that remote host is restarted
                Logger.Log(FunctionId.RemoteHostClientService_Restarted, KeyValueLogMessage.NoProperty);

                // we are going to kill the existing remote host, connection change is expected
                instance.ConnectionChanged -= OnConnectionChanged;

                lock (_gate)
                {
                    var token = _shutdownCancellationTokenSource.Token;
                    _instanceTask = Task.Run(() => EnableAsync(token), token);
                }

                // shutdown 
                instance.Shutdown();
            }
        }
    }
}