// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        private class SolutionChecksumUpdater : GlobalOperationAwareIdleProcessor
        {
            private readonly RemoteHostClientService _service;
            private readonly SemaphoreSlim _event;

            // hold onto last snapshot
            private CancellationTokenSource _globalOperationCancellationSource;
            private bool _synchronize;

            public SolutionChecksumUpdater(RemoteHostClientService service, CancellationToken shutdownToken) :
                base(AggregateAsynchronousOperationListener.CreateEmptyListener(),
                     service.Workspace.Services.GetService<IGlobalOperationNotificationService>(),
                     service.Workspace.Options.GetOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS), shutdownToken)
            {
                _service = service;

                _event = new SemaphoreSlim(initialCount: 0);

                // start listening workspace change event
                _service.Workspace.WorkspaceChanged += OnWorkspaceChanged;

                // create its own cancellation token source
                _globalOperationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);

                Start();
            }

            private CancellationToken ShutdownCancellationToken => CancellationToken;

            protected override async Task ExecuteAsync()
            {
                // wait for global operation to finish
                await GlobalOperationTask.ConfigureAwait(false);

                // cancel updating solution checksum if a global operation (such as loading solution, building solution and etc) has started
                await UpdateSolutionChecksumAsync(_globalOperationCancellationSource.Token).ConfigureAwait(false);

                // check whether we had bulk change that require asset synchronization
                if (_synchronize)
                {
                    await SynchronizeAssets().ConfigureAwait(false);
                }
            }

            protected override void PauseOnGlobalOperation()
            {
                var previousCancellationSource = _globalOperationCancellationSource;

                // create new cancellation token source linked with given shutdown cancellation token
                _globalOperationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(ShutdownCancellationToken);

                CancelAndDispose(previousCancellationSource);
            }

            protected override Task WaitAsync(CancellationToken cancellationToken)
            {
                return _event.WaitAsync(cancellationToken);
            }

            public override void Shutdown()
            {
                base.Shutdown();

                // stop listening workspace change event
                _service.Workspace.WorkspaceChanged -= OnWorkspaceChanged;

                CancelAndDispose(_globalOperationCancellationSource);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                // special bulk update case
                if (e.Kind == WorkspaceChangeKind.SolutionAdded ||
                    e.Kind == WorkspaceChangeKind.ProjectAdded)
                {
                    _synchronize = true;
                    EnqueueChecksumUpdate();
                    return;
                }

                // record that we are busy
                UpdateLastAccessTime();

                EnqueueChecksumUpdate();
            }

            private void EnqueueChecksumUpdate()
            {
                // event will raised sequencially. no concurrency on this handler
                if (_event.CurrentCount > 0)
                {
                    return;
                }

                _event.Release();
            }

            private async Task UpdateSolutionChecksumAsync(CancellationToken cancellationToken)
            {
                await _service.Workspace.CurrentSolution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            }

            private async Task SynchronizeAssets()
            {
                _synchronize = false;

                var remoteHostClient = await _service.GetRemoteHostClientAsync(ShutdownCancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    return;
                }

                using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizeAssets, ShutdownCancellationToken))
                {
                    var solution = _service.Workspace.CurrentSolution;
                    using (var session = await remoteHostClient.CreateServiceSessionAsync(WellKnownRemoteHostServices.RemoteHostService, solution, ShutdownCancellationToken).ConfigureAwait(false))
                    {
                        // ask remote host to sync initial asset
                        await session.InvokeAsync(WellKnownRemoteHostServices.RemoteHostService_SynchronizeAsync).ConfigureAwait(false);
                    }
                }
            }

            private static void CancelAndDispose(CancellationTokenSource cancellationSource)
            {
                // cancel running tasks
                cancellationSource.Cancel();

                // dispose cancellation token source
                cancellationSource.Dispose();
            }
        }
    }
}
