// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(RootSymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
internal sealed class RootSymbolTreeItemSourceProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
{
    private readonly ConcurrentSet<WeakReference<RootSymbolTreeItemCollectionSource>> _weakCollectionSources = [];

    private readonly AsyncBatchingWorkQueue<DocumentId> _updateSourcesQueue;
    private readonly Workspace _workspace;
    private readonly CancellationSeries _navigationCancellationSeries = new();

    public readonly IThreadingContext ThreadingContext;
    public readonly IAsynchronousOperationListener Listener;

    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    [ImportingConstructor]
    public RootSymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        ThreadingContext = threadingContext;
        _workspace = workspace;
        Listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);

        _updateSourcesQueue = new AsyncBatchingWorkQueue<DocumentId>(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            EqualityComparer<DocumentId>.Default,
            this.Listener,
            this.ThreadingContext.DisposalToken);

        this._workspace.RegisterWorkspaceChangedHandler(
            e =>
            {
                if (e is { Kind: WorkspaceChangeKind.DocumentChanged or WorkspaceChangeKind.DocumentAdded, DocumentId: not null })
                    _updateSourcesQueue.AddWork(e.DocumentId);
            },
            options: new WorkspaceEventOptions(RequiresMainThread: false));
    }

    public void NavigateTo(DocumentId documentId, int position, bool preview)
    {
        // Cancel any in flight navigation and kick off a new one.
        var cancellationToken = _navigationCancellationSeries.CreateNext(this.ThreadingContext.DisposalToken);
        var navigationService = _workspace.Services.GetRequiredService<IDocumentNavigationService>();

        var token = Listener.BeginAsyncOperation(nameof(NavigateTo));
        navigationService.TryNavigateToPositionAsync(
            ThreadingContext,
            _workspace,
            documentId,
            position,
            virtualSpace: 0,
            // May be calling this on stale data.  Allow the position to be invalid
            allowInvalidPosition: true,
            new NavigationOptions(PreferProvisionalTab: preview),
            cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken).CompletesAsyncOperation(token);
    }

    private async ValueTask UpdateCollectionSourcesAsync(
        ImmutableSegmentedList<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        using var _1 = Microsoft.CodeAnalysis.PooledObjects.PooledHashSet<DocumentId>.GetInstance(out var documentIdSet);
        using var _2 = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<RootSymbolTreeItemCollectionSource>.GetInstance(out var sources);

        documentIdSet.AddRange(documentIds);

        foreach (var weakSource in _weakCollectionSources)
        {
            // If solution explorer has released this collection source, we can drop it as well.
            if (!weakSource.TryGetTarget(out var source))
            {
                _weakCollectionSources.Remove(weakSource);
                continue;
            }

            sources.Add(source);
        }

        await RoslynParallel.ForEachAsync(
            sources,
            cancellationToken,
            async (source, cancellationToken) =>
            {
                try
                {
                    await source.UpdateIfAffectedAsync(documentIdSet, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
                {
                }
            }).ConfigureAwait(false);
    }

    protected override IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
    {
        if (item == null ||
            item.HierarchyIdentity == null ||
            item.HierarchyIdentity.NestedHierarchy == null ||
            relationshipName != KnownRelationships.Contains)
        {
            return null;
        }

        var hierarchy = item.HierarchyIdentity.NestedHierarchy;
        var itemId = item.HierarchyIdentity.NestedItemID;

        if (hierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out var capabilitiesObj) != VSConstants.S_OK ||
            capabilitiesObj is not string capabilities)
        {
            return null;
        }

        if (!capabilities.Contains(nameof(VisualStudio.ProjectSystem.ProjectTreeFlags.SourceFile)) ||
            !capabilities.Contains(nameof(VisualStudio.ProjectSystem.ProjectTreeFlags.FileOnDisk)))
        {
            return null;
        }

        var source = new RootSymbolTreeItemCollectionSource(this, item);
        _weakCollectionSources.Add(new WeakReference<RootSymbolTreeItemCollectionSource>(source));
        return source;
    }

    private sealed class RootSymbolTreeItemCollectionSource(
        RootSymbolTreeItemSourceProvider rootProvider,
        IVsHierarchyItem hierarchyItem) : IAttachedCollectionSource, INotifyPropertyChanged
    {
        private readonly RootSymbolTreeItemSourceProvider _rootProvider = rootProvider;
        private readonly IVsHierarchyItem _hierarchyItem = hierarchyItem;

        // Mark hasItems as null as we don't know up front if we have items, and instead have to compute it on demand.
        private readonly SymbolTreeChildCollection _childCollection = new(hierarchyItem, hasItems: null);

        private DocumentId? _documentId;

        internal async Task UpdateIfAffectedAsync(
            HashSet<DocumentId> updateSet,
            CancellationToken cancellationToken)
        {
            var documentId = DetermineDocumentId();

            // If we successfully handle this request, we're done.
            if (documentId != null && await TryUpdateItemsAsync(updateSet, documentId, cancellationToken).ConfigureAwait(false))
                return;

            // If we didn't have a doc id, or we failed for any reason, clear out all our items and set that as our
            // current state.
            await _rootProvider.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _childCollection.ClearAndMarkComputed_OnMainThread(_rootProvider);
        }

        private async ValueTask<bool> TryUpdateItemsAsync(
            HashSet<DocumentId> updateSet, DocumentId documentId, CancellationToken cancellationToken)
        {
            if (!updateSet.Contains(documentId))
            {
                // Note: we intentionally return 'true' here.  There was no failure here. We just got a notification
                // to update a different document than our own.  So we can just ignore this.
                return true;
            }

            var solution = _rootProvider._workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);

            // If we can't find this document anymore, clear everything out.
            if (document is null)
                return false;

            var itemProvider = document.Project.GetLanguageService<ISolutionExplorerSymbolTreeItemProvider>();
            if (itemProvider is null)
                return false;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var items = itemProvider.GetItems(root, cancellationToken);
            await _childCollection.SetItemsAndMarkComputedAsync(
                _rootProvider, documentId, itemProvider, items, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private DocumentId? DetermineDocumentId()
        {
            if (_documentId == null)
            {
                var idMap = _rootProvider._workspace.Services.GetService<IHierarchyItemToProjectIdMap>();
                idMap?.TryGetDocumentId(_hierarchyItem, targetFrameworkMoniker: null, out _documentId);
            }

            return _documentId;
        }

        object IAttachedCollectionSource.SourceItem => _childCollection.SourceItem;

        bool IAttachedCollectionSource.HasItems => _childCollection.HasItems;

        IEnumerable IAttachedCollectionSource.Items => _childCollection.Items;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => _childCollection.PropertyChanged += value;
            remove => _childCollection.PropertyChanged -= value;
        }
    }
}
