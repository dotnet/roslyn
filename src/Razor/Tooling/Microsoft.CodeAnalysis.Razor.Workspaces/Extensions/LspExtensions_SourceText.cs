// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static int GetPosition(this SourceText text, Position position)
        => text.GetPosition(position.ToLinePosition());

    public static Position GetPosition(this SourceText text, int position)
        => text.GetLinePosition(position).ToPosition();

    public static LspRange GetRange(this SourceText text, TextSpan span)
        => text.GetLinePositionSpan(span).ToRange();

    public static LspRange GetRange(this SourceText text, SourceSpan span)
        => text.GetLinePositionSpan(span).ToRange();

    public static LspRange GetRange(this SourceText text, int start, int end)
        => text.GetLinePositionSpan(start, end).ToRange();

    public static LspRange GetZeroWidthRange(this SourceText text, int position)
        => text.GetLinePosition(position).ToZeroWidthRange();

    public static bool IsValidPosition(this SourceText text, Position position)
        => text.IsValidPosition(position.Line, position.Character);

    public static bool TryGetAbsoluteIndex(this SourceText text, Position position, out int absoluteIndex)
        => text.TryGetAbsoluteIndex(position.Line, position.Character, out absoluteIndex);

    public static int GetRequiredAbsoluteIndex(this SourceText text, Position position)
        => text.GetRequiredAbsoluteIndex(position.Line, position.Character);

    public static TextSpan GetTextSpan(this SourceText text, LspRange range)
        => text.GetTextSpan(range.Start.Line, range.Start.Character, range.End.Line, range.End.Character);

    public static bool TryGetSourceLocation(this SourceText text, Position position, out SourceLocation location)
        => text.TryGetSourceLocation(position.Line, position.Character, out location);

    public static TextChange GetTextChange(this SourceText text, TextEdit edit)
        => new(text.GetTextSpan(edit.Range), edit.NewText);

    public static RazorTextChange GetRazorTextChange(this SourceText text, TextEdit edit)
        => new()
        {
            Span = text.GetTextSpan(edit.Range).ToRazorTextSpan(),
            NewText = edit.NewText
        };

    public static TextEdit GetTextEdit(this SourceText text, TextChange change)
        => LspFactory.CreateTextEdit(text.GetRange(change.Span), change.NewText ?? "");

    public static TextEdit GetTextEdit(this SourceText text, RazorTextChange change)
        => LspFactory.CreateTextEdit(text.GetRange(change.Span.Start, change.Span.Start + change.Span.Length), change.NewText ?? "");
}
