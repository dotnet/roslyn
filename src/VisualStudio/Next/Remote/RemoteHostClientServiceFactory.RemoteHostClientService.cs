// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        private class RemoteHostClientService : IRemoteHostClientService
        {
            private readonly Workspace _workspace;
            private readonly IDiagnosticAnalyzerService _analyzerService;

            private readonly object _gate;

            private CancellationTokenSource _shutdown;
            private Task<RemoteHostClient> _instance;

            public RemoteHostClientService(Workspace workspace, IDiagnosticAnalyzerService analyzerService)
            {
                _gate = new object();

                _workspace = workspace;
                _analyzerService = analyzerService;
            }

            public void Enable()
            {
                lock (_gate)
                {
                    if (_instance != null)
                    {
                        // already enabled
                        return;
                    }

                    if (!_workspace.Options.GetOption(RemoteHostOptions.RemoteHost))
                    {
                        // not turned on
                        return;
                    }

                    // make sure we run it on background thread
                    _shutdown = new CancellationTokenSource();

                    var token = _shutdown.Token;
                    _instance = Task.Run(() => EnableAsync(token), token);
                }
            }

            public void Disable()
            {
                lock (_gate)
                {
                    if (_instance == null)
                    {
                        // already disabled
                        return;
                    }

                    var instance = _instance;
                    _instance = null;

                    RemoveGlobalAssets();

                    _shutdown.Cancel();

                    try
                    {
                        instance.Result.Shutdown();
                    }
                    catch (OperationCanceledException)
                    {
                        // _instance wasn't finished running yet.
                    }
                }
            }

            public async Task<RemoteHostClient> GetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                // copy instance to local variable so that we don't need lock here
                var instance = _instance;
                if (instance == null)
                {
                    // service is in shutdown mode or not enabled
                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // make sure instance was started
                return await instance.ConfigureAwait(false);
            }

            private async Task<RemoteHostClient> EnableAsync(CancellationToken cancellationToken)
            {
                await AddGlobalAssetsAsync(cancellationToken).ConfigureAwait(false);

                return await StartInternalAsync(cancellationToken).ConfigureAwait(false);
            }

            private async Task<RemoteHostClient> StartInternalAsync(CancellationToken cancellationToken)
            {
                // TODO: abstract this out so that we can have more host than service hub
                var instance = await ServiceHubRemoteHostClient.CreateAsync(_workspace, cancellationToken).ConfigureAwait(false);
                instance.ConnectionChanged += OnConnectionChanged;

                return instance;
            }

            private async Task AddGlobalAssetsAsync(CancellationToken cancellationToken)
            {
                var snapshotService = _workspace.Services.GetService<ISolutionChecksumService>();
                var assetBuilder = new AssetBuilder(_workspace.CurrentSolution);

                foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                {
                    var asset = await assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                    snapshotService.AddGlobalAsset(reference, asset, cancellationToken);
                }
            }

            private void RemoveGlobalAssets()
            {
                var snapshotService = _workspace.Services.GetService<ISolutionChecksumService>();

                foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                {
                    snapshotService.RemoveGlobalAsset(reference, CancellationToken.None);
                }
            }

            // use local token and lock on instance
            private void OnConnectionChanged(object sender, bool connection)
            {
                if (connection)
                {
                    return;
                }

                // if remote host gets disconnected. tell users that remote host is gone and whether they want to recover remote host.
                lock (_gate)
                {
                    if (_shutdown.IsCancellationRequested)
                    {
                        // we are shutting down.
                        return;
                    }

                    _instance = null;
                }

                _workspace.Services.GetService<IErrorReportingService>().ShowErrorInfo(
                    ServicesVSResources.Connection_to_remote_host_has_been_lost_some_features_might_stop_working_or_start_working_in_proc_do_you_want_to_recover_remote_host,
                    new ErrorReportingUI(ServicesVSResources.Re_enable, ErrorReportingUI.UIKind.Button, () =>
                    {
                        lock (_gate)
                        {
                            if (_shutdown.IsCancellationRequested)
                            {
                                // we are shutting down
                                return;
                            }

                            _instance = StartInternalAsync(_shutdown.Token);
                        }
                    }));
            }
        }
    }
}
