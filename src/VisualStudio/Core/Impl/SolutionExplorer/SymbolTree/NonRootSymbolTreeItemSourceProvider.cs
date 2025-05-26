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

        return new NonRootSymbolTreeItemCollectionSource(item);
    }

    private sealed class NonRootSymbolTreeItemCollectionSource(
        SymbolTreeItem symbolTreeItem)
        : IAttachedCollectionSource, ISupportExpansionEvents
    {
        private readonly SymbolTreeItem _symbolTreeItem = symbolTreeItem;

        private readonly BulkObservableCollectionWithInit<SymbolTreeItem> _symbolTreeItems = [];

        private int _expanded = 0;

        public object SourceItem => _symbolTreeItem;
        public bool HasItems => _symbolTreeItem.HasItems;
        public IEnumerable Items => _symbolTreeItems;

        public void BeforeExpand()
        {
            if (Interlocked.CompareExchange(ref _expanded, 1, 0) == 0)
            {
                var provider = _symbolTreeItem.SourceProvider;
                var token = provider.Listener.BeginAsyncOperation(nameof(BeforeExpand));
                var cancellationToken = provider.ThreadingContext.DisposalToken;
                BeforeExpandAsync(cancellationToken)
                    .ReportNonFatalErrorUnlessCancelledAsync(cancellationToken)
                    .CompletesAsyncOperation(token);
            }
        }

        private async Task BeforeExpandAsync(CancellationToken cancellationToken)
        {
            var items = await _symbolTreeItem.ItemProvider.GetItemsAsync(
                _symbolTreeItem, cancellationToken).ConfigureAwait(false);
            foreach (var item in items)
            {
                item.SourceProvider = _symbolTreeItem.SourceProvider;
                item.ItemProvider = _symbolTreeItem.ItemProvider;
                item.DocumentId = _symbolTreeItem.DocumentId;
            }

            using (_symbolTreeItems.GetBulkOperation())
            {
                _symbolTreeItems.Clear();
                _symbolTreeItems.AddRange(items);
            }

            _symbolTreeItems.MarkAsInitialized();
        }

        public void AfterCollapse()
        {
            // No op
        }
    }
}
