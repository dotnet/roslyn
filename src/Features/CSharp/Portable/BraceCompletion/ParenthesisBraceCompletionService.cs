// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class ParenthesisBraceCompletionService : AbstractBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ParenthesisBraceCompletionService()
        {
        }

        protected override char OpeningBrace => Parenthesis.OpenCharacter;

        protected override char ClosingBrace => Parenthesis.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken);

        protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.OpenParenToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CloseParenToken);

        protected override async Task<bool> IsValidOpenBraceTokenAtPositionAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (ParentIsSkippedTokensTrivia(syntaxFactsService, token) ||
                !IsValidOpeningBraceToken(token) ||
                token.SpanStart != position || token.Parent == null)
            {
                return false;
            }

            // now check whether parser think whether there is already counterpart closing parenthesis
            var (openBrace, closeBrace) = token.Parent.GetParentheses();

            // If the completed pair is on the same line, then the closing parenthesis must belong to a different
            // brace completion session higher up on the stack.  If that's the case then we can
            // complete the opening brace here, so return this as valid for completion.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text.Lines.GetLineFromPosition(openBrace.SpanStart) == text.Lines.GetLineFromPosition(closeBrace.Span.End))
            {
                return true;
            }

            // We can complete the brace if the closing brace is missing or the incorrect kind.
            return closeBrace.Kind() != SyntaxKind.CloseParenToken || closeBrace.Span.Length == 0;
        }
    }
}
