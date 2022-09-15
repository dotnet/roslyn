// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This class runs against the in-process workspace, and when it sees changes proactively pushes them to
    /// the out-of-process workspace through the <see cref="IRemoteAssetSynchronizationService"/>.
    /// </summary>
    internal sealed class SolutionChecksumUpdater : GlobalOperationAwareIdleProcessor
    {
        private readonly Workspace _workspace;
        private readonly TaskQueue _textChangeQueue;
        private readonly AsyncQueue<IAsyncToken> _workQueue;
        private readonly object _gate;

        private CancellationTokenSource _globalOperationCancellationSource;

        // hold the async token from WaitAsync so ExecuteAsync can complete it
        private IAsyncToken _currentToken;

        public SolutionChecksumUpdater(Workspace workspace, IGlobalOptionService globalOptions, IAsynchronousOperationListenerProvider listenerProvider, CancellationToken shutdownToken)
            : base(listenerProvider.GetListener(FeatureAttribute.SolutionChecksumUpdater),
                   workspace.Services.GetService<IGlobalOperationNotificationService>(),
                   TimeSpan.FromMilliseconds(globalOptions.GetOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS)), shutdownToken)
        {
            _workspace = workspace;
            _textChangeQueue = new TaskQueue(Listener, TaskScheduler.Default);

            _workQueue = new AsyncQueue<IAsyncToken>();
            _gate = new object();

            // start listening workspace change event
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            // create its own cancellation token source
            _globalOperationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);

            Start();
        }

        private CancellationToken ShutdownCancellationToken => CancellationToken;

        protected override async Task ExecuteAsync()
        {
            lock (_gate)
            {
                Contract.ThrowIfNull(_currentToken);
                _currentToken.Dispose();
                _currentToken = null;
            }

            // wait for global operation to finish
            await GlobalOperationTask.ConfigureAwait(false);

            // update primary solution in remote host
            await SynchronizePrimaryWorkspaceAsync(_globalOperationCancellationSource.Token).ConfigureAwait(false);
        }

        protected override void PauseOnGlobalOperation()
        {
            var previousCancellationSource = _globalOperationCancellationSource;

            // create new cancellation token source linked with given shutdown cancellation token
            _globalOperationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(ShutdownCancellationToken);

            CancelAndDispose(previousCancellationSource);
        }

        protected override async Task WaitAsync(CancellationToken cancellationToken)
        {
            var currentToken = await _workQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                Contract.ThrowIfFalse(_currentToken is null);
                _currentToken = currentToken;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();

            // stop listening workspace change event
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;

            CancelAndDispose(_globalOperationCancellationSource);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.DocumentChanged)
            {
                PushTextChanges(e.OldSolution.GetDocument(e.DocumentId), e.NewSolution.GetDocument(e.DocumentId));
            }

            // record that we are busy
            UpdateLastAccessTime();

            EnqueueChecksumUpdate();
        }

        private void EnqueueChecksumUpdate()
        {
            // event will raised sequencially. no concurrency on this handler
            if (_workQueue.TryPeek(out _))
            {
                return;
            }

            _workQueue.Enqueue(Listener.BeginAsyncOperation(nameof(SolutionChecksumUpdater)));
        }

        private async Task SynchronizePrimaryWorkspaceAsync(CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;
            if (solution.BranchId != _workspace.PrimaryBranchId)
            {
                return;
            }

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizePrimaryWorkspace, cancellationToken))
            {
                var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                    solution,
                    (service, solution, cancellationToken) => service.SynchronizePrimaryWorkspaceAsync(solution, checksum, solution.WorkspaceVersion, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static void CancelAndDispose(CancellationTokenSource cancellationSource)
        {
            // cancel running tasks
            cancellationSource.Cancel();

            // dispose cancellation token source
            cancellationSource.Dispose();
        }

        private void PushTextChanges(Document oldDocument, Document newDocument)
        {
            // this pushes text changes to the remote side if it can.
            // this is purely perf optimization. whether this pushing text change
            // worked or not doesn't affect feature's functionality.
            //
            // this basically see whether it can cheaply find out text changes
            // between 2 snapshots, if it can, it will send out that text changes to
            // remote side.
            //
            // the remote side, once got the text change, will again see whether
            // it can use that text change information without any high cost and
            // create new snapshot from it.
            //
            // otherwise, it will do the normal behavior of getting full text from
            // VS side. this optimization saves times we need to do full text
            // synchronization for typing scenario.

            if ((oldDocument.TryGetText(out var oldText) == false) ||
                (newDocument.TryGetText(out var newText) == false))
            {
                // we only support case where text already exist
                return;
            }

            // get text changes
            var textChanges = newText.GetTextChanges(oldText);
            if (textChanges.Count == 0)
            {
                // no changes
                return;
            }

            // whole document case
            if (textChanges.Count == 1 && textChanges[0].Span.Length == oldText.Length)
            {
                // no benefit here. pulling from remote host is more efficient
                return;
            }

            // only cancelled when remote host gets shutdown
            _textChangeQueue.ScheduleTask(nameof(PushTextChanges), async () =>
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, CancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return;
                }

                var state = await oldDocument.State.GetStateChecksumsAsync(CancellationToken).ConfigureAwait(false);

                await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                    (service, cancellationToken) => service.SynchronizeTextAsync(oldDocument.Id, state.Text, textChanges, cancellationToken),
                    CancellationToken).ConfigureAwait(false);
            }, CancellationToken);
        }
    }
}
