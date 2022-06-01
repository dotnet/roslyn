// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This class runs against the in-process workspace, and when it sees changes proactively pushes them to
    /// the out-of-process workspace through the <see cref="IRemoteAssetSynchronizationService"/>.
    /// </summary>
    internal sealed class SolutionChecksumUpdater
    {
        private readonly Workspace _workspace;
        private readonly IGlobalOperationNotificationService _globalOperationService;

        /// <summary>
        /// Lock around mutable state
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Queue to push out text changes in a batched fashion when we hear about them.  Because these should be short
        /// operations (only syncing text changes) we don't cancel this when we enter the paused state.  We simply don't
        /// start queuing more requests into this until we become unpaused.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(Document oldDocument, Document newDocument)> _textChangeQueue;

        /// <summary>
        /// Queue for kicking off the work to synchronize the primary workspace's solution.  The cancellation token is
        /// used so that we can stop the work when we enter the paused state.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<CancellationToken> _synchronizeWorkspaceQueue;

        /// <summary>
        /// Cancellation series we use to stop work when we get paused.
        /// </summary>
        private readonly CancellationSeries _pauseCancellationSeries;

        /// <summary>
        /// Cancellation token we trigger when we enter the paused state. Lock <see cref="_gate"/> to read/write this.
        /// </summary>
        private CancellationToken _currentUnpausedWorkToken;

        /// <summary>
        /// Whether or not we are currently paused. Lock <see cref="_gate"/> to read/write this.
        /// </summary>
        private bool _isPaused;

        public SolutionChecksumUpdater(
            Workspace workspace,
            IAsynchronousOperationListenerProvider listenerProvider,
            CancellationToken shutdownToken)
        {
            var listener = listenerProvider.GetListener(FeatureAttribute.SolutionChecksumUpdater);
            _globalOperationService = workspace.Services.GetRequiredService<IGlobalOperationNotificationService>();

            _pauseCancellationSeries = new CancellationSeries(shutdownToken);
            _workspace = workspace;

            _textChangeQueue = new AsyncBatchingWorkQueue<(Document oldDocument, Document newDocument)>(
                DelayTimeSpan.NearImmediate,
                SynchronizeTextChangesAsync,
                listener,
                shutdownToken);
            _synchronizeWorkspaceQueue = new AsyncBatchingWorkQueue<CancellationToken>(
                DelayTimeSpan.NearImmediate,
                SynchronizePrimaryWorkspaceAsync,
                EqualityComparer<CancellationToken>.Default,
                listener,
                shutdownToken);

            // start listening workspace change event
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
            _globalOperationService.Started += OnGlobalOperationStarted;
            _globalOperationService.Stopped += OnGlobalOperationStopped;

            // Enqueue the work to sync the initial solution.
            ResumeWork();
        }

        public void Shutdown()
        {
            // Try to stop any work that is in progress.
            PauseWork();
            _pauseCancellationSeries.Dispose();

            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
            _globalOperationService.Started -= OnGlobalOperationStarted;
            _globalOperationService.Stopped -= OnGlobalOperationStopped;
        }

        private void OnGlobalOperationStarted(object? sender, EventArgs e)
            => PauseWork();

        private void OnGlobalOperationStopped(object? sender, EventArgs e)
            => ResumeWork();

        private void PauseWork()
        {
            // An expensive global operation started (like a build).  Pause ourselves and cancel any outstanding work in
            // progress to synchronize the solution.
            lock (_gate)
            {
                _isPaused = true;
                _pauseCancellationSeries.CreateNext();
            }
        }

        private void ResumeWork()
        {
            lock (_gate)
            {
                _isPaused = false;

                // create a new token to control all the work we need to do while we're current unpaused.
                _currentUnpausedWorkToken = _pauseCancellationSeries.CreateNext();
                // Start synchronizing again.
                _synchronizeWorkspaceQueue.AddWork(_currentUnpausedWorkToken);
            }
        }

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            bool isPaused;
            CancellationToken workToken;

            lock (_gate)
            {
                isPaused = _isPaused;
                workToken = _currentUnpausedWorkToken;
            }

            // If we're paused, ignore this notification.  We don't want to any work in response to whatever the
            // workspace is doing.
            if (isPaused)
                return;

            if (e.Kind == WorkspaceChangeKind.DocumentChanged)
            {
                _textChangeQueue.AddWork((e.OldSolution.GetDocument(e.DocumentId), e.NewSolution.GetDocument(e.DocumentId)));
            }

            _synchronizeWorkspaceQueue.AddWork(workToken);
        }

        private async ValueTask SynchronizePrimaryWorkspaceAsync(
            ImmutableSegmentedList<CancellationToken> cancellationTokens,
            CancellationToken disposalToken)
        {
            var solution = _workspace.CurrentSolution;
            if (solution.BranchId != _workspace.PrimaryBranchId)
                return;

            // Check if we've been asked to cancel because we've been paused.  If so, don't bother proceeding.
            var uncancelledTokens = cancellationTokens.Where(ct => !ct.IsCancellationRequested).ToList();
            if (uncancelledTokens.Count == 0)
                return;

            uncancelledTokens.Add(disposalToken);

            // Create a token that will fire if we are disposed or if we get paused.
            using var source = CancellationTokenSource.CreateLinkedTokenSource(uncancelledTokens.ToArray());

            // Now actually synchronize the workspace.
            var cancellationToken = source.Token;
            cancellationToken.ThrowIfCancellationRequested();

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizePrimaryWorkspace, cancellationToken))
            {
                var workspaceVersion = solution.WorkspaceVersion;
                await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                    solution,
                    (service, solution, cancellationToken) => service.SynchronizePrimaryWorkspaceAsync(solution, workspaceVersion, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask SynchronizeTextChangesAsync(
            ImmutableSegmentedList<(Document oldDocument, Document newDocument)> values,
            CancellationToken cancellationToken)
        {
            foreach (var (oldDocument, newDocument) in values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SynchronizeTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
            }

            return;

            async ValueTask SynchronizeTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
            {
                // this pushes text changes to the remote side if it can. this is purely perf optimization. whether this
                // pushing text change worked or not doesn't affect feature's functionality.
                //
                // this basically see whether it can cheaply find out text changes between 2 snapshots, if it can, it will
                // send out that text changes to remote side.
                //
                // the remote side, once got the text change, will again see whether it can use that text change information
                // without any high cost and create new snapshot from it.
                //
                // otherwise, it will do the normal behavior of getting full text from VS side. this optimization saves
                // times we need to do full text synchronization for typing scenario.

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
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                    return;

                var state = await oldDocument.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

                await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                    (service, cancellationToken) => service.SynchronizeTextAsync(oldDocument.Id, state.Text, textChanges, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
