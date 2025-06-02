﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionExplorer;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class RootSymbolTreeItemSourceProvider
{
    private sealed class RootSymbolTreeItemCollectionSource(
        RootSymbolTreeItemSourceProvider rootProvider,
        IVsHierarchyItem hierarchyItem) : IAttachedCollectionSource, INotifyPropertyChanged
    {
        private readonly RootSymbolTreeItemSourceProvider _rootProvider = rootProvider;
        private readonly IVsHierarchyItem _hierarchyItem = hierarchyItem;

        // Mark hasItems as null as we don't know up front if we have items, and instead have to compute it on demand.
        private readonly SymbolTreeChildCollection _childCollection = new(
            rootProvider, hierarchyItem, hasItemsDefault: GetHasItemsDefaultValue(hierarchyItem));

        /// <summary>
        /// Whether or not this root solution explorer node has been expanded or not.  Until it is first expanded,
        /// we do no work so as to avoid CPU time and rooting things like syntax nodes.
        /// </summary>
        private volatile int _hasEverBeenExpanded;

        private static bool? GetHasItemsDefaultValue(IVsHierarchyItem hierarchyItem)
            // If this is not a c#/vb file initially, then mark this file as having no symbolic children.
            // If it is c#/vb, then mark it as null (which means 'unknown') so that we show the arrow next
            // to the item, but compute only once expanded.
            => Path.GetExtension(hierarchyItem.CanonicalName).ToLowerInvariant() is ".cs" or ".vb"
                ? null
                : false;

        public void Reset()
        {
            _rootProvider.ThreadingContext.ThrowIfNotOnUIThread();
            _childCollection.ResetToUncomputedState(GetHasItemsDefaultValue(_hierarchyItem));

            // Note: we intentionally do not touch _hasEverBeenExpanded.  The platform only ever calls "Items"
            // at most once (even if we notify that it changed). So if we reset _hasEverBeenExpanded to 0, then
            // it will never leave that state from that point on, and we'll be stuck in an invalid state.
        }

        public async Task UpdateIfEverExpandedAsync(CancellationToken cancellationToken)
        {
            // If we haven't been initialized yet, then we don't have to do anything.  We will get called again
            // in the future as documents are mutated, and we'll ignore until the point that the user has at
            // least expanded this node once.
            if (_hasEverBeenExpanded == 0)
                return;

            // Try to find a roslyn document for this file path.  Note: it is intentional that we continue onwards,
            // even if this returns null.  We still want to put ourselves into the final "i have no items" state,
            // instead of bailing out and potentially leaving either stale items, or leaving ourselves in the 
            // "i don't know what items are in me" state.
            var documentId = DetermineDocumentId();

            var solution = _rootProvider._workspace.CurrentSolution;

            var document = solution.GetDocument(documentId);
            var itemProvider = document?.GetLanguageService<ISolutionExplorerSymbolTreeItemProvider>();

            if (document != null && itemProvider != null)
            {
                // Compute the items on the BG.
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var items = itemProvider.GetItems(document.Id, root, cancellationToken);

                // Then switch to the UI thread to actually update the collection.
                await _rootProvider.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _childCollection.SetItemsAndMarkComputed_OnMainThread(itemProvider, items);
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
            var filePath = TryGetCanonicalName();

            if (filePath != null)
            {
                var idMap = _rootProvider._workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();
                if (idMap.TryGetProject(_hierarchyItem.Parent, targetFrameworkMoniker: null, out var project))
                {
                    var documentIds = project.Solution.GetDocumentIdsWithFilePath(filePath);
                    return documentIds.FirstOrDefault(static (d, projectId) => d.ProjectId == projectId, project.Id);
                }
            }

            return null;

            string? TryGetCanonicalName()
            {
                // Quick check that will be correct the majority of the time.
                if (!_hierarchyItem.IsDisposed)
                {
                    // We are running in the background.  So it's possible that the type may be disposed between
                    // the above check and retrieving the canonical name.  So have to guard against that just in case.
                    try
                    {
                        return _hierarchyItem.CanonicalName;
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                return null;
            }
        }

        object IAttachedCollectionSource.SourceItem => _childCollection.SourceItem;

        bool IAttachedCollectionSource.HasItems => _childCollection.HasItems;

        IEnumerable IAttachedCollectionSource.Items
        {
            get
            {
                if (Interlocked.CompareExchange(ref _hasEverBeenExpanded, 1, 0) == 0)
                {
                    // This was the first time this node was expanded.  Kick off the initial work to 
                    // compute the items for it.
                    _rootProvider._updateSourcesQueue.AddWork(_hierarchyItem.CanonicalName);
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
