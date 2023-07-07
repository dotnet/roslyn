// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.Wrapping
{
    /// <summary>
    /// Code action for actually wrapping items.  Provided as a special subclass because it will
    /// also update the wrapping most-recently-used list when the code action is actually
    /// invoked.
    /// </summary>
    internal class WrapItemsAction(string title, string parentTitle, Func<CancellationToken, Task<Document>> createChangedDocument) : DocumentChangeAction(title, createChangedDocument, title, CodeActionPriority.Low)
    {
        // Keeps track of the invoked code actions.  That way we can prioritize those code actions 
        // in the future since they're more likely the ones the user wants.  This is important as 
        // we have 9 different code actions offered (3 major groups, with 3 actions per group).  
        // It's likely the user will just pick from a few of these. So we'd like the ones they
        // choose to be prioritized accordingly.
        private static ImmutableArray<string> s_mruTitles = ImmutableArray<string>.Empty;

        public string ParentTitle { get; } = parentTitle;

        public string SortTitle { get; } = parentTitle + "_" + title;

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            // For preview, we don't want to compute the normal operations.  Specifically, we don't
            // want to compute the stateful operation that tracks which code action was triggered.
            return base.ComputeOperationsAsync(cancellationToken);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            var operations = await base.ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);
            var operationsList = operations.ToList();

            operationsList.Add(new RecordCodeActionOperation(SortTitle, ParentTitle));
            return operationsList;
        }

        public static ImmutableArray<CodeAction> SortActionsByMostRecentlyUsed(ImmutableArray<CodeAction> codeActions)
            => SortByMostRecentlyUsed(codeActions, s_mruTitles, GetSortTitle);

        public static ImmutableArray<T> SortByMostRecentlyUsed<T>(
            ImmutableArray<T> items, ImmutableArray<string> mostRecentlyUsedKeys, Func<T, string> getKey)
        {
            return items.Sort((d1, d2) =>
            {
                var mruIndex1 = mostRecentlyUsedKeys.IndexOf(getKey(d1));
                var mruIndex2 = mostRecentlyUsedKeys.IndexOf(getKey(d2));

                // If both are in the mru, prefer the one earlier on.
                if (mruIndex1 >= 0 && mruIndex2 >= 0)
                    return mruIndex1 - mruIndex2;

                // if either is in the mru, and the other is not, then the mru item is preferred.
                if (mruIndex1 >= 0)
                    return -1;

                if (mruIndex2 >= 0)
                    return 1;

                // Neither are in the mru.  Sort them based on their original locations.
                var index1 = items.IndexOf(d1);
                var index2 = items.IndexOf(d2);

                // Note: we don't return 0 here as ImmutableArray.Sort is not stable.
                return index1 - index2;
            });
        }

        private static string GetSortTitle(CodeAction codeAction)
            => (codeAction as WrapItemsAction)?.SortTitle ?? codeAction.Title;

        private class RecordCodeActionOperation(string sortTitle, string parentTitle) : CodeActionOperation
        {
            private readonly string _sortTitle = sortTitle;
            private readonly string _parentTitle = parentTitle;

            internal override bool ApplyDuringTests => false;

            public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            {
                // Record both the sortTitle of the nested action and the tile of the parent
                // action.  This way we any invocation of a code action helps prioritize both
                // the parent lists and the nested lists.
                s_mruTitles = s_mruTitles.Remove(_sortTitle).Remove(_parentTitle)
                                         .Insert(0, _sortTitle).Insert(0, _parentTitle);
            }
        }
    }
}
