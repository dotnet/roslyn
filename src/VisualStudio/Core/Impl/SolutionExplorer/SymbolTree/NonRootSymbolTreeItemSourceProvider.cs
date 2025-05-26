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

        return null;
    }

    private sealed class SymbolTreeItemCollectionSource(
        RootSymbolTreeItemSourceProvider provider,
        SymbolTreeItem symbolTreeItem)
        : IAttachedCollectionSource
    {
        private readonly RootSymbolTreeItemSourceProvider _provider = provider;
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

            var solution = _provider.Workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);

            // If we can't find this document anymore, clear everything out.
            if (document is null)
                return false;

            var service = document.Project.GetLanguageService<ISolutionExplorerSymbolTreeItemProvider>();
            if (service is null)
                return false;

            var items = await service.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            foreach (var item in items)
            {
                item.Provider = _provider;
                item.DocumentId = document.Id;
            }

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
}
