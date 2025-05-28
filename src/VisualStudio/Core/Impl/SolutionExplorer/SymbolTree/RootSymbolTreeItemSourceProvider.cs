// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(RootSymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
internal sealed partial class RootSymbolTreeItemSourceProvider : AttachedCollectionSourceProvider<IVsHierarchyItem>
{
    private readonly ConcurrentDictionary<IVsHierarchyItem, RootSymbolTreeItemCollectionSource> _hierarchyToCollectionSource = [];

    private readonly AsyncBatchingWorkQueue<DocumentId> _updateSourcesQueue;
    private readonly Workspace _workspace;
    public readonly SymbolTreeNavigationSupport NavigationSupport;

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
        NavigationSupport = new(workspace, threadingContext, Listener);

        _updateSourcesQueue = new AsyncBatchingWorkQueue<DocumentId>(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            EqualityComparer<DocumentId>.Default,
            this.Listener,
            this.ThreadingContext.DisposalToken);

        this._workspace.RegisterWorkspaceChangedHandler(
            e =>
            {
                if (e.DocumentId != null)
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
        sources.AddRange(_hierarchyToCollectionSource.Values);

        await RoslynParallel.ForEachAsync(
            sources,
            cancellationToken,
            async (source, cancellationToken) =>
            {
                await source.UpdateIfAffectedAsync(documentIdSet, cancellationToken)
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

        if (!hierarchy.TryGetCanonicalName(itemId, out var itemName))
            return null;

        // We only support C# and VB files for now.  This ensures we don't create source providers for
        // other types of files we'll never have results for.
        var extension = Path.GetExtension(itemName);
        if (extension is not ".cs" and not ".vb")
            return null;

        var source = new RootSymbolTreeItemCollectionSource(this, item);
        _hierarchyToCollectionSource[item] = source;

        item.PropertyChanged += OnItemPropertyChanged;

        return source;

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // We are notified when the IVsHierarchyItem is removed from the tree via its INotifyPropertyChanged
            // event for the IsDisposed property. When this fires, we remove the item->sourcce mapping we're holding.
            if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed) && item.IsDisposed)
            {
                _hierarchyToCollectionSource.TryRemove(item, out _);
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }
    }
}
