// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

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

    private readonly AsyncBatchingWorkQueue _updateSourcesQueue;

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

        _updateSourcesQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Medium,
            UpdateCollectionSourcesAsync,
            listenerProvider.GetListener(FeatureAttribute.SolutionExplorer),
            _threadingContext.DisposalToken);

        _workspace.RegisterWorkspaceChangedHandler(
            e => _updateSourcesQueue.AddWork(cancelExistingWork: true),
            options: new WorkspaceEventOptions(RequiresMainThread: false));
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
        public object SourceItem => hierarchyItem;

        public bool HasItems => true;

        public IEnumerable Items => Array.Empty<SymbolTreeItem>();
    }

    private sealed class SymbolTreeItem() : BaseItem(nameof(SymbolTreeItem))
    {

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
