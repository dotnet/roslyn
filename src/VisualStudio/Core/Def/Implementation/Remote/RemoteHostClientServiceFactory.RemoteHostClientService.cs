// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        public class RemoteHostClientService : IRemoteHostClientService
        {
            private readonly Workspace _workspace;
            private readonly IDiagnosticAnalyzerService _analyzerService;

            private readonly object _gate;

            private CancellationTokenSource _shutdownCancellationTokenSource;
            private Task<RemoteHostClient> _instanceTask;

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
                    if (_instanceTask != null)
                    {
                        // already enabled
                        return;
                    }

                    if (!_workspace.Options.GetOption(RemoteHostOptions.RemoteHost))
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
                    _instanceTask = Task.Run(() => EnableAsync(token), token);
                }
            }

            public void Disable()
            {
                lock (_gate)
                {
                    if (_instanceTask == null)
                    {
                        // already disabled
                        return;
                    }

                    var instance = _instanceTask;
                    _instanceTask = null;

                    RemoveGlobalAssets();

                    _shutdownCancellationTokenSource.Cancel();

                    try
                    {
                        instance.Wait(_shutdownCancellationTokenSource.Token);

                        instance.Result.Shutdown();
                    }
                    catch (OperationCanceledException)
                    {
                        // _instance wasn't finished running yet.
                    }
                }
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
                await AddGlobalAssetsAsync(cancellationToken).ConfigureAwait(false);

                // if we reached here, IRemoteHostClientFactory must exist.
                // this will make VS.Next dll to be loaded
                var instance = await _workspace.Services.GetRequiredService<IRemoteHostClientFactory>().CreateAsync(_workspace, cancellationToken).ConfigureAwait(false);
                instance.ConnectionChanged += OnConnectionChanged;

                return instance;
            }

            private async Task AddGlobalAssetsAsync(CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.RemoteHostClientService_AddGlobalAssetsAsync, cancellationToken))
                {
                    var snapshotService = _workspace.Services.GetService<ISolutionChecksumService>();
                    var assetBuilder = new AssetBuilder(_workspace.CurrentSolution);

                    foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                    {
                        var asset = await assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                        snapshotService.AddGlobalAsset(reference, asset, cancellationToken);
                    }
                }
            }

            private void RemoveGlobalAssets()
            {
                using (Logger.LogBlock(FunctionId.RemoteHostClientService_RemoveGlobalAssets, CancellationToken.None))
                {
                    var snapshotService = _workspace.Services.GetService<ISolutionChecksumService>();

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

                lock (_gate)
                {
                    if (_shutdownCancellationTokenSource.IsCancellationRequested)
                    {
                        // we are shutting down
                        return;
                    }
                }

                // crash right away when connection is closed
                FatalError.Report(new Exception("Connection to remote host closed"));
            }
        }
    }
}
