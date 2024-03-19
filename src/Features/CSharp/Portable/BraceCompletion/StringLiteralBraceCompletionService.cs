// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion;

[Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class StringLiteralBraceCompletionService() : AbstractCSharpBraceCompletionService
{
    protected override char OpeningBrace => DoubleQuote.OpenCharacter;
    protected override char ClosingBrace => DoubleQuote.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
        => AllowOverTypeWithValidClosingToken(context);

    public override bool CanProvideBraceCompletion(char brace, int position, ParsedDocument document, CancellationToken cancellationToken)
    {
        var text = document.Text;

        // Only potentially valid for string literal completion if not in an interpolated string brace completion context.
        if (OpeningBrace == brace && InterpolatedStringBraceCompletionService.IsPositionInInterpolatedStringContext(document, position, cancellationToken))
            return false;

        if (!base.CanProvideBraceCompletion(brace, position, document, cancellationToken))
            return false;

        // Find the start of the string literal token (including if it is a verbatim string).
        var start = position;
        if (start > 0 && text[start - 1] == '@')
            start--;

        // Has to be the start of an expression, or within a directive (string literals are legal there).
        return IsLegalExpressionLocation(document.SyntaxTree, start, cancellationToken)
            || document.SyntaxTree.IsPreProcessorDirectiveContext(start, cancellationToken);
    }

    protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.StringLiteralToken);

    protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.StringLiteralToken);

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        if (ParentIsSkippedTokensTriviaOrNull(this.SyntaxFacts, token) || !IsValidOpeningBraceToken(token))
            return false;

        // If the single token that the user typed is a string literal that is more than just
        // the one double quote character they typed, and the line doesn't have errors, then
        // it means it is completing an existing token, from the start. For example given:
        //
        // var s = "te$$st";
        //
        // When the user types `" + "` to split the string into two literals, the first
        // quote won't be completed (because its in a string literal), and with this check
        // the second quote won't either.
        //
        // We don't do this optimization for verbatim strings because they are multi-line so
        // the flow on effects from us getting it wrong are much greater, and it can really change
        // the tree.
        if (token.IsKind(SyntaxKind.StringLiteralToken) &&
            !token.IsVerbatimStringLiteral() &&
            token.Span.Length > 1 &&
            !RestOfLineContainsDiagnostics(token))
        {
            return false;
        }

        var isStartOfString = token.SpanStart == position;

        // If the character at the position is a double quote but the token's span start we found at the position
        // doesn't match the position.  Check if we're in a verbatim string token @" where the token span start is the @
        // character and the " is one past the token start.
        var isStartOfVerbatimString = token.SpanStart + 1 == position && token.IsVerbatimStringLiteral();

        if (!isStartOfString && !isStartOfVerbatimString)
            return false;

        return true;
    }

    private static bool RestOfLineContainsDiagnostics(SyntaxToken token)
    {
        while (!token.IsKind(SyntaxKind.None) && !token.TrailingTrivia.Contains(t => t.IsEndOfLine()))
        {
            if (token.ContainsDiagnostics)
                return true;

            token = token.GetNextToken();
        }

        if (token.ContainsDiagnostics)
            return true;

        return false;
    }
}
