// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal class WrapItemsAction : DocumentChangeAction
    {
        // Keeps track of the invoked code actions.  That way we can prioritize those code actions 
        // in the future since they're more likely the ones the user wants.  This is important as 
        // we have 9 different code actions offered (3 major groups, with 3 actions per group).  
        // It's likely the user will just pick from a few of these. So we'd like the ones they
        // choose to be prioritized accordingly.
        private static ImmutableArray<string> s_mruTitles = ImmutableArray<string>.Empty;

        public string ParentTitle { get; }

        public string SortTitle { get; }

        // Make our code action low priority.  This option will be offered *a lot*, and 
        // much of  the time will not be something the user particularly wants to do.  
        // It should be offered after all other normal refactorings.
        //
        // This value is only relevant if this code action is the only one in its group,
        // and it ends up getting inlined as a top-level-action that is offered.
        internal override CodeActionPriority Priority => CodeActionPriority.Low;

        public WrapItemsAction(string title, string parentTitle, Func<CancellationToken, Task<Document>> createChangedDocument)
            : base(title, createChangedDocument)
        {
            ParentTitle = parentTitle;
            SortTitle = parentTitle + "_" + title;
        }

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

            operationsList.Add(new RecordCodeActionOperation(this.SortTitle, this.ParentTitle));
            return operationsList;
        }

        public static ImmutableArray<CodeAction> SortActionsByMostRecentlyUsed(ImmutableArray<CodeAction> codeActions)
        {
            // make a local so this array can't change out from under us.
            var mruTitles = s_mruTitles;
            return codeActions.Sort((ca1, ca2) =>
            {
                var titleIndex1 = mruTitles.IndexOf(GetSortTitle(ca1));
                var titleIndex2 = mruTitles.IndexOf(GetSortTitle(ca2));

                if (titleIndex1 >= 0 && titleIndex2 >= 0)
                {
                    // we've invoked both of these before.  Order by how recently it was invoked.
                    return titleIndex1 - titleIndex2;
                }

                // one of these has never been invoked.  It's always after an item that has been
                // invoked.
                if (titleIndex1 >= 0)
                {
                    return -1;
                }

                if (titleIndex2 >= 0)
                {
                    return 1;
                }

                // Neither of these has been invoked.   Keep it in the same order we found it in the
                // array.  Note: we cannot return 0 here as ImmutableArray/Array are not guaranteed
                // to sort stably.
                return codeActions.IndexOf(ca1) - codeActions.IndexOf(ca2);
            });
        }

        private static string GetSortTitle(CodeAction codeAction)
            => (codeAction as WrapItemsAction)?.SortTitle ?? codeAction.Title;

        private class RecordCodeActionOperation : CodeActionOperation
        {
            private readonly string _sortTitle;
            private readonly string _parentTitle;

            public RecordCodeActionOperation(string sortTitle, string parentTitle)
            {
                _sortTitle = sortTitle;
                _parentTitle = parentTitle;
            }

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
