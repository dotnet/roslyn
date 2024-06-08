// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion;

/// <summary>
/// Brace completion service for double quotes marking an interpolated string. Note that the <see
/// cref="StringLiteralBraceCompletionService"/> is used for other double quote completions.
/// </summary>
[ExportBraceCompletionService(LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class InterpolatedStringBraceCompletionService() : AbstractCSharpBraceCompletionService
{
    protected override char OpeningBrace => DoubleQuote.OpenCharacter;
    protected override char ClosingBrace => DoubleQuote.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
        => AllowOverTypeWithValidClosingToken(context);

    /// <summary>
    /// Only return this service as valid when we're starting an interpolated string. Otherwise double quotes should be
    /// completed using the <see cref="StringLiteralBraceCompletionService"/>
    /// </summary>
    public override bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
        => OpeningBrace == brace && IsPositionInInterpolatedStringContext(document, openingPosition, cancellationToken);

    protected override bool IsValidOpeningBraceToken(SyntaxToken leftToken)
        => leftToken.Kind() is SyntaxKind.InterpolatedStringStartToken or SyntaxKind.InterpolatedVerbatimStringStartToken;

    protected override bool IsValidClosingBraceToken(SyntaxToken rightToken)
        => rightToken.IsKind(SyntaxKind.InterpolatedStringEndToken);

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
        => IsValidOpeningBraceToken(token) && token.Span.End - 1 == position;

    /// <summary>
    /// Returns true when the input position could be starting an interpolated string if opening quotes were typed.
    /// </summary>
    public static bool IsPositionInInterpolatedStringContext(ParsedDocument document, int position, CancellationToken cancellationToken)
    {
        var text = document.Text;

        var start = position - 1;
        if (start < 0)
            return false;

        // Check if the user is typing an interpolated or interpolated verbatim string.
        // If the preceding character(s) are not '$' or '$@' then we can't be starting an interpolated string.
        if (text[start] == '@')
        {
            // must have $@ for an interpolated string.  otherwise this is some other @ construct.
            if (start == 0 || text[start - 1] != '$')
                return false;

            start--;
        }
        else if (text[start] == '$')
        {
            // could be @$
            if (start > 0 && text[start - 1] == '@')
                start--;
        }
        else
        {
            return false;
        }

        return IsLegalExpressionLocation(document.SyntaxTree, start, cancellationToken);
    }
}
