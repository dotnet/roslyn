// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// This class runs against the in-process workspace, and when it sees changes proactively pushes them to
/// the out-of-process workspace through the <see cref="IRemoteAssetSynchronizationService"/>.
/// </summary>
internal sealed class SolutionChecksumUpdater
{
    private readonly Workspace _workspace;

    /// <summary>
    /// We're not at a layer where we are guaranteed to have an IGlobalOperationNotificationService.  So allow for
    /// it being null.
    /// </summary>
    private readonly IGlobalOperationNotificationService? _globalOperationService;

    /// <summary>
    /// Queue to push out text changes in a batched fashion when we hear about them.  Because these should be short
    /// operations (only syncing text changes) we don't cancel this when we enter the paused state.  We simply don't
    /// start queuing more requests into this until we become unpaused.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<(Document? oldDocument, Document? newDocument)> _textChangeQueue;

    /// <summary>
    /// Queue for kicking off the work to synchronize the primary workspace's solution.
    /// </summary>
    private readonly AsyncBatchingWorkQueue _synchronizeWorkspaceQueue;

    private readonly object _gate = new();
    private bool _isPaused;

    public SolutionChecksumUpdater(
        Workspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider,
        CancellationToken shutdownToken)
    {
        var listener = listenerProvider.GetListener(FeatureAttribute.SolutionChecksumUpdater);

        _globalOperationService = workspace.Services.SolutionServices.ExportProvider.GetExports<IGlobalOperationNotificationService>().FirstOrDefault()?.Value;

        _workspace = workspace;

        _textChangeQueue = new AsyncBatchingWorkQueue<(Document? oldDocument, Document? newDocument)>(
            DelayTimeSpan.NearImmediate,
            SynchronizeTextChangesAsync,
            listener,
            shutdownToken);

        // Use an equality comparer here as we will commonly get lots of change notifications that will all be
        // associated with the same cancellation token controlling that batch of work.  No need to enqueue the same
        // token a huge number of times when we only need the single value of it when doing the work.
        _synchronizeWorkspaceQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.NearImmediate,
            SynchronizePrimaryWorkspaceAsync,
            listener,
            shutdownToken);

        // start listening workspace change event
        _workspace.WorkspaceChanged += OnWorkspaceChanged;

        if (_globalOperationService != null)
        {
            _globalOperationService.Started += OnGlobalOperationStarted;
            _globalOperationService.Stopped += OnGlobalOperationStopped;
        }

        // Enqueue the work to sync the initial solution.
        ResumeWork();
    }

    public void Shutdown()
    {
        // Try to stop any work that is in progress.
        PauseWork();

        _workspace.WorkspaceChanged -= OnWorkspaceChanged;

        if (_globalOperationService != null)
        {
            _globalOperationService.Started -= OnGlobalOperationStarted;
            _globalOperationService.Stopped -= OnGlobalOperationStopped;
        }
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
            _synchronizeWorkspaceQueue.CancelExistingWork();
            _isPaused = true;
        }
    }

    private void ResumeWork()
    {
        lock (_gate)
        {
            _isPaused = false;
            _synchronizeWorkspaceQueue.AddWork();
        }
    }

    private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        // Check if we're currently paused.  If so ignore this notification.  We don't want to any work in response
        // to whatever the workspace is doing.
        lock (_gate)
        {
            if (_isPaused)
                return;
        }

        if (e.Kind == WorkspaceChangeKind.DocumentChanged)
        {
            _textChangeQueue.AddWork((e.OldSolution.GetDocument(e.DocumentId), e.NewSolution.GetDocument(e.DocumentId)));
        }

        _synchronizeWorkspaceQueue.AddWork();
    }

    private async ValueTask SynchronizePrimaryWorkspaceAsync(CancellationToken cancellationToken)
    {
        var solution = _workspace.CurrentSolution;
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
        ImmutableSegmentedList<(Document? oldDocument, Document? newDocument)> values,
        CancellationToken cancellationToken)
    {
        foreach (var (oldDocument, newDocument) in values)
        {
            if (oldDocument is null || newDocument is null)
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            await SynchronizeTextChangesAsync(oldDocument, newDocument, cancellationToken).ConfigureAwait(false);
        }

        return;

        async ValueTask SynchronizeTextChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
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
