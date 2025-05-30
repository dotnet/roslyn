// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

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
    private readonly ConcurrentDictionary<IVsHierarchyItem, RootSymbolTreeItemCollectionSource> _hierarchyToCollectionSource = [];

    /// <summary>
    /// Queue of notifications we've heard about for changed document file paths.  We'll then go update the
    /// symbol tree item for each of these documents so that it is up to date.  Note: if the symbol tree has
    /// never been expanded,  this will bail immediately to avoid doing unnecessary work.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<string> _updateSourcesQueue;
    private readonly Workspace _workspace;

    public readonly SolutionExplorerNavigationSupport NavigationSupport;
    public readonly IThreadingContext ThreadingContext;
    public readonly IAsynchronousOperationListener Listener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RootSymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        ThreadingContext = threadingContext;
        _workspace = workspace;
        Listener = listenerProvider.GetListener(FeatureAttribute.SolutionExplorer);
        NavigationSupport = new(workspace, threadingContext, listenerProvider);

        _updateSourcesQueue = new AsyncBatchingWorkQueue<string>(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            // Ignore case as we're comparing file paths here.
            StringComparer.OrdinalIgnoreCase,
            this.Listener,
            this.ThreadingContext.DisposalToken);

        this._workspace.RegisterWorkspaceChangedHandler(
            e =>
            {
                var oldPath = e.OldSolution.GetDocument(e.DocumentId)?.FilePath;
                var newPath = e.NewSolution.GetDocument(e.DocumentId)?.FilePath;

                if (oldPath != null)
                    _updateSourcesQueue.AddWork(oldPath);

                if (newPath != null)
                    _updateSourcesQueue.AddWork(newPath);
            },
            options: new WorkspaceEventOptions(RequiresMainThread: false));
    }

    private async ValueTask UpdateCollectionSourcesAsync(
        ImmutableSegmentedList<string> updatedFilePaths, CancellationToken cancellationToken)
    {
        using var _1 = SharedPools.StringIgnoreCaseHashSet.GetPooledObject(out var filePathSet);
        using var _2 = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<RootSymbolTreeItemCollectionSource>.GetInstance(out var sources);

        filePathSet.AddRange(updatedFilePaths);
        sources.AddRange(_hierarchyToCollectionSource.Values);

        // Update all the affected documents in parallel.
        await RoslynParallel.ForEachAsync(
            sources,
            cancellationToken,
            async (source, cancellationToken) =>
            {
                await source.UpdateIfAffectedAsync(filePathSet, cancellationToken)
                    .ReportNonFatalErrorUnlessCancelledAsync(cancellationToken)
                    .ConfigureAwait(false);
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

        var filePath = item.CanonicalName;

        // We only support C# and VB files for now.  This ensures we don't create source providers for
        // other types of files we'll never have results for.
        var extension = Path.GetExtension(filePath);
        if (extension is not ".cs" and not ".vb")
            return null;

        var source = new RootSymbolTreeItemCollectionSource(this, item);
        _hierarchyToCollectionSource[item] = source;

        // Register to hear about if this hierarchy is disposed. We'll stop watching it if so.
        item.PropertyChanged += OnItemPropertyChanged;

        return source;

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed) && item.IsDisposed)
            {
                // We are notified when the IVsHierarchyItem is removed from the tree via its INotifyPropertyChanged
                // event for the IsDisposed property. When this fires, we remove the item->sourcce mapping we're holding.
                _hierarchyToCollectionSource.TryRemove(item, out _);
                item.PropertyChanged -= OnItemPropertyChanged;
            }
            //else if (e.PropertyName == nameof(IVsHierarchyItem.CanonicalName))
            //{
            //    // Name of the file changed.  Clear out the cached document id for it so it is recomputed.
            //    source.FilePath = item.CanonicalName;
            //}
        }
    }
}
