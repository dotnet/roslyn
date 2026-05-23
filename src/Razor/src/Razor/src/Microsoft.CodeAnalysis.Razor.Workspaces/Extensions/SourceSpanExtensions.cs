// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal static class SourceSpanExtensions
{
    public static TextSpan ToTextSpan(this SourceSpan span)
        => new(span.AbsoluteIndex, span.Length);

    public static LinePositionSpan ToLinePositionSpan(this SourceSpan span)
    {
        var start = new LinePosition(span.LineIndex, span.CharacterIndex);
        var end = new LinePosition(span.LineIndex + span.LineCount, span.EndCharacterIndex);

        return new LinePositionSpan(start, end);
    }
}
