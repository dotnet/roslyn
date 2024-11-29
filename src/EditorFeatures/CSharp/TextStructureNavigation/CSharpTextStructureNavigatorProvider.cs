// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.TextStructureNavigation;

[Export(typeof(ITextStructureNavigatorProvider))]
[ContentType(ContentTypeNames.CSharpContentType)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CSharpTextStructureNavigatorProvider(
    ITextStructureNavigatorSelectorService selectorService,
    IContentTypeRegistryService contentTypeService,
    IUIThreadOperationExecutor uIThreadOperationExecutor) : AbstractTextStructureNavigatorProvider(selectorService, contentTypeService, uIThreadOperationExecutor)
{
    protected override bool ShouldSelectEntireTriviaFromStart(SyntaxTrivia trivia)
        => trivia.IsRegularOrDocComment();

    private static int GetStartOfRawStringLiteralEndDelimiter(SyntaxToken token)
    {
        var text = token.ToString();
        var start = 0;
        var end = text.Length;

        if (token.Kind() is SyntaxKind.Utf8MultiLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken)
        {
            // Skip past the u8 suffix
            end -= "u8".Length;
        }

        while (start < end && text[start] == '"')
            start++;

        while (end > start && text[end - 1] == '"')
            end--;

        return token.SpanStart + end;
    }

    private static bool IsAtClosingQuote(SyntaxToken token, int position)
        => token.Kind() switch
        {
            SyntaxKind.StringLiteralToken => position == token.Span.End - 1 && token.Text[^1] == '"',
            SyntaxKind.Utf8StringLiteralToken => position == token.Span.End - 3 && token.Text is [.., '"', 'u' or 'U', '8'],
            _ => throw ExceptionUtilities.Unreachable()
        };

    protected override bool TryGetExtentOfWordFromToken(SyntaxToken token, SnapshotPoint position, out TextExtent textExtent)
    {
        textExtent = default;

        // Legacy behavior.  We let the editor handle these.  Note: this can be revisited if we think we would do a better
        // job handling these.
        if (token.Kind() is SyntaxKind.InterpolatedStringTextToken or SyntaxKind.XmlTextLiteralToken)
            return false;

        // Legacy behavior.  If we're on the start of a char literal, we select the entire thing.  For anything else, we
        // defer to the editor. Note: this can be revisited if we think we would do a better
        if (token.Kind() is SyntaxKind.CharacterLiteralToken)
        {
            if (token.SpanStart == position)
                return base.TryGetExtentOfWordFromToken(token, position, out textExtent);

            return false;
        }

        // For string literals, if we're on the starting quote, we want to select the entire string.
        //
        // If we're on the closing quote, we want to treat it as separate token.  This allows the cursor to stop during
        // word navigation (Ctrl+LeftArrow, etc.) immediately before AND after the closing quote, just like it did in
        // VS2013 and like it currently does for interpolated strings.
        //
        // If we're in the middle of the string, we want to let the editor take over.  but if it selects a span outside
        // of the string, we'll clamp the result back to within the string.

        if (token.Kind() is not (
                SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken or
                SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken or
                SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken))
        {
            return base.TryGetExtentOfWordFromToken(token, position, out textExtent);
        }

        // At the start of the string, select the entire string.
        var (startSpan, contentSpan, endSpan) = GetContentAndEndSpan(token);
        if (startSpan.Contains(position))
            return base.TryGetExtentOfWordFromToken(token, position, out textExtent);

        // If at the end, select the end piece only.
        if (endSpan.Contains(position))
        {
            textExtent = new TextExtent(new SnapshotSpan(position.Snapshot, endSpan), isSignificant: true);
            return true;
        }

        // We're in the middle.  Defer to the editor.  But make sure we don't go outside of the middle section.
        _natu

        if (token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken &&
            IsAtClosingQuote(token, position.Position))
        {
            // Special case to treat the closing quote of a string literal as a separate token.  This allows the
            // cursor to stop during word navigation (Ctrl+LeftArrow, etc.) immediately before AND after the
            // closing quote, just like it did in VS2013 and like it currently does for interpolated strings.
            var span = new Span(position.Position, token.Span.End - position.Position);
            return new TextExtent(new SnapshotSpan(position.Snapshot, span), isSignificant: true);
        }
        else if (token.Kind() is
            SyntaxKind.SingleLineRawStringLiteralToken or
            SyntaxKind.MultiLineRawStringLiteralToken or
            SyntaxKind.Utf8SingleLineRawStringLiteralToken or
            SyntaxKind.Utf8MultiLineRawStringLiteralToken)
        {
            var delimiterStart = GetStartOfRawStringLiteralEndDelimiter(token);
            return new TextExtent(new SnapshotSpan(position.Snapshot, Span.FromBounds(delimiterStart, token.Span.End)), isSignificant: true);
        }
        else
        {
        }
    }
}
