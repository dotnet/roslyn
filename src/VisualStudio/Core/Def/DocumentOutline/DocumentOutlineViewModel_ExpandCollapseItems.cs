// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Queue to expand/collapse the items in the tree view
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ExpansionOption> _expandCollapseQueue;

        public void EnqueueExpandCollapseUpdate(ExpansionOption option)
            => _expandCollapseQueue.AddWork(option, cancelExistingWork: true);

        private async ValueTask ExpandCollapseItemsAsync(ImmutableSegmentedList<ExpansionOption> expansionOptions, CancellationToken token)
        {
            var expansionOption = expansionOptions.Last();

            // guard here as we are going to be modifying properties on the view models
            // even though they are bools and this is safe to do without a guard it makes things happen 'in order' for the user
            using (await _guard.DisposableWaitAsync(token).ConfigureAwait(false))
            {
                var documentSymbolViewModelItems = _documentSymbolViewModelItems;
                DocumentOutlineHelper.SetExpansionOption(documentSymbolViewModelItems, expansionOption);
            }
        }
    }
}
