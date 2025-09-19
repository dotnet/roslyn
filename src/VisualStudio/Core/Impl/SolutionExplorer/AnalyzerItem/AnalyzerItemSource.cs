// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class AnalyzerItemSource : IAttachedCollectionSource
{
    private readonly AnalyzersFolderItem _analyzersFolder;
    private readonly IAnalyzersCommandHandler _commandHandler;

    private readonly BulkObservableCollection<AnalyzerItem> _items = [];

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AsyncBatchingWorkQueue _workQueue;

    private IReadOnlyCollection<AnalyzerReference>? _analyzerReferences;

    private Workspace Workspace => _analyzersFolder.Workspace;
    private ProjectId ProjectId => _analyzersFolder.ProjectId;

    private WorkspaceEventRegistration? _workspaceChangedDisposer;

    public AnalyzerItemSource(
        AnalyzersFolderItem analyzersFolder,
        IAnalyzersCommandHandler commandHandler,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _analyzersFolder = analyzersFolder;
        _commandHandler = commandHandler;

        _workQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Idle,
            ProcessQueueAsync,
            listenerProvider.GetListener(FeatureAttribute.SourceGenerators),
            _cancellationTokenSource.Token);

        _workspaceChangedDisposer = this.Workspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);

        // Kick off the initial work to determine the starting set of items.
        _workQueue.AddWork();
    }

    public object SourceItem => _analyzersFolder;

    // Defer actual determination and computation of the items until later.
    public bool HasItems => !_cancellationTokenSource.IsCancellationRequested;

    public IEnumerable Items => _items;

    private void OnWorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        switch (e.Kind)
        {
            case WorkspaceChangeKind.SolutionAdded:
            case WorkspaceChangeKind.SolutionChanged:
            case WorkspaceChangeKind.SolutionReloaded:
            case WorkspaceChangeKind.SolutionRemoved:
            case WorkspaceChangeKind.SolutionCleared:
                _workQueue.AddWork();
                break;

            case WorkspaceChangeKind.ProjectAdded:
            case WorkspaceChangeKind.ProjectReloaded:
            case WorkspaceChangeKind.ProjectChanged:
            case WorkspaceChangeKind.ProjectRemoved:
                if (e.ProjectId == this.ProjectId)
                    _workQueue.AddWork();

                break;
        }
    }

    private async ValueTask ProcessQueueAsync(CancellationToken cancellationToken)
    {
        // If the project went away, then shut ourselves down.
        var project = this.Workspace.CurrentSolution.GetProject(this.ProjectId);
        if (project is null)
        {
            _workspaceChangedDisposer?.Dispose();
            _workspaceChangedDisposer = null;

            _cancellationTokenSource.Cancel();

            // Note: mutating _items will be picked up automatically by clients who are bound to the collection.  We do
            // not need to notify them through some other mechanism.

            if (_items.Count > 0)
            {
                // Go back to UI thread to update the observable collection.  Otherwise, it enqueue its own UI work that we cannot track.
                await _analyzersFolder.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _items.Clear();
            }

            return;
        }

        // If nothing changed wrt analyzer references, then there's nothing we need to do.
        if (project.AnalyzerReferences == _analyzerReferences)
            return;

        // Set the new set of analyzer references we're going to have AnalyzerItems for.
        _analyzerReferences = project.AnalyzerReferences;

        var references = await GetAnalyzerReferencesWithAnalyzersOrGeneratorsAsync(
            project, cancellationToken).ConfigureAwait(false);

        // Go back to UI thread to update the observable collection.  Otherwise, it enqueue its own UI work that we cannot track.
        await _analyzersFolder.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        try
        {
            _items.BeginBulkOperation();

            _items.Clear();
            foreach (var analyzerReference in references.OrderBy(static r => r.Display))
                _items.Add(new AnalyzerItem(_analyzersFolder, analyzerReference, _commandHandler.AnalyzerContextMenuController));

            return;
        }
        finally
        {
            _items.EndBulkOperation();
        }

        async Task<ImmutableArray<AnalyzerReference>> GetAnalyzerReferencesWithAnalyzersOrGeneratorsAsync(
            Project project,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(this.Workspace, cancellationToken).ConfigureAwait(false);

            // If we can't make a remote call.  Fall back to processing in the VS host.
            if (client is null)
                return [.. project.AnalyzerReferences.Where(r => r is not AnalyzerFileReference || r.HasAnalyzersOrSourceGenerators(project.Language))];

            using var connection = client.CreateConnection<IRemoteSourceGenerationService>(callbackTarget: null);

            using var _ = ArrayBuilder<AnalyzerReference>.GetInstance(out var builder);
            foreach (var reference in project.AnalyzerReferences)
            {
                // Can only remote AnalyzerFileReferences over to the oop side.
                if (reference is AnalyzerFileReference analyzerFileReference)
                {
                    var result = await connection.TryInvokeAsync<bool>(
                        project,
                        (service, solutionChecksum, cancellationToken) => service.HasAnalyzersOrSourceGeneratorsAsync(
                            solutionChecksum, project.Id, analyzerFileReference.FullPath, cancellationToken),
                        cancellationToken).ConfigureAwait(false);

                    // If the call fails, the OOP substrate will have already reported an error
                    if (!result.HasValue)
                        return [];

                    if (result.Value)
                        builder.Add(analyzerFileReference);
                }
                else if (reference.HasAnalyzersOrSourceGenerators(project.Language))
                {
                    builder.Add(reference);
                }
            }

            return builder.ToImmutableAndClear();
        }
    }
}
