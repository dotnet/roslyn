// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// Responsible for taking search result items and parenting them with their corresponding real document
/// (i.e. an IVhHierarchy + itemid) in the solution explorer window.  In other words, this source provider
/// providers the "contained by" relation, mapping <see cref="SolutionExplorerSearchDisplayItem"/> to
/// <see cref="IVsHierarchyItem"/>.
/// </summary>
[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(SolutionExplorerSearchDisplayItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class SolutionExplorerSearchDisplayItemSourceProvider(
    VisualStudioWorkspace workspace,
    IVsHierarchyItemManager hierarchyItemManager)
    : AttachedCollectionSourceProvider<SolutionExplorerSearchDisplayItem>
{
    protected override IAttachedCollectionSource? CreateCollectionSource(
        SolutionExplorerSearchDisplayItem item, string relationshipName)
    {
        if (relationshipName != KnownRelationships.ContainedBy)
            return null;

        var document = workspace.CurrentSolution.GetDocument(item.Result.NavigableItem.Document.Id);
        if (document is null)
            return null;

        if (!VisualStudioWorkspaceUtilities.TryGetVsHierarchyItem(
                hierarchyItemManager, document, out var hierarchyItem))
        {
            return null;
        }

        return new SolutionExplorerSearchDisplayItemCollectionSource(item, hierarchyItem);
    }

    private sealed class SolutionExplorerSearchDisplayItemCollectionSource(
        SolutionExplorerSearchDisplayItem item, IVsHierarchyItem hierarchyItem) : IAttachedCollectionSource
    {
        public object SourceItem => item;
        public bool HasItems => true;
        public IEnumerable Items => ImmutableArray.Create(hierarchyItem);
    }
}
