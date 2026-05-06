// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal partial class SourceTextDiffer
{
    private sealed class WordDiffer(SourceText oldText, SourceText newText)
        : TextSpanDiffer(oldText, newText)
    {
        protected override ImmutableArray<TextSpan> Tokenize(SourceText text)
        {
            using var builder = new PooledArrayBuilder<TextSpan>();

            var currentSpanStart = 0;
            var currentClassification = Classify(text[0]);

            // This algorithm is simpler than a normal tokenizer might be because we want to keep contiguous
            // whitespace characters in the same "word", and we don't really care about contiguous quotes
            // or slashes, so we can keep it simple and just capture a "word" when the classification of
            // the current character changes.
            for (var index = 1; index < text.Length; index++)
            {
                var classification = Classify(text[index]);
                if (classification != currentClassification)
                {
                    // We've hit a word boundary, so store this and move on
                    builder.Add(TextSpan.FromBounds(currentSpanStart, index));
                    currentSpanStart = index;
                    currentClassification = classification;
                }
            }

            // It's impossible for the loop to capture the last word
            Debug.Assert(currentSpanStart < text.Length);
            builder.Add(TextSpan.FromBounds(currentSpanStart, text.Length));

            return builder.ToImmutableAndClear();
        }

        private static int Classify(char c)
        {
            // The type of classification doesn't matter as long as its unique and equatible
            return c switch
            {
                '/' => 0,
                '"' => 1,
                _ when char.IsWhiteSpace(c) => 2,
                _ => 3,
            };
        }
    }
}
