// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolMonikers;
using Roslyn.Utilities;
using VS.IntelliNav.Contracts;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    using static FindUsagesHelpers;

    internal abstract partial class AbstractFindUsagesService
    {
        private static async Task FindCodeIndexReferencesAsync(
            ICodeIndexProvider? codeIndexProvider,
            ISymbol definition,
            IFindUsagesContext context,
            CancellationToken cancellationToken)
        {
            if (codeIndexProvider == null)
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
                    GlyphTags.GetTags(definition.GetGlyph()),
                    displayParts,
                    originationParts: ImmutableArray.Create(new TaggedText(TextTags.Text, "external")));

                var monikers = SpecializedCollections.SingletonEnumerable(moniker);
                var currentPage = 0;
                while (true)
                {
                    var keepGoing = await FindCodeIndexReferencesAsync(
                        codeIndexProvider, monikers, context,
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
        private static async Task<bool> FindCodeIndexReferencesAsync(
            ICodeIndexProvider codeIndexProvider,
            IEnumerable<ISymbolMoniker> monikers,
            IFindUsagesContext context,
            IStreamingProgressTracker progress,
            DefinitionItem definitionItem,
            int currentPage,
            CancellationToken cancellationToken)
        {
            try
            {
                await progress.AddItemsAsync(1).ConfigureAwait(false);

                var results = await codeIndexProvider.FindReferencesByMonikerAsync(
                    monikers, includeDecleration: true, pageIndex: currentPage, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (results == null || results.Count == 0)
                    return false;

                if (currentPage == 0)
                {
                    // found some results.  Add the definition item to the context.
                    await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
                }

                currentPage++;
                await ProcessCodeIndexResultsAsync(
                    codeIndexProvider, context, definitionItem, results, cancellationToken).ConfigureAwait(false);
                return true;
            }
            finally
            {
                await progress.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        private static async Task ProcessCodeIndexResultsAsync(
            ICodeIndexProvider codeIndexProvider,
            IFindUsagesContext context,
            DefinitionItem definitionItem,
            ICollection<string> results,
            CancellationToken cancellationToken)
        {
            foreach (var result in results)
            {
                var referenceItem = new ExternalReferenceItem(
                    definitionItem, null, null, null, null);
                await context.OnExternalReferenceFoundAsync(referenceItem).ConfigureAwait(false);
            }
        }
    }
}
