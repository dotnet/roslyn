// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.SymbolMonikers;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    using static FindUsagesHelpers;

    internal abstract partial class AbstractFindUsagesService
    {
        private static async Task FindSymbolMonikerReferencesAsync(
            IFindSymbolMonikerUsagesService monikerUsagesService,
            ISymbol definition,
            IFindUsagesContext context,
            CancellationToken cancellationToken)
        {
            if (monikerUsagesService == null)
                return;

            var moniker = SymbolMoniker.TryCreate(definition);
            if (moniker == null)
                return;

            var progress = new StreamingProgressTracker(context.ReportProgressAsync);
            try
            {
                await progress.AddItemsAsync(1).ConfigureAwait(false);

                // TODO: loc this
                var displayParts = GetDisplayParts(definition).Add(
                    new TaggedText(TextTags.Text, " - (external)"));

                var definitionItem = DefinitionItem.CreateNonNavigableItem(
                    tags: GlyphTags.GetTags(definition.GetGlyph()),
                    displayParts,
                    originationParts: DefinitionItem.GetOriginationParts(definition));

                var monikers = ImmutableArray.Create(moniker);
                var currentPage = 0;
                while (true)
                {
                    var keepGoing = await FindSymbolMonikerReferencesAsync(
                        monikerUsagesService, monikers, context,
                        progress, definitionItem, currentPage, cancellationToken).ConfigureAwait(false);
                    if (!keepGoing)
                        break;
                }
            }
            finally
            {
                await progress.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns <c>false</c> when it's time to stop searching.
        /// </summary>
        private static async Task<bool> FindSymbolMonikerReferencesAsync(
            IFindSymbolMonikerUsagesService monikerUsagesService,
            ImmutableArray<SymbolMoniker> monikers,
            IFindUsagesContext context,
            IStreamingProgressTracker progress,
            DefinitionItem definitionItem,
            int currentPage,
            CancellationToken cancellationToken)
        {
            try
            {
                await progress.AddItemsAsync(1).ConfigureAwait(false);

                var results = await monikerUsagesService.FindReferencesByMonikerAsync(
                    definitionItem, monikers, page: currentPage, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (results == null || results.Length == 0)
                    return false;

                if (currentPage == 0)
                {
                    // found some results.  Add the definition item to the context.
                    await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
                }

                currentPage++;

                foreach (var referenceItem in results)
                    await context.OnExternalReferenceFoundAsync(referenceItem).ConfigureAwait(false);

                return true;
            }
            finally
            {
                await progress.ItemCompletedAsync().ConfigureAwait(false);
            }
        }
    }
}
