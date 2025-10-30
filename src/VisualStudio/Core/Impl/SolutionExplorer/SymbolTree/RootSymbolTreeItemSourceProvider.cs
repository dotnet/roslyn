// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.GoOrFind;
using Microsoft.CodeAnalysis.GoToBase;
using Microsoft.CodeAnalysis.GoToImplementation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// Source provider responsible for hearing about C#/VB files and attaching the root 'symbol tree' node.
/// Users can then expand that node to get access to the symbols within the file.  Note: this tree is 
/// built lazily (one level at a time), and only uses syntax so it can be done extremely quickly.
/// </summary>
[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(RootSymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
internal sealed partial class RootSymbolTreeItemSourceProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
{
    /// <summary>
    /// Mapping from filepath to the collection sources made for it.  Is a multi dictionary because the same
    /// file may appear in multiple projects, but each will have its own collection soure to represent the view
    /// of that file through that project.
    /// </summary>
    /// <remarks>Lock this instance when reading/writing as it is used over different threads.</remarks>
    private readonly Dictionary<string, List<RootSymbolTreeItemCollectionSource>> _filePathToCollectionSources = new(
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Queue of notifications we've heard about for changed document file paths.  We'll then go update the
    /// symbol tree item for each of these documents so that it is up to date.  Note: if the symbol tree has
    /// never been expanded,  this will bail immediately to avoid doing unnecessary work.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<string> _updateSourcesQueue;
    private readonly Workspace _workspace;

    private readonly IGoOrFindNavigationService _goToBaseNavigationService;
    private readonly IGoOrFindNavigationService _goToImplementationNavigationService;
    private readonly IGoOrFindNavigationService _findReferencesNavigationService;

    public readonly SolutionExplorerNavigationSupport NavigationSupport;
    public readonly IThreadingContext ThreadingContext;
    public readonly IAsynchronousOperationListener Listener;

    public readonly IContextMenuController ContextMenuController;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RootSymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        GoToBaseNavigationService goToBaseNavigationService,
        GoToImplementationNavigationService goToImplementationNavigationService,
        FindReferencesNavigationService findReferencesNavigationService,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        ThreadingContext = threadingContext;
        _workspace = workspace;
        _goToBaseNavigationService = goToBaseNavigationService;
        _goToImplementationNavigationService = goToImplementationNavigationService;
        _findReferencesNavigationService = findReferencesNavigationService;
        Listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);
        NavigationSupport = new(workspace, threadingContext, listenerProvider);

        _updateSourcesQueue = new AsyncBatchingWorkQueue<string>(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            // Ignore case as we're comparing file paths here.
            StringComparer.OrdinalIgnoreCase,
            this.Listener,
            this.ThreadingContext.DisposalToken);

        // Register for workspace changes so that if any documents change, we can update the symbol tree *as long as it
        // has been expanded at least once* to reflect the new state of the document.
        this._workspace.RegisterWorkspaceChangedHandler(
            e =>
            {
                var oldPath = e.OldSolution.GetDocument(e.DocumentId)?.FilePath;
                var newPath = e.NewSolution.GetDocument(e.DocumentId)?.FilePath;

                // Update both the old and new paths.  That way if the path changed, we'll remove the old source and add
                // in the new one.  Note: if the paths are the same, this will just add one entry to the queue as we
                // dedupe based on file path.

                if (oldPath != null)
                    _updateSourcesQueue.AddWork(oldPath);

                if (newPath != null)
                    _updateSourcesQueue.AddWork(newPath);
            },
            options: new WorkspaceEventOptions(RequiresMainThread: false));

        // Register for document open events so that if a document is opened, we can preemptively update the symbol tree
        // even if it was never opened before.  We do this as users expect the root symbol tree arrow to be there or not
        // if the file has no symbols within it.  We don't do this for closed documents as it would be extremely
        // expensive to go to every file and read/parse it.  However, once opened, we're going to do all that work
        // anyways so it is worthwhile to force it to happen.
        this._workspace.RegisterDocumentOpenedHandler(
            e =>
            {
                var filePath = e.Document.FilePath;

                if (filePath == null)
                    return;

                lock (_filePathToCollectionSources)
                {
                    if (_filePathToCollectionSources.TryGetValue(filePath, out var pathSources))
                    {
                        // For each source, go and ensure that the .Items collection is computed.  If the source was
                        // already expanded, this will no-op.  If it was never expanded, this will mark it as being
                        // expanded, and kick off the work back to us (in _updateSourcesQueue) to compute the actual
                        // items.
                        foreach (var source in pathSources)
                            source.EnsureItemsComputed();
                    }
                }
            },
            new WorkspaceEventOptions(RequiresMainThread: false));

        this.ContextMenuController = new SymbolItemContextMenuController(this);
    }

    private async ValueTask UpdateCollectionSourcesAsync(
        ImmutableSegmentedList<string> updatedFilePaths, CancellationToken cancellationToken)
    {
        using var _ = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<RootSymbolTreeItemCollectionSource>.GetInstance(out var sources);

        lock (_filePathToCollectionSources)
        {
            foreach (var filePath in updatedFilePaths)
            {
                if (_filePathToCollectionSources.TryGetValue(filePath, out var pathSources))
                    sources.AddRange(pathSources);
            }
        }

        // Update all the affected documents in parallel.
        await Parallel.ForEachAsync(
            sources,
            cancellationToken,
            async (source, cancellationToken) =>
            {
                await source.UpdateIfEverBeenAskedToComputeAsync(cancellationToken)
                    .ReportNonFatalErrorUnlessCancelledAsync(cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    protected override IAttachedCollectionSource? CreateCollectionSource(IVsHierarchyItem item, string relationshipName)
    {
        if (item == null ||
            item.IsDisposed ||
            item.HierarchyIdentity == null ||
            item.HierarchyIdentity.NestedHierarchy == null ||
            relationshipName != KnownRelationships.Contains)
        {
            return null;
        }

        // Important: currentFilePath is mutable state captured *AND UPDATED* in the local function  
        // OnItemPropertyChanged below.  It allows us to know the file path of the item *prior* to
        // it being changed *when* we hear the update about it having changed (since the event doesn't
        // contain the old value).  
        if (item.CanonicalName is not string currentFilePath)
            return null;

        if (item.HierarchyIdentity.NestedHierarchy.GetGuidProperty(
                item.HierarchyIdentity.NestedItemID,
                (int)__VSHPROPID.VSHPROPID_TypeGuid,
                out var guid) != VSConstants.S_OK)
        {
            return null;
        }

        // We only support this for real files that we'll then have Roslyn Documents for.  All the other
        // things in a project (like folders, nested projects, etc) are not supported.
        if (guid != VSConstants.ItemTypeGuid.PhysicalFile_guid)
            return null;

        var source = new RootSymbolTreeItemCollectionSource(this, item);
        lock (_filePathToCollectionSources)
        {
            _filePathToCollectionSources.MultiAdd(currentFilePath, source);
        }

        // Register to hear about if this hierarchy is disposed. We'll stop watching it if so.
        item.PropertyChanged += OnItemPropertyChanged;

        return source;

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed) && item.IsDisposed)
            {
                // We are notified when the IVsHierarchyItem is removed from the tree via its INotifyPropertyChanged
                // event for the IsDisposed property. When this fires, we remove the filePath->sourcce mapping we're holding.
                lock (_filePathToCollectionSources)
                {
                    _filePathToCollectionSources.MultiRemove(currentFilePath, source);
                }

                item.PropertyChanged -= OnItemPropertyChanged;
            }
            else if (e.PropertyName == nameof(IVsHierarchyItem.CanonicalName))
            {
                var newPath = item.CanonicalName;
                if (newPath != currentFilePath)
                {
                    lock (_filePathToCollectionSources)
                    {

                        // Unlink the oldPath->source mapping, and add a new line for the newPath->source.
                        _filePathToCollectionSources.MultiRemove(currentFilePath, source);
                        _filePathToCollectionSources.MultiAdd(newPath, source);

                        // Keep track of the 'newPath'.
                        currentFilePath = newPath;
                    }

                    // If the filepath changes for an item (which can happen when it is renamed), place a notification
                    // in the queue to update it in the future.  This will ensure all the items presented for it have hte
                    // right document id.  Also reset the state of the source.  The filepath could change to something
                    // no longer valid (like .cs to .txt), or vice versa.
                    source.Reset();
                    _updateSourcesQueue.AddWork(newPath);
                }
            }
        }
    }
}
