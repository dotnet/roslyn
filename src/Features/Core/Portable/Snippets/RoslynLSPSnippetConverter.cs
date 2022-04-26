// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal static class RoslynLSPSnippetConverter
    {
        public static string? GenerateLSPSnippet(TextChange textChange, ImmutableArray<RoslynLSPSnippetItem> placeholders)
        {
            var textChangeStart = textChange.Span.Start;
            var textChangeText = textChange.NewText!;
            using var _ = PooledStringBuilder.GetInstance(out var lspSnippetString);
            var map = GetMapOfSpanStartsToLSPStringItem(placeholders, textChangeStart);

            for (var i = 0; i < textChangeText.Length;)
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

                    if (strLength == 0)
                    {
                        lspSnippetString.Append(textChangeText[i]);
                        i++;
                    }
                }
            }

            return lspSnippetString.ToString();
        }

        private static Dictionary<int, RoslynLSPSnippetStringItem> GetMapOfSpanStartsToLSPStringItem(ImmutableArray<RoslynLSPSnippetItem> placeholders, int textChangeStart)
        {
            var map = new Dictionary<int, RoslynLSPSnippetStringItem>();

            foreach (var placeholder in placeholders)
            {
                foreach (var span in placeholder.PlaceHolderSpans)
                {
                    map.Add(span.Start - textChangeStart, new RoslynLSPSnippetStringItem(placeholder.Identifier, placeholder.Priority));
                }

                if (placeholder.CaretPosition.HasValue)
                {
                    map.Add(placeholder.CaretPosition.Value - textChangeStart, new RoslynLSPSnippetStringItem(placeholder.Identifier, placeholder.Priority));
                }
            }

            return map;
        }

        private static (string str, int strLength) GetStringInPosition(Dictionary<int, RoslynLSPSnippetStringItem> map, int position)
        {
            if (map.TryGetValue(position, out var lspStringItem))
            {
                if (lspStringItem.Identifier is not null)
                {
                    return ($"${{{lspStringItem.Priority}:{lspStringItem.Identifier}}}", lspStringItem.Identifier.Length);
                }
                else
                {
                    return ("$0", 0);
                }
            }

            return (string.Empty, 0);
        }

        public static TextChange ExtendSnippetTextChange(TextChange textChange, ImmutableArray<RoslynLSPSnippetItem> lspSnippetItems)
        {
            var newTextChange = textChange;
            foreach (var lspSnippetItem in lspSnippetItems)
            {
                foreach (var placeholder in lspSnippetItem.PlaceHolderSpans)
                {
                    if (newTextChange.Span.Start > placeholder.Start)
                    {
                        newTextChange = new TextChange(new TextSpan(placeholder.Start, 0), textChange.NewText!);
                    }
                }

                if (lspSnippetItem.CaretPosition is not null && textChange.Span.Start > lspSnippetItem.CaretPosition)
                {
                    newTextChange = new TextChange(new TextSpan(lspSnippetItem.CaretPosition.Value, 0), textChange.NewText!);
                }
            }

            return newTextChange;
        }
    }
}
