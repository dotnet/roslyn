// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
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

    private readonly IDocumentTrackingService _documentTrackingService;

    /// <summary>
    /// Queue to push out text changes in a batched fashion when we hear about them.  Because these should be short
    /// operations (only syncing text changes) we don't cancel this when we enter the paused state.  We simply don't
    /// start queuing more requests into this until we become unpaused.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<(Document oldDocument, Document newDocument)> _textChangeQueue;

    /// <summary>
    /// Queue for kicking off the work to synchronize the primary workspace's solution.
    /// </summary>
    private readonly AsyncBatchingWorkQueue _synchronizeWorkspaceQueue;

    /// <summary>
    /// Queue for kicking off the work to synchronize the active document to the remote process.
    /// </summary>
    private readonly AsyncBatchingWorkQueue _synchronizeActiveDocumentQueue;

    private readonly object _gate = new();
    private bool _isSynchronizeWorkspacePaused;

    public SolutionChecksumUpdater(
        Workspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider,
        CancellationToken shutdownToken)
    {
        var listener = listenerProvider.GetListener(FeatureAttribute.SolutionChecksumUpdater);

        _globalOperationService = workspace.Services.SolutionServices.ExportProvider.GetExports<IGlobalOperationNotificationService>().FirstOrDefault()?.Value;

        _workspace = workspace;
        _documentTrackingService = workspace.Services.GetRequiredService<IDocumentTrackingService>();

        _synchronizeWorkspaceQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.NearImmediate,
            SynchronizePrimaryWorkspaceAsync,
            listener,
            shutdownToken);

        // Text changes and active doc info are tiny messages.  So attempt to send them immediately.  Just batching
        // things up if we get a flurry of notifications.
        _textChangeQueue = new AsyncBatchingWorkQueue<(Document oldDocument, Document newDocument)>(
            TimeSpan.Zero,
            SynchronizeTextChangesAsync,
            listener,
            shutdownToken);

        _synchronizeActiveDocumentQueue = new AsyncBatchingWorkQueue(
            TimeSpan.Zero,
            SynchronizeActiveDocumentAsync,
            listener,
            shutdownToken);

        // start listening workspace change event
        _workspace.WorkspaceChanged += OnWorkspaceChanged;
        _documentTrackingService.ActiveDocumentChanged += OnActiveDocumentChanged;

        if (_globalOperationService != null)
        {
            _globalOperationService.Started += OnGlobalOperationStarted;
            _globalOperationService.Stopped += OnGlobalOperationStopped;
        }

        // Enqueue the work to sync the initial data over.
        _synchronizeActiveDocumentQueue.AddWork();
        _synchronizeWorkspaceQueue.AddWork();
    }

    public void Shutdown()
    {
        // Try to stop any work that is in progress.
        PauseSynchronizingPrimaryWorkspace();

        _documentTrackingService.ActiveDocumentChanged -= OnActiveDocumentChanged;
        _workspace.WorkspaceChanged -= OnWorkspaceChanged;

        if (_globalOperationService != null)
        {
            _globalOperationService.Started -= OnGlobalOperationStarted;
            _globalOperationService.Stopped -= OnGlobalOperationStopped;
        }
    }

    private void OnGlobalOperationStarted(object? sender, EventArgs e)
        => PauseSynchronizingPrimaryWorkspace();

    private void OnGlobalOperationStopped(object? sender, EventArgs e)
        => ResumeSynchronizingPrimaryWorkspace();

    private void PauseSynchronizingPrimaryWorkspace()
    {
        // An expensive global operation started (like a build).  Pause ourselves and cancel any outstanding work in
        // progress to synchronize the solution.
        lock (_gate)
        {
            _synchronizeWorkspaceQueue.CancelExistingWork();
            _isSynchronizeWorkspacePaused = true;
        }
    }

    private void ResumeSynchronizingPrimaryWorkspace()
    {
        lock (_gate)
        {
            _isSynchronizeWorkspacePaused = false;
            _synchronizeWorkspaceQueue.AddWork();
        }
    }

    private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        if (e.Kind == WorkspaceChangeKind.DocumentChanged)
        {
            var oldDocument = e.OldSolution.GetDocument(e.DocumentId);
            var newDocument = e.NewSolution.GetDocument(e.DocumentId);
            if (oldDocument != null && newDocument != null)
                _textChangeQueue.AddWork((oldDocument, newDocument));
        }

        // Check if we're currently paused.  If so ignore this notification.  We don't want to any work in response
        // to whatever the workspace is doing.
        lock (_gate)
        {
            if (!_isSynchronizeWorkspacePaused)
                _synchronizeWorkspaceQueue.AddWork();
        }
    }

    private void OnActiveDocumentChanged(object? sender, DocumentId? e)
        => _synchronizeActiveDocumentQueue.AddWork();

    private async ValueTask SynchronizePrimaryWorkspaceAsync(CancellationToken cancellationToken)
    {
        var solution = _workspace.CurrentSolution;

        // Wait for the remote side to actually become available (without being the cause of its creation ourselves). We
        // want to wait for some feature to kick this off, then we'll start syncing this data once that has happened.
        await RemoteHostClient.WaitForClientCreationAsync(_workspace, cancellationToken).ConfigureAwait(false);

        var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
        if (client == null)
            return;

        using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizePrimaryWorkspace, cancellationToken))
        {
            await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                solution,
                (service, solution, cancellationToken) => service.SynchronizePrimaryWorkspaceAsync(solution, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask SynchronizeActiveDocumentAsync(CancellationToken cancellationToken)
    {
        var activeDocument = _documentTrackingService.TryGetActiveDocument();

        // Wait for the remote side to actually become available (without being the cause of its creation ourselves). We
        // want to wait for some feature to kick this off, then we'll start syncing this data once that has happened.
        await RemoteHostClient.WaitForClientCreationAsync(_workspace, cancellationToken).ConfigureAwait(false);

        var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
        if (client == null)
            return;

        var solution = _workspace.CurrentSolution;
        await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
            (service, cancellationToken) => service.SynchronizeActiveDocumentAsync(activeDocument, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask SynchronizeTextChangesAsync(
        ImmutableSegmentedList<(Document oldDocument, Document newDocument)> values,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
        if (client == null)
            return;

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
        using var _ = ArrayBuilder<(DocumentId id, Checksum textChecksum, ImmutableArray<TextChange> changes, Checksum newTextChecksum)>.GetInstance(out var builder);

        foreach (var (oldDocument, newDocument) in values)
        {
            if (!oldDocument.TryGetText(out var oldText) ||
                !newDocument.TryGetText(out var newText))
            {
                // we only support case where text already exist
                continue;
            }

            // Avoid allocating text before seeing if we can bail out.
            var changeRanges = newText.GetChangeRanges(oldText).AsImmutable();
            if (changeRanges.Length == 0)
                continue;

            // no benefit here. pulling from remote host is more efficient
            if (changeRanges is [{ Span.Length: var singleChangeLength }] && singleChangeLength == oldText.Length)
                continue;

            var state = await oldDocument.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var newState = await newDocument.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            var textChanges = newText.GetTextChanges(oldText).AsImmutable();
            builder.Add((oldDocument.Id, state.Text, textChanges, newState.Text));
        }

        if (builder.Count == 0)
            return;

        await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
            (service, cancellationToken) => service.SynchronizeTextChangesAsync(builder.ToImmutableAndClear(), cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }
}
