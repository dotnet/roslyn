// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal static class RoslynLSPSnippetConverter
    {
        /// <summary>
        /// Extends the TextChange to encompass all placeholder positions as well as caret position.
        /// Generates a LSP formatted snippet from a TextChange, list of placeholders, and caret position.
        /// </summary>
        public static async Task<string> GenerateLSPSnippetAsync(Document document, int caretPosition, ImmutableArray<SnippetPlaceholder> placeholders, TextChange textChange, CancellationToken cancellationToken)
        {
            var extendedTextChange = await ExtendSnippetTextChangeAsync(document, textChange, placeholders, caretPosition, cancellationToken).ConfigureAwait(false);
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

            using var _ = PooledStringBuilder.GetInstance(out var lspSnippetString);
            using var disposer = PooledDictionary<int, (string identifier, int priority)>.GetInstance(out var dictionary);
            GetMapOfSpanStartsToLSPStringItem(ref dictionary, placeholders, textChangeStart);

            // Need to go through the length + 1 since caret postions occur before and after the
            // character position.
            // If there is a caret at the end of the line, then it's position
            // will be equivalent to the length of the TextChange.
            for (var i = 0; i < textChangeText.Length + 1;)
            {
                if (i == caretPosition - textChangeStart)
                {
                    lspSnippetString.Append("$0");

                    // Special case for cursor position since they will occur between positions
                    // so we still wants to insert the character following the cursor position.
                    // Will not happen for placeholders since they have a direct mapping from position
                    // of the identifier to their position in the TextChange text.
                    if (i < textChangeText.Length)
                    {
                        lspSnippetString.Append(textChangeText[i]);
                    }

                    i++;
                }

                //Tries to see if a value exists at that position in the map, and if so it
                // generates a string that is LSP formatted.
                if (dictionary.TryGetValue(i, out var placeholderInfo))
                {
                    var str = $"${{{placeholderInfo.priority}:{placeholderInfo.identifier}}}";
                    lspSnippetString.Append(str);

                    // Skip past the entire identifier in the TextChange text
                    i += placeholderInfo.identifier.Length;
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
        private static void GetMapOfSpanStartsToLSPStringItem(ref PooledDictionary<int, (string identifier, int priority)> dictionary, ImmutableArray<SnippetPlaceholder> placeholders, int textChangeStart)
        {
            for (var i = 0; i < placeholders.Length; i++)
            {
                var placeholder = placeholders[i];
                foreach (var position in placeholder.PlaceHolderPositions)
                {
                    // i + 1 since the placeholder priority is set according to the index in the
                    // placeholders array, starting at 1.
                    dictionary.Add(position - textChangeStart, (placeholder.Identifier, i + 1));
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
        private static async Task<TextChange> ExtendSnippetTextChangeAsync(Document document, TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition, CancellationToken cancellationToken)
        {
            var extendedSpan = GetUpdatedTextSpan(textChange, placeholders, caretPosition);

            if (extendedSpan.Length == 0)
            {
                return textChange;
            }

            var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newString = documentText.ToString(extendedSpan);
            var newTextChange = new TextChange(new TextSpan(extendedSpan.Start, 0), newString);

            return newTextChange;
        }

        /// <summary>
        /// Iterates through the placeholders and determines if any of the positions
        /// come before or after what is indicated by the snippet's TextChange.
        /// If so, adjust the starting and ending position accordingly.
        /// </summary>
        private static TextSpan GetUpdatedTextSpan(TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition)
        {
            var textChangeText = textChange.NewText;
            Contract.ThrowIfNull(textChangeText);

            var startPosition = textChange.Span.Start;
            var endPosition = textChange.Span.Start + textChangeText.Length;

            if (placeholders.Length > 0)
            {
                startPosition = Math.Min(startPosition, placeholders.Min(placeholder => placeholder.PlaceHolderPositions.Min()));
                endPosition = Math.Max(endPosition, placeholders.Max(placeholder => placeholder.PlaceHolderPositions.Max()));
            }
                
            startPosition = Math.Min(startPosition, caretPosition);
            endPosition = Math.Max(endPosition, caretPosition);

            return TextSpan.FromBounds(startPosition, endPosition);
        }
    }
}
