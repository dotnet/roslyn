// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    /// <summary>
    /// Brace completion service for double quotes marking an interpolated string.
    /// Note that the <see cref="StringLiteralBraceCompletionService"/> is used for
    /// other double quote completions.
    /// </summary>
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class InterpolatedStringBraceCompletionService : AbstractBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InterpolatedStringBraceCompletionService()
        {
        }

        protected override char OpeningBrace => DoubleQuote.OpenCharacter;

        protected override char ClosingBrace => DoubleQuote.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeWithValidClosingTokenAsync(context, cancellationToken);

        /// <summary>
        /// Only return this service as valid when we're starting an interpolated string.
        /// Otherwise double quotes should be completed using the <see cref="StringLiteralBraceCompletionService"/>
        /// </summary>
        public override async Task<bool> CanProvideBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
            => OpeningBrace == brace && await IsPositionInInterpolatedStringContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false);

        protected override bool IsValidOpeningBraceToken(SyntaxToken leftToken)
            => leftToken.IsKind(SyntaxKind.InterpolatedStringStartToken) || leftToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken rightToken)
            => rightToken.IsKind(SyntaxKind.InterpolatedStringEndToken);

        protected override Task<bool> IsValidOpenBraceTokenAtPositionAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
            => Task.FromResult(IsValidOpeningBraceToken(token) && token.Span.End - 1 == position);

        /// <summary>
        /// Returns true when the input position could be starting an interpolated string if opening quotes were typed.
        /// </summary>
        public static async Task<bool> IsPositionInInterpolatedStringContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var start = position - 1;
            if (start < 0)
                return false;

            // Check if the user is typing an interpolated or interpolated verbatim string.
            // If the preceding character(s) are not '$' or '$@' then we can't be starting an interpolated string.
            if (text[start] == '@')
            {
                if (--start < 0)
                    return false;
            }

            if (text[start] != '$')
                return false;

            // Verify that we are actually in an location allowed for an interpolated string.
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(start);
            if (token.Kind() is not SyntaxKind.InterpolatedStringStartToken and
                                not SyntaxKind.InterpolatedVerbatimStringStartToken and
                                not SyntaxKind.StringLiteralToken and
                                not SyntaxKind.IdentifierToken)
            {
                return false;
            }

            var previousToken = token.GetPreviousToken();

            return root.SyntaxTree.IsExpressionContext(token.SpanStart, previousToken, attributes: true, cancellationToken)
                || root.SyntaxTree.IsStatementContext(token.SpanStart, previousToken, cancellationToken);
        }
    }
}
