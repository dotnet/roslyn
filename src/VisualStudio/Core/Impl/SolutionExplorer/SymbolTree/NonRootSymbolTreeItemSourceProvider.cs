// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// Provides a collection source for getting the children of a given SymbolTreeItem on demand.  This can be done 
/// trivially as hold onto to the syntax node that they were created for.  Note: if the root item is not expanded
/// then no actual calls into the syntax model are done to avoid creating parse trees unnecessarily.
/// </summary>
[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(NonRootSymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class NonRootSymbolTreeItemSourceProvider() : AttachedCollectionSourceProvider<SymbolTreeItem>
{
    protected override IAttachedCollectionSource? CreateCollectionSource(SymbolTreeItem item, string relationshipName)
    {
        if (relationshipName != KnownRelationships.Contains)
            return null;

        // A SymbolTreeItem is its own collection source.  In other words, it points at its own children
        // and can be queried directly for them.
        return item;
    }
}
