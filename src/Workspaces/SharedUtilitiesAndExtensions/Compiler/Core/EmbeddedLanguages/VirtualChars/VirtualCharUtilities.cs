// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

internal static class VirtualCharUtilities
{
    public static TextSpan FromBounds(VirtualChar vc1, VirtualChar vc2)
        => TextSpan.FromBounds(vc1.Span.Start, vc2.Span.End);

    /// <summary>
    /// Takes a <see cref="VirtualCharSequence"/> and returns the same characters from it, without any characters
    /// corresponding to test markup (e.g. <c>$$</c> and the like).  Because the virtual chars contain their
    /// original text span, these final virtual chars can be used both as the underlying source of a <see
    /// cref="SourceText"/> (which only cares about their <see cref="char"/> value), as well as the way to then map
    /// positions/spans within that <see cref="SourceText"/> to actual full virtual char spans in the original
    /// document for classification.
    /// </summary>
    public static (ImmutableSegmentedList<VirtualChar> sourceCode, ImmutableArray<TextSpan> markdownSpans) StripMarkupCharacters(
        ArrayBuilder<VirtualChar> virtualChars, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var markdownSpans);
        var builder = ImmutableSegmentedList.CreateBuilder<VirtualChar>();

        var nestedAnonymousSpanCount = 0;
        var nestedNamedSpanCount = 0;

        for (int i = 0, n = virtualChars.Count; i < n;)
        {
            var vc1 = virtualChars[i];
            var vc2 = i + 1 < n ? virtualChars[i + 1] : default;

            // These casts are safe because we disallowed virtual chars whose Value doesn't fit in a char in
            // RegisterClassifications.
            //
            // TODO: this algorithm is not actually the one used in roslyn or the roslyn-sdk for parsing a
            // markup file.  for example it will get `[|]` wrong (as that depends on knowing if we're starting
            // or ending an existing span).  Fix this up to follow the actual algorithm we use.
            switch ((vc1.Value, vc2.Value))
            {
                case ('$', '$'):
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    i += 2;
                    continue;
                case ('|', ']'):
                    nestedAnonymousSpanCount = Math.Max(0, nestedAnonymousSpanCount - 1);
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    i += 2;
                    continue;
                case ('|', '}'):
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    nestedNamedSpanCount = Math.Max(0, nestedNamedSpanCount - 1);
                    i += 2;
                    continue;

                // We have a slight ambiguity with cases like these:
                //
                // [|]    [|}
                //
                // Is it starting a new match, or ending an existing match.  As a workaround, we special case
                // these and consider it ending a match if we have something on the stack already.

                case ('[', '|'):
                    var vc3 = i + 2 < n ? virtualChars[i + 2] : default;
                    if ((vc3 == ']' && nestedAnonymousSpanCount > 0) ||
                        (vc3 == '}' && nestedNamedSpanCount > 0))
                    {
                        // not the start of a span, don't classify this '[' specially.
                        break;
                    }

                    nestedAnonymousSpanCount++;
                    markdownSpans.Add(FromBounds(vc1, vc2));
                    i += 2;
                    continue;

                case ('{', '|'):
                    if (TryConsumeNamedSpanStart(ref i, n))
                        continue;

                    // didn't find the colon.  don't classify these specially.
                    break;
            }

            // Nothing special, add character as is.
            builder.Add(vc1);
            i++;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return (builder.ToImmutable(), markdownSpans.ToImmutableAndClear());

        bool TryConsumeNamedSpanStart(ref int i, int n)
        {
            var start = i;
            var seekPoint = i;
            while (seekPoint < n)
            {
                var colonChar = virtualChars[seekPoint];
                if (colonChar == ':')
                {
                    markdownSpans.Add(FromBounds(virtualChars[start], colonChar));
                    nestedNamedSpanCount++;
                    i = seekPoint + 1;
                    return true;
                }

                seekPoint++;
            }

            return false;
        }
    }
}
