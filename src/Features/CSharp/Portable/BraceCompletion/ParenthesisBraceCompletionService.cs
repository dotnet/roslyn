// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion;

[ExportBraceCompletionService(LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ParenthesisBraceCompletionService() : AbstractCSharpBraceCompletionService
{
    protected override char OpeningBrace => Parenthesis.OpenCharacter;
    protected override char ClosingBrace => Parenthesis.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext context, CancellationToken cancellationToken)
        => AllowOverTypeInUserCodeWithValidClosingToken(context, cancellationToken);

    protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.OpenParenToken);

    protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CloseParenToken);

    protected override bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
    {
        if (ParentIsSkippedTokensTriviaOrNull(this.SyntaxFacts, token)
            || !IsValidOpeningBraceToken(token)
            || token.SpanStart != position
            || token.Parent == null)
        {
            return false;
        }

        // now check whether parser think whether there is already counterpart closing parenthesis
        var (openParen, closeParen) = token.Parent.GetParentheses();

        // We can complete the brace if the closing brace is missing or the incorrect kind.
        if (closeParen.Kind() != SyntaxKind.CloseParenToken || closeParen.Span.Length == 0)
        {
            return true;
        }

        // Walk up parent nodes to see if any ancestor has a missing close paren positioned
        // right after our current close paren. This handles cases like: if (a.Where(
        // where the parser "borrows" the ArgumentList's close paren for the if statement.
        var currentNode = token.Parent.Parent;
        while (currentNode != null)
        {
            var (ancestorOpenParen, ancestorCloseParen) = currentNode.GetParentheses();

            // Check if this ancestor has a missing close paren that's positioned right after
            // our current close paren (meaning the parser reused our close paren for the ancestor)
            if (ancestorOpenParen != default &&
                ancestorCloseParen.IsMissing &&
                ancestorCloseParen.FullSpan.Start == closeParen.FullSpan.End)
            {
                return true;
            }

            currentNode = currentNode.Parent;
        }

        // If the completed pair is on the same line, then the closing parenthesis must belong to a different
        // brace completion session higher up on the stack.  If that's the case then we can
        // complete the opening brace here, so return this as valid for completion.
        return text.Lines.GetLineFromPosition(openParen.SpanStart).LineNumber == text.Lines.GetLineFromPosition(closeParen.Span.End).LineNumber;
    }
}
