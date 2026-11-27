// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal partial class SourceTextDiffer
{
    private sealed class LineDiffer(SourceText oldText, SourceText newText)
        : TextSpanDiffer(oldText, newText)
    {
        protected override ImmutableArray<TextSpan> Tokenize(SourceText text)
        {
            using var builder = new PooledArrayBuilder<TextSpan>();

            foreach (var line in text.Lines)
            {
                builder.Add(line.SpanIncludingLineBreak);
            }

            return builder.ToImmutableAndClear();
        }
    }
}
