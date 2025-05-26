// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ForEachCast;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Microsoft.VisualStudio.LanguageServices.Extensions;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.CodeAnalysis.Editor.Wpf;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(SymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
internal sealed class SymbolTreeItemSourceProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
{
    private readonly IThreadingContext _threadingContext;
    private readonly Workspace _workspace;

    private readonly ConcurrentSet<WeakReference<SymbolTreeItemCollectionSource>> _weakCollectionSources = [];

    private readonly AsyncBatchingWorkQueue<DocumentId> _updateSourcesQueue;

    // private readonly IAnalyzersCommandHandler _commandHandler = commandHandler;

    // private IHierarchyItemToProjectIdMap? _projectMap;

    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    [ImportingConstructor]
    public SymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider
        /*,
    [Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler*/)
    {
        _threadingContext = threadingContext;
        _workspace = workspace;

        _updateSourcesQueue = new AsyncBatchingWorkQueue<DocumentId>(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            EqualityComparer<DocumentId>.Default,
            listenerProvider.GetListener(FeatureAttribute.SolutionExplorer),
            _threadingContext.DisposalToken);

        _workspace.RegisterWorkspaceChangedHandler(
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
        using var _2 = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<SymbolTreeItemCollectionSource>.GetInstance(out var sources);

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

    //private IHierarchyItemToProjectIdMap? TryGetProjectMap()
    //    => _projectMap ??= _workspace.Services.GetService<IHierarchyItemToProjectIdMap>();

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

        var source = new SymbolTreeItemCollectionSource(this, item);
        _weakCollectionSources.Add(new WeakReference<SymbolTreeItemCollectionSource>(source));
        return source;
        //var hierarchyMapper = TryGetProjectMap();
        //if (hierarchyMapper == null ||
        //    !hierarchyMapper.TryGetDocumentId(item, targetFrameworkMoniker: null, out var documentId))
        //{
        //    return null;
        //}

        //return null;
    }

    private sealed class SymbolTreeItemCollectionSource(
        SymbolTreeItemSourceProvider provider,
        IVsHierarchyItem hierarchyItem)
        : IAttachedCollectionSource
    {
        private readonly SymbolTreeItemSourceProvider _provider = provider;
        private readonly IVsHierarchyItem _hierarchyItem = hierarchyItem;

        private readonly BulkObservableCollection<SymbolTreeItem> _symbolTreeItems = [];

        private DocumentId? _documentId;

        public object SourceItem => _hierarchyItem;
        public bool HasItems => true;
        public IEnumerable Items => _symbolTreeItems;

        internal async Task UpdateIfAffectedAsync(
            HashSet<DocumentId> updateSet, CancellationToken cancellationToken)
        {
            var documentId = DetermineDocumentId();

            // If we successfully handle this request, we're done.
            if (await TryUpdateItemsAsync(updateSet, documentId, cancellationToken).ConfigureAwait(false))
                return;

            // If we failed for any reason, clear out all our items.
            using (_symbolTreeItems.GetBulkOperation())
                _symbolTreeItems.Clear();
        }
        private async ValueTask<bool> TryUpdateItemsAsync(
            HashSet<DocumentId> updateSet, DocumentId? documentId, CancellationToken cancellationToken)
        {
            if (documentId is null)
                return false;

            if (!updateSet.Contains(documentId))
            {
                // Note: we intentionally return 'true' here.  There was no failure here. We just got a notification
                // to update a different document than our own.  So we can just ignore this.
                return true;
            }

            var solution = _provider._workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);

            // If we can't find this document anymore, clear everything out.
            if (document is null)
                return false;

            var service = document.Project.GetLanguageService<ISolutionExplorerSymbolTreeItemProvider>();
            if (service is null)
                return false;

            var items = await service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);

            using (_symbolTreeItems.GetBulkOperation())
            {
                _symbolTreeItems.Clear();
                _symbolTreeItems.AddRange(items);
            }

            return true;
        }

        private DocumentId? DetermineDocumentId()
        {
            if (_documentId == null)
            {
                var idMap = _provider._workspace.Services.GetService<IHierarchyItemToProjectIdMap>();
                idMap?.TryGetDocumentId(_hierarchyItem, targetFrameworkMoniker: null, out _documentId);
            }

            return _documentId;
        }
    }

    //private static ImmutableArray<string> GetProjectTreeCapabilities(IVsHierarchy hierarchy, uint itemId)
    //{
    //    if (hierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, out var capabilitiesObj) == VSConstants.S_OK)
    //    {
    //        var capabilitiesString = (string)capabilitiesObj;
    //        return ImmutableArray.Create(capabilitiesString.Split(' '));
    //    }
    //    else
    //    {
    //        return [];
    //    }
    //}

}

internal sealed class SymbolTreeItem(
    string name,
    Glyph glyph,
    SyntaxNode syntaxNode)
    : BaseItem(name)
{
    public override ImageMoniker IconMoniker { get; } = glyph.GetImageMoniker();

    public readonly SyntaxNode SyntaxNode = syntaxNode;
}

internal interface ISolutionExplorerSymbolTreeItemProvider : ILanguageService
{
    Task<ImmutableArray<SymbolTreeItem>> GetItemsAsync(Document document, CancellationToken cancellationToken);
}
