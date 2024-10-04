// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets;

internal static class RoslynLSPSnippetConverter
{
    /// <summary>
    /// Extends the TextChange to encompass all placeholder positions as well as caret position.
    /// Generates a LSP formatted snippet from a TextChange, list of placeholders, and caret position.
    /// </summary>
    public static async Task<string> GenerateLSPSnippetAsync(Document document, int caretPosition, ImmutableArray<SnippetPlaceholder> placeholders, TextChange textChange, int triggerLocation, CancellationToken cancellationToken)
    {
        var extendedTextChange = await ExtendSnippetTextChangeAsync(document, textChange, placeholders, caretPosition, triggerLocation, cancellationToken).ConfigureAwait(false);
        return ConvertToLSPSnippetString(extendedTextChange, placeholders, caretPosition);
    }

    /// <summary>
    /// Iterates through every index in the snippet string and determines where the
    /// LSP formatted chunks should be inserted for each placeholder.
    /// </summary>
    private static string ConvertToLSPSnippetString(TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition)
    {
        var textChangeStart = textChange.Span.Start;
        var textChangeText = textChange.NewText;
        Contract.ThrowIfNull(textChangeText);

        using var _1 = PooledStringBuilder.GetInstance(out var lspSnippetString);
        using var _2 = PooledDictionary<int, (string text, int priority)>.GetInstance(out var dictionary);
        PopulateMapOfSpanStartsToLSPStringItem(dictionary, placeholders, textChangeStart);

        // Need to go through the length + 1 since caret positions occur before and after the
        // character position.
        // If there is a caret at the end of the line, then it's position
        // will be equivalent to the length of the TextChange.
        for (var i = 0; i < textChange.Span.Length + 1;)
        {
            if (i == caretPosition - textChangeStart)
            {
                lspSnippetString.Append("$0");
            }

            // Tries to see if a value exists at that position in the map, and if so it
            // generates a string that is LSP formatted.
            if (dictionary.TryGetValue(i, out var placeholderInfo))
            {
                var str = $"${{{placeholderInfo.priority}:{placeholderInfo.text}}}";
                lspSnippetString.Append(str);

                // Skip past the entire identifier in the TextChange text
                i += placeholderInfo.text.Length;
            }
            else
            {
                if (i < textChangeText.Length)
                {
                    lspSnippetString.Append(textChangeText[i]);
                    i++;
                }
                else
                {
                    break;
                }
            }
        }

        return lspSnippetString.ToString();
    }

    /// <summary>
    /// Preprocesses the list of placeholders into a dictionary that maps the insertion position
    /// in the string to the placeholder's identifier and the priority associated with it.
    /// </summary>
    private static void PopulateMapOfSpanStartsToLSPStringItem(Dictionary<int, (string identifier, int priority)> dictionary, ImmutableArray<SnippetPlaceholder> placeholders, int textChangeStart)
    {
        for (var i = 0; i < placeholders.Length; i++)
        {
            var placeholder = placeholders[i];
            foreach (var position in placeholder.StartingPositions)
            {
                // i + 1 since the placeholder priority is set according to the index in the
                // placeholders array, starting at 1.
                // We should never be adding two placeholders in the same position since identifiers
                // must have a length greater than 0.
                dictionary.Add(position - textChangeStart, (placeholder.Text, i + 1));
            }
        }
    }

    /// <summary>
    /// We need to extend the snippet's TextChange if any of the placeholders or
    /// if the caret position comes before or after the span of the TextChange.
    /// If so, then find the new string that encompasses all of the placeholders
    /// and caret position.
    /// This is important for the cases in which the document does not determine the TextChanges from
    /// the original document accurately.
    /// </summary>
    private static async Task<TextChange> ExtendSnippetTextChangeAsync(Document document, TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition, int triggerLocation, CancellationToken cancellationToken)
    {
        var extendedSpan = GetUpdatedTextSpan(textChange, placeholders, caretPosition, triggerLocation);
        var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var newString = documentText.ToString(extendedSpan);
        var newTextChange = new TextChange(extendedSpan, newString);

        return newTextChange;
    }

    /// <summary>
    /// Iterates through the placeholders and determines if any of the positions
    /// come before or after what is indicated by the snippet's TextChange.
    /// If so, adjust the starting and ending position accordingly.
    /// </summary>
    private static TextSpan GetUpdatedTextSpan(TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition, int triggerLocation)
    {
        var textChangeText = textChange.NewText;
        Contract.ThrowIfNull(textChangeText);

        var startPosition = textChange.Span.Start;
        var endPosition = textChange.Span.Start + textChangeText.Length;

        if (placeholders.Length > 0)
        {
            startPosition = Math.Min(startPosition, placeholders.Min(placeholder => placeholder.StartingPositions.Min()));
            endPosition = Math.Max(endPosition, placeholders.Max(placeholder => placeholder.StartingPositions.Max()));
        }

        startPosition = Math.Min(startPosition, caretPosition);
        endPosition = Math.Max(endPosition, caretPosition);

        startPosition = Math.Min(startPosition, triggerLocation);

        return TextSpan.FromBounds(startPosition, endPosition);
    }
}
