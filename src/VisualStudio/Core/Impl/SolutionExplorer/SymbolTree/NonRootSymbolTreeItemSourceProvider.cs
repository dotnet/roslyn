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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Microsoft.VisualStudio.LanguageServices.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[Export(typeof(IAttachedCollectionSourceProvider))]
[Name(nameof(NonRootSymbolTreeItemSourceProvider))]
[Order(Before = HierarchyItemsProviderNames.Contains)]
[AppliesToProject("CSharp | VB")]
internal sealed class NonRootSymbolTreeItemSourceProvider : AbstractSymbolTreeItemSourceProvider<SymbolTreeItem>
{
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    [ImportingConstructor]
    public NonRootSymbolTreeItemSourceProvider(
        IThreadingContext threadingContext,
        VisualStudioWorkspace workspace,
        IAsynchronousOperationListenerProvider listenerProvider
    /*,
[Import(typeof(AnalyzersCommandHandler))] IAnalyzersCommandHandler commandHandler*/)
        : base(threadingContext, workspace, listenerProvider)
    {
 
    }

    protected override IAttachedCollectionSource? CreateCollectionSource(SymbolTreeItem item, string relationshipName)
    {
        if (relationshipName != KnownRelationships.Contains)
            return null;

        // A SymbolTreeItem is its own collection source.  In other words, it points at its own children
        // and can be queried directly for them.
        return item;
    }

    //private sealed class NonRootSymbolTreeItemCollectionSource(
    //    RootSymbolTreeItemSourceProvider rootProvider,
    //    SymbolTreeItem parentItem)
    //    : AbstractSymbolTreeItemCollectionSource<SymbolTreeItem>(
    //        rootProvider, parentItem), ISupportExpansionEvents
    //{
    //    private int _expanded = 0;


    //    public void AfterCollapse()
    //    {
    //        // No op
    //    }
    //}
}
