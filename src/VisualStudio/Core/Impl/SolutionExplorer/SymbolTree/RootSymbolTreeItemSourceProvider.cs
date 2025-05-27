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
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ForEachCast;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using static Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer.RootSymbolTreeItemSourceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(RootSymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
internal sealed class RootSymbolTreeItemSourceProvider : AbstractSymbolTreeItemSourceProvider<IVsHierarchyItem>
{
    private readonly ConcurrentSet<WeakReference<RootSymbolTreeItemCollectionSource>> _weakCollectionSources = [];

    private readonly AsyncBatchingWorkQueue<DocumentId> _updateSourcesQueue;

    // private readonly IAnalyzersCommandHandler _commandHandler = commandHandler;

    // private IHierarchyItemToProjectIdMap? _projectMap;

    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    [ImportingConstructor]
    public RootSymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider
        /*,
    [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler*/)
        : base(threadingContext, workspace, listenerProvider)
    {
        _updateSourcesQueue = new AsyncBatchingWorkQueue<DocumentId>(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            EqualityComparer<DocumentId>.Default,
            this.Listener,
            this.ThreadingContext.DisposalToken);

        this.Workspace.RegisterWorkspaceChangedHandler(
            e =>
            {
                if (e is { Kind: WorkspaceChangeKind.DocumentChanged or WorkspaceChangeKind.DocumentAdded, DocumentId: not null })
                    _updateSourcesQueue.AddWork(e.DocumentId);
            },
            options: new WorkspaceEventOptions(RequiresMainThread: false));
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
        //var hierarchyMapper = TryGetProjectMap();
        //if (hierarchyMapper == null ||
        //    !hierarchyMapper.TryGetDocumentId(item, targetFrameworkMoniker: null, out var documentId))
        //{
        //    return null;
        //}

        //return null;
    }

    internal abstract class AbstractSymbolTreeItemCollectionSource<TItem>(
        RootSymbolTreeItemSourceProvider provider,
        TItem parentItem) : IAttachedCollectionSource, INotifyPropertyChanged
    {
        protected readonly RootSymbolTreeItemSourceProvider RootProvider = provider;
        protected readonly TItem ParentItem = parentItem;

        protected readonly BulkObservableCollectionWithInit<SymbolTreeItem> SymbolTreeItems = [];

        public object SourceItem { get; } = parentItem!;
        public bool HasItems => !SymbolTreeItems.IsInitialized || SymbolTreeItems.Count > 0;
        public IEnumerable Items => SymbolTreeItems;

        public event PropertyChangedEventHandler PropertyChanged = null!;

        protected void UpdateItems(
            DocumentId documentId,
            ISolutionExplorerSymbolTreeItemProvider itemProvider,
            ImmutableArray<SymbolTreeItemData> items)
        {
            using (this.SymbolTreeItems.GetBulkOperation())
            {
                if (items.Length == 0)
                {
                    // If we got no items, clear everything out.
                    this.SymbolTreeItems.Clear();
                }
                else
                {
                    // We got some item datas.  Attempt to reuse existing symbol tree items that match up to preserve
                    // identity in the tree between changes.
                    IncorporateNewItems(documentId, itemProvider, items);
                }
            }

            // Once we've been initialized once, mark us that way so that we we move out of the 'spinning/computing' state.
            this.SymbolTreeItems.MarkAsInitialized();

            // Notify any listenerrs that we may or may not have items now.
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
        }

        private void IncorporateNewItems(
            DocumentId documentId,
            ISolutionExplorerSymbolTreeItemProvider itemProvider,
            ImmutableArray<SymbolTreeItemData> datas)
        {
            using var _ = Microsoft.CodeAnalysis.PooledObjects.PooledDictionary<SymbolTreeItemKey, Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<SymbolTreeItem>>.GetInstance(out var keyToItems);
            foreach (var item in this.SymbolTreeItems)
                keyToItems.MultiAdd(item.ItemKey, item);

            this.SymbolTreeItems.Clear();

            foreach (var data in datas)
            {
                if (keyToItems.TryGetValue(data.Key, out var matchingItems))
                {
                    // Found a matching item we can use.  Remove it from the list of items so we don't reuse it again.
                    var matchingItem = matchingItems[0];
                    matchingItems.RemoveAt(0);
                    if (matchingItems.Count == 0)
                        keyToItems.Remove(data.Key);

                    // And update it to point to the new data.
                    Contract.ThrowIfFalse(matchingItem.DocumentId == documentId);
                    Contract.ThrowIfFalse(matchingItem.ItemProvider == itemProvider);
                    Contract.ThrowIfFalse(matchingItem.ItemKey == data.Key);

                    matchingItem.ItemSyntax = new(data.DeclarationNode, data.NavigationToken);
                    this.SymbolTreeItems.Add(matchingItem);
                }
                else
                {
                    // If we didn't find an existing item, create a new one.
                    this.SymbolTreeItems.Add(new(this.RootProvider, documentId, itemProvider)
                    {
                        ItemKey = data.Key,
                        ItemSyntax = new(data.DeclarationNode, data.NavigationToken)
                    });
                }
            }
        }
    }

    private sealed class RootSymbolTreeItemCollectionSource(
        RootSymbolTreeItemSourceProvider provider,
        IVsHierarchyItem hierarchyItem)
        : AbstractSymbolTreeItemCollectionSource<IVsHierarchyItem>(provider, hierarchyItem)
    {
        private DocumentId? _documentId;

        internal async Task UpdateIfAffectedAsync(
            HashSet<DocumentId> updateSet, CancellationToken cancellationToken)
        {
            var documentId = DetermineDocumentId();

            // If we successfully handle this request, we're done.
            if (documentId != null && await TryUpdateItemsAsync(updateSet, documentId, cancellationToken).ConfigureAwait(false))
                return;

            // If we didn't have a doc id, or we failed for any reason, clear out all our items.
            using (this.SymbolTreeItems.GetBulkOperation())
                SymbolTreeItems.Clear();
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

            var solution = this.RootProvider.Workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);

            // If we can't find this document anymore, clear everything out.
            if (document is null)
                return false;

            var itemProvider = document.Project.GetLanguageService<ISolutionExplorerSymbolTreeItemProvider>();
            if (itemProvider is null)
                return false;

            var items = await itemProvider.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            this.UpdateItems(documentId, itemProvider, items);
            return true;
        }

        private DocumentId? DetermineDocumentId()
        {
            if (_documentId == null)
            {
                var idMap = this.RootProvider.Workspace.Services.GetService<IHierarchyItemToProjectIdMap>();
                idMap?.TryGetDocumentId(this.ParentItem, targetFrameworkMoniker: null, out _documentId);
            }

            return _documentId;
        }
    }
}
