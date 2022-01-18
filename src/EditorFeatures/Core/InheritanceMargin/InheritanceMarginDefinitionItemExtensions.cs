// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin
{
    internal static class InheritanceMarginDefinitionItemExtensions
    {
        /// <summary>
        /// Create the DefinitionItem based on the numbers of locations for <paramref name="symbol"/>.
        /// If there is only one location, create the DefinitionItem contains only the documentSpan or symbolKey to save memory.
        /// Because in such case, when clicking InheritanceMarginGlpph, it will directly navigate to the symbol.
        /// Otherwise, create the full non-classified DefinitionItem. Because in such case we want to display all the locations to the user
        /// by reusing the FAR window.
        /// </summary>
        public static async Task<DefinitionItem> ToSlimDefinitionItemAsync(this ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var locations = symbol.Locations;
            if (locations.IsEmpty)
            {
                return DefinitionItem.CreateNonNavigableItem(
                    tags: GlyphTags.GetTags(symbol.GetGlyph()),
                    displayParts: FindUsagesHelpers.GetDisplayParts(symbol));
            }

            if (locations.Length > 1)
            {
                return await symbol.ToNonClassifiedDefinitionItemAsync(
                    solution,
                    includeHiddenLocations: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var location = locations[0];
            if (location.IsInMetadata)
            {
                return DefinitionItem.CreateMetadataDefinition(
                    tags: ImmutableArray<string>.Empty,
                    displayParts: ImmutableArray<TaggedText>.Empty,
                    nameDisplayParts: ImmutableArray<TaggedText>.Empty,
                    solution,
                    symbol);
            }
            else if (location.IsInSource && location.IsVisibleSourceLocation())
            {
                var document = solution.GetDocument(location.SourceTree);
                if (document != null)
                {
                    var documentSpan = new DocumentSpan(document, location.SourceSpan);
                    return DefinitionItem.Create(
                        tags: ImmutableArray<string>.Empty,
                        displayParts: ImmutableArray<TaggedText>.Empty,
                        documentSpan,
                        nameDisplayParts: ImmutableArray<TaggedText>.Empty);
                }
            }

            return DefinitionItem.CreateNonNavigableItem(
                tags: GlyphTags.GetTags(symbol.GetGlyph()),
                displayParts: FindUsagesHelpers.GetDisplayParts(symbol));
        }
    }
}
