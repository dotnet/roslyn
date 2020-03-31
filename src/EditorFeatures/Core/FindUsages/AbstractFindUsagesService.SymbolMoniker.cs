// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.LanguageServiceIndexFormat;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

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
            var moniker = SymbolMoniker.TryCreate(definition);
            if (moniker == null)
                return;

            // Let the find-refs window know we have outstanding work
            await using var _ = await context.ProgressTracker.AddSingleItemAsync().ConfigureAwait(false);

            var displayParts = GetDisplayParts(definition).AddRange(new[]
            {
                new TaggedText(TextTags.Space, " "),
                new TaggedText(TextTags.Text, EditorFeaturesResources.external),
            });

            var definitionItem = DefinitionItem.CreateNonNavigableItem(
                tags: GlyphTags.GetTags(definition.GetGlyph()),
                displayParts,
                originationParts: DefinitionItem.GetOriginationParts(definition));

            var monikers = ImmutableArray.Create(moniker);

            var first = true;
            await foreach (var referenceItem in monikerUsagesService.FindReferencesByMoniker(
                definitionItem, monikers, context.ProgressTracker, cancellationToken))
            {
                if (first)
                {
                    // found some results.  Add the definition item to the context.
                    first = false;
                    await context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
                }

                await context.OnExternalReferenceFoundAsync(referenceItem).ConfigureAwait(false);
            }
        }
    }
}
