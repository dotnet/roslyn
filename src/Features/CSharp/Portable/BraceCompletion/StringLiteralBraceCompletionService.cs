﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class StringLiteralBraceCompletionService : AbstractBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StringLiteralBraceCompletionService()
        {
        }

        protected override char OpeningBrace => DoubleQuote.OpenCharacter;

        protected override char ClosingBrace => DoubleQuote.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeWithValidClosingTokenAsync(context, cancellationToken);

        public override async Task<bool> CanProvideBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // Only potentially valid for string literal completion if not in an interpolated string brace completion context.
            if (OpeningBrace == brace && await InterpolatedStringBraceCompletionService.IsPositionInInterpolatedStringContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            return await base.CanProvideBraceCompletionAsync(brace, openingPosition, document, cancellationToken).ConfigureAwait(false);
        }

        protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.StringLiteralToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.StringLiteralToken);

        protected override Task<bool> IsValidOpenBraceTokenAtPositionAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (ParentIsSkippedTokensTriviaOrNull(syntaxFactsService, token) || !IsValidOpeningBraceToken(token))
            {
                return SpecializedTasks.False;
            }

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
                return SpecializedTasks.False;
            }

            if (token.SpanStart == position)
            {
                return SpecializedTasks.True;
            }

            // The character at the position is a double quote but the token's span start we found at the position
            // doesn't match the position.  Check if we're in a verbatim string token @" where the token span start
            // is the @ character and the " is one past the token start.
            return Task.FromResult(token.SpanStart + 1 == position && token.IsVerbatimStringLiteral());
        }

        private static bool RestOfLineContainsDiagnostics(SyntaxToken token)
        {
            while (!token.TrailingTrivia.Contains(t => t.IsEndOfLine()))
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
}
