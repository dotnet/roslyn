// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat;
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
            IFindUsagesContext context)
        {
            var moniker = SymbolMoniker.TryCreate(definition);
            if (moniker == null)
                return;

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
            await foreach (var referenceItem in monikerUsagesService.FindReferencesByMonikerAsync(definitionItem, monikers, context.CancellationToken))
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
