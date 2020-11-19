// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
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

        public override async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
            => OpeningBrace == brace && await IsPositionInInterpolatedStringContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false);

        protected override bool IsValidOpeningBraceToken(SyntaxToken leftToken)
            => leftToken.IsKind(SyntaxKind.InterpolatedStringStartToken) || leftToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken rightToken)
            => rightToken.IsKind(SyntaxKind.InterpolatedStringEndToken);

        protected override Task<bool> IsValidOpenBraceTokenAtPositionAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsValidOpeningBraceToken(token)
                && token.Span.End - 1 == position);
        }

        public static async Task<bool> IsPositionInInterpolatedStringContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // Check to see if we're to the right of an $ or an @$
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var start = position - 1;
            if (start < 0)
            {
                return false;
            }

            if (text[start] == '@')
            {
                start--;

                if (start < 0)
                {
                    return false;
                }
            }

            if (text[start] != '$')
            {
                return false;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindTokenOnLeftOfPosition(start);

            return root.SyntaxTree.IsExpressionContext(start, token, attributes: false, cancellationToken: cancellationToken)
                || root.SyntaxTree.IsStatementContext(start, token, cancellationToken);
        }
    }
}
