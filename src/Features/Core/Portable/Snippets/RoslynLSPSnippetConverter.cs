// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public static async Task<string> GenerateLSPSnippetAsync(Document document, int caretPosition, ImmutableArray<SnippetPlaceholder> placeholders, TextChange collapsedTextChange)
        {
            var extendedTextChange = await ExtendSnippetTextChangeAsync(document, collapsedTextChange, placeholders, caretPosition).ConfigureAwait(false);
            return ConvertToLSPSnippetString(extendedTextChange, placeholders, caretPosition);
        }

        private static string ConvertToLSPSnippetString(TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition)
        {
            var textChangeStart = textChange.Span.Start;
            var textChangeText = textChange.NewText!;
            using var _ = PooledStringBuilder.GetInstance(out var lspSnippetString);
            var map = GetMapOfSpanStartsToLSPStringItem(placeholders, textChangeStart);

            for (var i = 0; i < textChangeText.Length + 1;)
            {
                if (i == caretPosition - textChangeStart)
                {
                    lspSnippetString.Append("$0");
                    i++;
                }

                if (i < textChangeText.Length)
                {
                    var (str, strLength) = GetStringInPosition(map, position: i);
                    if (str.IsEmpty())
                    {
                        lspSnippetString.Append(textChangeText[i]);
                        i++;
                    }
                    else
                    {
                        lspSnippetString.Append(str);
                        i += strLength;
                    }
                }
                else
                {
                    break;
                }
            }

            return lspSnippetString.ToString();
        }

        private static Dictionary<int, (string identifier, int priority)> GetMapOfSpanStartsToLSPStringItem(ImmutableArray<SnippetPlaceholder> placeholders, int textChangeStart)
        {
            var map = new Dictionary<int, (string, int)>();

            for (var i = 0; i < placeholders.Length; i++)
            {
                var placeholder = placeholders[i];
                foreach (var position in placeholder.PlaceHolderPositions)
                {
                    map.Add(position - textChangeStart, (placeholder.Identifier, i + 1));
                }
            }

            return map;
        }

        private static (string str, int strLength) GetStringInPosition(Dictionary<int, (string identifier, int priority)> map, int position)
        {
            if (map.TryGetValue(position, out var placeholderInfo))
            {
                return ($"${{{placeholderInfo.priority}:{placeholderInfo.identifier}}}", placeholderInfo.identifier.Length);
            }

            return (string.Empty, 0);
        }

        private static async Task<TextChange> ExtendSnippetTextChangeAsync(Document document, TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition)
        {
            var extendedSpan = GetUpdatedTextSpan(textChange, placeholders, caretPosition);

            if (extendedSpan.Length == 0)
            {
                return textChange;
            }

            var documentText = await document.GetTextAsync().ConfigureAwait(false);
            var newString = documentText.ToString(extendedSpan);
            var newTextChange = new TextChange(new TextSpan(extendedSpan.Start, 0), newString);

            return newTextChange;
        }

        private static TextSpan GetUpdatedTextSpan(TextChange textChange, ImmutableArray<SnippetPlaceholder> placeholders, int caretPosition)
        {
            var textSpanLength = textChange.NewText!.Length;

            var startPosition = textChange.Span.Start;
            var endPosition = textChange.Span.Start + textSpanLength;

            foreach (var placeholder in placeholders)
            {
                foreach (var position in placeholder.PlaceHolderPositions)
                {
                    if (startPosition > position)
                    {
                        endPosition += startPosition - caretPosition;
                        startPosition = position;
                    }

                    if (startPosition + textSpanLength < position)
                    {
                        endPosition = position;
                    }
                }
            }

            if (startPosition > caretPosition)
            {
                endPosition += startPosition - caretPosition;
                startPosition = caretPosition;
            }

            if (startPosition + textSpanLength < caretPosition)
            {
                endPosition = caretPosition;
            }

            return TextSpan.FromBounds(startPosition, endPosition);
        }
    }
}
