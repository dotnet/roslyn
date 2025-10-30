// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RawStringLiteral;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.RawStringLiteral;

[ExportLanguageService(typeof(IRawStringLiteralAutoInsertService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRawStringLiteralOnAutoInsertService() : IRawStringLiteralAutoInsertService
{
    public TextChange? GetTextChangeForQuote(Document document, SourceText text, int caretPosition, CancellationToken cancellationToken)
    {
        return
            TryGenerateInitialEmptyRawString(text, document, caretPosition, cancellationToken) ??
            TryGrowInitialEmptyRawString(text, document, caretPosition, cancellationToken) ??
            TryGrowRawStringDelimiters(text, document, caretPosition, cancellationToken);
    }

    /// <summary>
    /// When typing <c>"</c> given a normal string like <c>""$$</c>, then update the text to be <c>"""$$"""</c>.
    /// Note that this puts the user in the position where TryGrowInitialEmptyRawString can now take effect.
    /// </summary>
    private static TextChange? TryGenerateInitialEmptyRawString(
        SourceText text,
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        // if we have ""$$"   then typing `"` here should not be handled by this path but by TryGrowInitialEmptyRawString
        if (position + 1 < text.Length && text[position + 1] == '"')
            return null;

        var start = position;
        while (start - 1 >= 0 && text[start - 1] == '"')
            start--;

        // must have exactly `""`
        if (position - start != 2)
            return null;

        while (start - 1 >= 0 && text[start - 1] == '$')
            start--;

        // hitting `"` after `@""` shouldn't do anything
        if (start - 1 >= 0 && text[start - 1] == '@')
            return null;

        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        var token = root.FindToken(start);
        if (token.SpanStart != start)
            return null;

        if (token.Kind() is not (SyntaxKind.StringLiteralToken or
                                 SyntaxKind.InterpolatedStringStartToken or
                                 SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                 SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            return null;
        }

        return new TextChange(new TextSpan(position + 1, 0), "\"\"\"");
    }

    /// <summary>
    /// When typing <c>"</c> given a raw string like <c>"""$$"""</c> (or a similar multiline form), then update the
    /// text to be: <c>""""$$""""</c>.  i.e. grow both the start and end delimiters to keep the string properly
    /// balanced.  This differs from TryGrowRawStringDelimiters in that the language will consider that initial
    /// <c>""""""</c> text to be a single delimiter, while we want to treat it as two.
    /// </summary>
    private static TextChange? TryGrowInitialEmptyRawString(
        SourceText text,
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var start = position;
        while (start - 1 >= 0 && text[start - 1] == '"')
            start--;

        var end = position;
        while (end < text.Length && text[end] == '"')
            end++;

        // Have to have an even number of quotes.
        var quoteLength = end - start;
        if (quoteLength % 2 == 1)
            return null;

        // User position must be halfway through the quotes.
        if (position != (start + quoteLength / 2))
            return null;

        // have to at least have `"""$$"""`
        if (quoteLength < 6)
            return null;

        while (start - 1 >= 0 && text[start - 1] == '$')
            start--;

        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        var token = root.FindToken(start);
        if (token.SpanStart != start)
            return null;

        if (token.Kind() is not (SyntaxKind.SingleLineRawStringLiteralToken or
                                 SyntaxKind.MultiLineRawStringLiteralToken or
                                 SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                 SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            return null;
        }

        return new TextChange(new TextSpan(position + 1, 0), "\"");
    }

    /// <summary>
    /// When typing <c>"</c> given a raw string like <c>"""$$ goo bar """</c> (or a similar multiline form), then
    /// update the text to be: <c>"""" goo bar """"</c>.  i.e. grow both the start and end delimiters to keep the
    /// string properly balanced.
    /// </summary>
    private static TextChange? TryGrowRawStringDelimiters(
        SourceText text,
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        // if we have """$$"   then typing `"` here should not grow the start/end quotes.  we only want to grow them
        // if the user is at the end of the start delimiter.
        if (position < text.Length && text[position] == '"')
            return null;

        var start = position;
        while (start - 1 >= 0 && text[start - 1] == '"')
            start--;

        // must have at least three quotes for this to be a raw string
        var quoteCount = position - start;
        if (quoteCount < 3)
            return null;

        while (start - 1 >= 0 && text[start - 1] == '$')
            start--;

        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        var token = root.FindToken(start);
        if (token.SpanStart != start)
            return null;

        if (token.Span.Length < (2 * quoteCount))
            return null;

        if (token.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken)
        {
            var interpolatedString = (InterpolatedStringExpressionSyntax)token.GetRequiredParent();
            var endToken = interpolatedString.StringEndToken;
            if (!endToken.Text.EndsWith(new string('"', quoteCount)))
                return null;
        }
        else if (token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken)
        {
            if (!token.Text.EndsWith(new string('"', quoteCount)))
                return null;
        }

        return new TextChange(new TextSpan(token.GetRequiredParent().Span.End, 0), "\"");
    }
}
