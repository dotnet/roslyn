// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        public class RemoteHostClientService : ForegroundThreadAffinitizedObject, IRemoteHostClientService
        {
            private readonly Workspace _workspace;
            private readonly IDiagnosticAnalyzerService _analyzerService;
            private readonly IEditorOptions _globalEditorOptions;

            private readonly object _gate;

            private SolutionChecksumUpdater _checksumUpdater;
            private CancellationTokenSource _shutdownCancellationTokenSource;
            private Task<RemoteHostClient> _instanceTask;

            public RemoteHostClientService(
                Workspace workspace,
                IDiagnosticAnalyzerService analyzerService,
                IEditorOptions globalEditorOptions) :
                base()
            {
                _gate = new object();

                _workspace = workspace;
                _analyzerService = analyzerService;
                _globalEditorOptions = globalEditorOptions;
            }

            public Workspace Workspace => _workspace;

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

                    // create solution checksum updater
                    _checksumUpdater = new SolutionChecksumUpdater(this, token);

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
                        instanceTask.Result?.Shutdown();
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

                // ensure we have solution checksum
                _checksumUpdater.EnsureSolutionChecksum(cancellationToken);

                return instanceTask;
            }

            private async Task<RemoteHostClient> EnableAsync(CancellationToken cancellationToken)
            {
                AddGlobalAssets(cancellationToken);

                // if we reached here, IRemoteHostClientFactory must exist.
                // this will make VS.Next dll to be loaded
                var instance = await _workspace.Services.GetRequiredService<IRemoteHostClientFactory>().CreateAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (instance == null)
                {
                    return null;
                }

                instance.ConnectionChanged += OnConnectionChanged;

                return instance;
            }

            private void AddGlobalAssets(CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.RemoteHostClientService_AddGlobalAssetsAsync, cancellationToken))
                {
                    var snapshotService = _workspace.Services.GetService<ISolutionChecksumService>();
                    var assetBuilder = new AssetBuilder(_workspace.CurrentSolution);

                    foreach (var reference in _analyzerService.GetHostAnalyzerReferences())
                    {
                        var asset = assetBuilder.Build(reference, cancellationToken);
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

            private bool DynamicAnalysisEnabled()
            {
                const string dynamicAnalysisPackageGuid = "3EADAB3E-2035-4513-8C13-FBA84414A16C";

                // TODO: think about how to get rid of this code. since we don't want any code that require foreground
                //       thread to run
                AssertIsForeground();

                var guid = new Guid(dynamicAnalysisPackageGuid);
                var shell = (IVsShell)Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsShell));
                if (shell == null)
                {
                    return false;
                }

                int installed;
                if (ErrorHandler.Failed(shell.IsPackageInstalled(ref guid, out installed)))
                {
                    return false;
                }

                return installed != 0;
            }

            private bool CodeLenEnabled()
            {
#if false
                return _globalEditorOptions.GetOptionValue(CodeLensOptions.IsCodeLensEnabledOptionKey);
#endif
                return false;
            }
        }
    }
}
