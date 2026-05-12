// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed record class IndentationContext(
    FormattingSpan FirstSpan,
    int Line,
#if DEBUG
    string? DebugOnly_LineText,
#endif
    int RazorIndentationLevel,
    int HtmlIndentationLevel,
    int RelativeIndentationLevel,
    int ExistingIndentation,
    bool EmptyOrWhitespaceLine,
    int ExistingIndentationSize)
{
    public int IndentationLevel => RazorIndentationLevel + HtmlIndentationLevel;

    public bool StartsInHtmlContext => FirstSpan.Kind == FormattingSpanKind.Markup;

    public bool StartsInCSharpContext => FirstSpan.Kind == FormattingSpanKind.Code;

    public bool StartsInRazorContext => !StartsInHtmlContext && !StartsInCSharpContext;

    public int MinCSharpIndentLevel => FirstSpan.MinCSharpIndentLevel;

    public override string ToString()
    {
        return $"Line: {Line}, IndentationLevel: {IndentationLevel}, RelativeIndentationLevel: {RelativeIndentationLevel}, ExistingIndentation: {ExistingIndentation}";
    }
}
