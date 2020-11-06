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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class InterpolationBraceCompletionService : AbstractBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InterpolationBraceCompletionService()
        {
        }

        protected override char OpeningBrace => CurlyBrace.OpenCharacter;

        protected override char ClosingBrace => CurlyBrace.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => SpecializedTasks.True;

        protected override Task<bool> CheckOpeningPointAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult(IsValidOpeningBraceToken(token)
                && token.SpanStart == position);
        }

        public override async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
            => OpeningBrace == brace && await IsCurlyBraceInInterpolationContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false);

        protected override bool IsValidOpeningBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent.IsKind(SyntaxKind.Interpolation);

        protected override bool IsValidClosingBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.CloseBraceToken);

        public static async Task<bool> IsCurlyBraceInInterpolationContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // First, check to see if the character to the left of the position is an open curly. If it is,
            // we shouldn't complete because the user may be trying to escape a curly.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var index = position - 1;
            var openCurlyCount = 0;
            while (index >= 0)
            {
                if (text[index] == '{')
                {
                    openCurlyCount++;
                }
                else
                {
                    break;
                }

                index--;
            }

            if (openCurlyCount > 0 && openCurlyCount % 2 == 1)
            {
                return false;
            }

            // Next, check to see if we're typing in an interpolated string
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindTokenOnLeftOfPosition(position);

            if (!token.Span.IntersectsWith(position))
            {
                return false;
            }

            return token.IsKind(
                SyntaxKind.InterpolatedStringStartToken,
                SyntaxKind.InterpolatedVerbatimStringStartToken,
                SyntaxKind.InterpolatedStringTextToken,
                SyntaxKind.InterpolatedStringEndToken);
        }
    }
}
