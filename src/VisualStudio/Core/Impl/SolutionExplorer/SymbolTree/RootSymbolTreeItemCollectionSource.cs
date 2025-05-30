// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionExplorer;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class RootSymbolTreeItemSourceProvider
{
    private sealed class RootSymbolTreeItemCollectionSource(
        RootSymbolTreeItemSourceProvider rootProvider,
        IVsHierarchyItem hierarchyItem,
        string itemName) : IAttachedCollectionSource, INotifyPropertyChanged
    {
        private readonly RootSymbolTreeItemSourceProvider _rootProvider = rootProvider;
        private readonly IVsHierarchyItem _hierarchyItem = hierarchyItem;

        // Mark hasItems as null as we don't know up front if we have items, and instead have to compute it on demand.
        private readonly SymbolTreeChildCollection _childCollection = new(rootProvider, hierarchyItem, hasItems: null);

        /// <summary>
        /// Whether or not this root solution explorer node has been expanded or not.  Until it is first expanded,
        /// we do no work so as to avoid CPU time and rooting things like syntax nodes.
        /// </summary>
        private volatile int _initialized;

        public string ItemName { get; set; } = itemName;

        public async Task UpdateIfAffectedAsync(
            HashSet<string>? updatedFilePaths,
            CancellationToken cancellationToken)
        {
            // If we haven't been initialized yet, then we don't have to do anything.  We will get called again
            // in the future as documents are mutated, and we'll ignore until the point that the user has at
            // least expanded this node once.
            if (_initialized == 0)
                return;

            if (updatedFilePaths != null && !updatedFilePaths.Contains(this.ItemName))
                return;

            var documentId = DetermineDocumentId();

            var solution = _rootProvider._workspace.CurrentSolution;

            var document = solution.GetDocument(documentId);
            var itemProvider = document?.GetLanguageService<ISolutionExplorerSymbolTreeItemProvider>();

            if (document != null && itemProvider != null)
            {
                // Compute the items on the BG.
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var items = itemProvider.GetItems(root, cancellationToken);

                // Then switch to the UI thread to actually update the collection.
                await _rootProvider.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _childCollection.SetItemsAndMarkComputed_OnMainThread(document.Id, itemProvider, items);
            }
            else
            {
                // If we can't find this document anymore, clear everything out.
                await _rootProvider.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _childCollection.ClearAndMarkComputed_OnMainThread();
            }
        }

        private DocumentId? DetermineDocumentId()
        {
            var idMap = _rootProvider._workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();
            if (idMap.TryGetProject(_hierarchyItem.Parent, targetFrameworkMoniker: null, out var project))
            {
                var documentIds = project.Solution.GetDocumentIdsWithFilePath(this.ItemName);
                return documentIds.FirstOrDefault(static (d, projectId) => d.ProjectId == projectId, project.Id);
            }

            return null;
        }

        object IAttachedCollectionSource.SourceItem => _childCollection.SourceItem;

        bool IAttachedCollectionSource.HasItems => _childCollection.HasItems;

        IEnumerable IAttachedCollectionSource.Items
        {
            get
            {
                if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
                {
                    // This was the first time this node was expanded.  Kick off the initial work to 
                    // compute the items for it.
                    var token = _rootProvider.Listener.BeginAsyncOperation(nameof(IAttachedCollectionSource.Items));
                    UpdateIfAffectedAsync(updatedFilePaths: null, _rootProvider.ThreadingContext.DisposalToken)
                        .ReportNonFatalErrorAsync()
                        .CompletesAsyncOperation(token);
                }

                return _childCollection.Items;
            }
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => _childCollection.PropertyChanged += value;
            remove => _childCollection.PropertyChanged -= value;
        }
    }
}
