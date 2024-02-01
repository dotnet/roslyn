// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal sealed partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Snapshot of all state used to compute and present our models. Used when we produce new models to both
        /// transfer state over from the old ones, and to efficiently tell what is different between the last results
        /// and the new ones.
        /// </summary>
        private sealed class DocumentOutlineViewState
        {
            /// <summary>
            /// The snapshot of the document used to compute this state.
            /// </summary>
            public readonly ITextSnapshot TextSnapshot;

            /// <summary>
            /// The text string that was used to filter the original LSP results down to the set of <see
            /// cref="DocumentSymbolData"/> we have.
            /// </summary>
            public readonly string SearchText;

            /// <summary>
            /// The view items we created from <see cref="DocumentSymbolData"/>.  Note: these values are a bit odd in
            /// that they represent mutable UI state.  Docs on DocumentSymbolDataViewModel indicate that it likely
            /// should be as the only mutable state is <see cref="DocumentSymbolDataViewModel.IsExpanded"/>/<see
            /// cref="DocumentSymbolDataViewModel.IsSelected"/>, both of which are threadsafe.
            /// </summary>
            public readonly ImmutableArray<DocumentSymbolDataViewModel> ViewModelItems;

            /// <summary>
            /// Interval-tree view over <see cref="ViewModelItems"/> so that we can quickly determine which of them
            /// intersect with a particular position in the document.
            /// </summary>
            public readonly IntervalTree<DocumentSymbolDataViewModel> ViewModelItemsTree;

            public DocumentOutlineViewState(
                ITextSnapshot textSnapshot,
                string searchText,
                ImmutableArray<DocumentSymbolDataViewModel> viewModelItems,
                IntervalTree<DocumentSymbolDataViewModel> viewModelItemsTree)
            {
                TextSnapshot = textSnapshot;
                SearchText = searchText;
                ViewModelItems = viewModelItems;
                ViewModelItemsTree = viewModelItemsTree;
            }
        }
    }
}
