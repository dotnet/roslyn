// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal abstract partial class BaseDiagnosticAndGeneratorItemSource : IAttachedCollectionSource
{
    private static readonly DiagnosticDescriptorComparer s_comparer = new();

    private readonly BulkObservableCollection<BaseItem> _items = [];

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AsyncBatchingWorkQueue _workQueue;
    private readonly IThreadingContext _threadingContext;

    protected Workspace Workspace { get; }
    protected ProjectId ProjectId { get; }
    protected IAnalyzersCommandHandler CommandHandler { get; }

    private WorkspaceEventRegistration? _workspaceChangedDisposer;

    public BaseDiagnosticAndGeneratorItemSource(
        IThreadingContext threadingContext,
        Workspace workspace,
        ProjectId projectId,
        IAnalyzersCommandHandler commandHandler,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        Workspace = workspace;
        ProjectId = projectId;
        CommandHandler = commandHandler;

        _workQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Idle,
            ProcessQueueAsync,
            listenerProvider.GetListener(FeatureAttribute.SourceGenerators),
            _cancellationTokenSource.Token);
    }

    /// <summary>
    /// The analyzer reference that has been found. Once it's been assigned a non-null value, it'll never be assigned
    /// <see langword="null"/> again.
    /// </summary>
    protected AnalyzerReference? AnalyzerReference
    {
        get;
        set
        {
            Contract.ThrowIfTrue(field != null);
            if (value is null)
                return;

            field = value;

            // Listen for changes that would affect the set of analyzers/generators in this reference, and kick off work
            // to now get the items for this source.
            _workspaceChangedDisposer = Workspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);
            _workQueue.AddWork();
        }
    }

    public abstract object SourceItem { get; }

    // Defer actual determination and computation of the items until later.
    public bool HasItems => !_cancellationTokenSource.IsCancellationRequested;

    public IEnumerable Items => _items;

    private async ValueTask ProcessQueueAsync(CancellationToken cancellationToken)
    {
        var analyzerReference = this.AnalyzerReference;

        // If we haven't even determined which analyzer reference we're for, there's nothing to do.
        if (analyzerReference is null)
            return;

        // If the project went away, or no longer contains this analyzer.  Shut ourselves down.
        var project = this.Workspace.CurrentSolution.GetProject(this.ProjectId);
        if (project is null || !project.AnalyzerReferences.Contains(analyzerReference))
        {
            _workspaceChangedDisposer?.Dispose();
            _workspaceChangedDisposer = null;

            _cancellationTokenSource.Cancel();

            // Note: mutating _items will be picked up automatically by clients who are bound to the collection.  We do
            // not need to notify them through some other mechanism.
            if (_items.Count > 0)
            {
                // Go back to UI thread to update the observable collection.  Otherwise, it enqueue its own UI work that we cannot track.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _items.Clear();
            }

            return;
        }

        var (latestDiagnosticItems, latestSourceGeneratorItems) = await GetLatestItemsAsync(
            project, analyzerReference, cancellationToken).ConfigureAwait(false);

        // If we computed the same set of items as the last time, we can bail out now.
        if (_items.SequenceEqual([.. latestDiagnosticItems, .. latestSourceGeneratorItems]))
            return;

        // Go back to UI thread to update the observable collection.  Otherwise, it enqueue its own UI work that we cannot track.
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _items.BeginBulkOperation();
        try
        {
            _items.Clear();
            _items.AddRange(latestDiagnosticItems);
            _items.AddRange(latestSourceGeneratorItems);
        }
        finally
        {
            _items.EndBulkOperation();
        }
    }

    private async Task<(ImmutableArray<BaseItem>, ImmutableArray<BaseItem>)> GetLatestItemsAsync(
        Project project,
        AnalyzerReference analyzerReference,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(this.Workspace, cancellationToken).ConfigureAwait(false);

        var latestDiagnosticItems = await GenerateDiagnosticItemsAsync().ConfigureAwait(false);
        var latestSourceGeneratorItems = await GenerateSourceGeneratorItemsAsync().ConfigureAwait(false);

        return (latestDiagnosticItems, latestSourceGeneratorItems);

        async Task<ImmutableArray<BaseItem>> GenerateDiagnosticItemsAsync()
        {
            var generalDiagnosticOption = project.CompilationOptions!.GeneralDiagnosticOption;
            var specificDiagnosticOptions = project.CompilationOptions!.SpecificDiagnosticOptions;
            var analyzerConfigOptions = project.GetAnalyzerConfigOptions();

            var descriptors = await GetDiagnosticDescriptorsAsync().ConfigureAwait(false);

            return descriptors
                .GroupBy(d => d.Id)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .SelectAsArray(g =>
                {
                    var selectedDiagnostic = g.OrderBy(d => d, s_comparer).First();
                    var effectiveSeverity = selectedDiagnostic.GetEffectiveSeverity(
                        project.CompilationOptions!,
                        analyzerConfigOptions?.ConfigOptionsWithoutFallback,
                        analyzerConfigOptions?.TreeOptions);
                    return (BaseItem)new DiagnosticItem(project.Id, analyzerReference, selectedDiagnostic, effectiveSeverity, CommandHandler);
                });
        }

        async ValueTask<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync()
        {
            // Call out to oop to do this if possible.  This way we don't actually load the analyzers in proc.
            // this also allows 
            if (client is not null &&
                analyzerReference is AnalyzerFileReference analyzerFileReference)
            {
                var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticDescriptorData>>(
                    project,
                    (service, solutionChecksum, cancellationToken) => service.GetDiagnosticDescriptorsAsync(
                        solutionChecksum, project.Id, analyzerFileReference.FullPath, project.Language, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                // If the call fails, the OOP substrate will have already reported an error
                if (!result.HasValue)
                    return [];

                return result.Value.SelectAsArray(d => d.ToDiagnosticDescriptor());
            }

            // Otherwise, do the work in process.
            return await project.GetDiagnosticDescriptorsAsync(analyzerReference, cancellationToken).ConfigureAwait(false);
        }

        async Task<ImmutableArray<BaseItem>> GenerateSourceGeneratorItemsAsync()
        {
            var identifies = await GetIdentitiesAsync().ConfigureAwait(false);
            return identifies.SelectAsArray(
                identity => (BaseItem)new SourceGeneratorItem(project.Id, identity, analyzerReference.FullPath));
        }

        async Task<ImmutableArray<SourceGeneratorIdentity>> GetIdentitiesAsync()
        {
            // Can only remote AnalyzerFileReferences over to the oop side.  If we have another form of reference (like
            // in tests), we'll just fall back to loading these in process.
            if (client is not null &&
                analyzerReference is AnalyzerFileReference analyzerFileReference)
            {
                var result = await client.TryInvokeAsync<IRemoteSourceGenerationService, ImmutableArray<SourceGeneratorIdentity>>(
                    project,
                    (service, solutionChecksum, cancellationToken) => service.GetSourceGeneratorIdentitiesAsync(
                        solutionChecksum, project.Id, analyzerFileReference.FullPath, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                // If the call fails, the OOP substrate will have already reported an error
                if (!result.HasValue)
                    return [];

                return result.Value;
            }

            // Do the work in process.
            return SourceGeneratorIdentity.GetIdentities(analyzerReference, project.Language);
        }
    }

    private void OnWorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        switch (e.Kind)
        {
            // Solution is going away or being reloaded. The work queue will detect this and clean up accordingly.
            case WorkspaceChangeKind.SolutionCleared:
            case WorkspaceChangeKind.SolutionReloaded:
            case WorkspaceChangeKind.SolutionRemoved:
            // The project itself is being removed.  The work queue will detect this and clean up accordingly.
            case WorkspaceChangeKind.ProjectRemoved:
            case WorkspaceChangeKind.ProjectChanged:
            // Could change the severity of an analyzer.
            case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
            case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                _workQueue.AddWork();
                break;
            default:
                break;
        }
    }
}
