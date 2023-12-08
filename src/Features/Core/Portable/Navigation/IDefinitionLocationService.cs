// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation;

/// <summary>
/// Service used by "go to definition" and "ctrl-click on symbol" to find the symbol definition location and navigate to
/// it. Specifically, services that do not intend to show any interesting UI for the symbol definition, they just intend
/// to navigate to it.  If richer information is desired (like determining what to display for the symbol name), then 
/// <see cref="INavigableItemsService"/> should be used instead.
/// </summary>
internal interface IDefinitionLocationService : ILanguageService
{
    /// <summary>
    /// If the supplied <paramref name="position"/> is on a code construct with a navigable location, then this
    /// returns that <see cref="INavigableLocation"/>.  The <see cref="TextSpan"/> returned in the span of the
    /// symbol in the code that references that navigable location.  e.g. the full identifier token that the
    /// position is within.
    /// </summary>
    Task<DefinitionLocation?> GetDefinitionLocationAsync(
        Document document, int position, CancellationToken cancellationToken);
}

/// <summary>
/// The result of a <see cref="IDefinitionLocationService.GetDefinitionLocationAsync"/> call.
/// </summary>
/// <param name="Location">The location where the symbol is actually defined at.  Can be used to then navigate to that
/// symbol.
/// </param>
/// <param name="Span">The <see cref="TextSpan"/> returned in the span of the symbol in the code that references that
/// navigable location.  e.g. the full identifier token that the position is within.  Can be used to highlight/underline
/// that text in the document in some fashion.</param>
internal sealed record DefinitionLocation(INavigableLocation Location, DocumentSpan Span);

internal static class DefinitionLocationServiceHelpers
{
    public static async Task<DefinitionLocation?> GetDefinitionLocationFromLegacyImplementationsAsync(
        Document document, int position, Func<CancellationToken, Task<IEnumerable<(Document document, TextSpan sourceSpan)>?>> getNavigableItems, CancellationToken cancellationToken)
    {
        var items = await getNavigableItems(cancellationToken).ConfigureAwait(false);
        if (items is null)
            return null;

        var firstItem = items.FirstOrNull();
        if (firstItem is null)
            return null;

        var navigableItem = await new DocumentSpan(firstItem.Value.document, firstItem.Value.sourceSpan).GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
        if (navigableItem is null)
            return null;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return new DefinitionLocation(navigableItem, GetDocumentSpan());

        DocumentSpan GetDocumentSpan()
        {
            // To determine the span to underline, just use a simple heuristic of expanding out to the surrounding
            // letters and numbers.  F#/TS can reimplement this in the future to be more accurate to their language.

            var startPosition = position;
            var endPosition = position + 1;

            if (char.IsLetterOrDigit(text[position]))
            {
                while (char.IsLetterOrDigit(GetChar(startPosition - 1)))
                    startPosition--;

                while (char.IsLetterOrDigit(GetChar(endPosition + 1)))
                    endPosition++;
            }

            return new DocumentSpan(document, TextSpan.FromBounds(startPosition, endPosition));
        }

        char GetChar(int position)
            => position >= 0 && position < text.Length ? text[position] : (char)0;
    }
}
