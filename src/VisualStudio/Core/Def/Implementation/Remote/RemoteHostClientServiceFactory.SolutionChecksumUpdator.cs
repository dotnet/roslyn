// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory
    {
        private class SolutionChecksumUpdator : GlobalOperationAwareIdleProcessor
        {
            private readonly Workspace _workspace;
            private readonly ISolutionChecksumService _checksumService;
            private readonly SemaphoreSlim _event;

            // hold onto last snapshot
            private CancellationTokenSource _globalOperationCancellationSource;
            private ChecksumScope _lastSnapshot;

            public SolutionChecksumUpdator(
                Workspace workspace,
                CancellationToken shutdownToken) :
                base(AggregateAsynchronousOperationListener.CreateEmptyListener(),
                     workspace.Services.GetService<IGlobalOperationNotificationService>(),
                     workspace.Options.GetOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS), shutdownToken)
            {
                _workspace = workspace;
                _checksumService = _workspace.Services.GetService<ISolutionChecksumService>();
                _event = new SemaphoreSlim(initialCount: 0);

                // start listening workspace change event
                _workspace.WorkspaceChanged += OnWorkspaceChanged;

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

            public async void EnsureSolutionChecksum(CancellationToken cancellationToken)
            {
                if (_lastSnapshot != null)
                {
                    // we already have one. pass
                    return;
                }

                try
                {
                    // update solution checksum
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_globalOperationCancellationSource.Token, cancellationToken))
                    {
                        await UpdateSolutionChecksumAsync(linked.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore cancellation
                }
            }

            public override void Shutdown()
            {
                base.Shutdown();

                // stop listening workspace change event
                _workspace.WorkspaceChanged -= OnWorkspaceChanged;

                CancelAndDispose(_globalOperationCancellationSource);

                // release last snapshot
                _lastSnapshot?.Dispose();
                _lastSnapshot = null;
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                // special initial case
                if (e.Kind == WorkspaceChangeKind.SolutionAdded)
                {
                    CreateInitialSolutionChecksum();
                    return;
                }

                // record that we are busy
                UpdateLastAccessTime();

                // event will raised sequencially. no concurrency on this handler
                if (_event.CurrentCount > 0)
                {
                    return;
                }

                _event.Release();
            }

            private async Task UpdateSolutionChecksumAsync(CancellationToken cancellationToken)
            {
                // hold onto previous snapshot
                var previousSnapshot = _lastSnapshot;

                // create a new one (incrementally update the snapshot)
                _lastSnapshot = await _checksumService.CreateChecksumAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

                // let old one go.
                previousSnapshot?.Dispose();
            }

            private void CreateInitialSolutionChecksum()
            {
                // initial solution checksum creation won't be affected by global operation.
                // cancellation can only happen if it is being shutdown.
                Task.Run(() => UpdateSolutionChecksumAsync(ShutdownCancellationToken), ShutdownCancellationToken);
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
