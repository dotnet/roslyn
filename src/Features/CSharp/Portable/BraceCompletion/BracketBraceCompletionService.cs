// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class BracketBraceCompletionService : AbstractBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BracketBraceCompletionService()
        {
        }

        protected override char OpeningBrace => Bracket.OpenCharacter;

        protected override char ClosingBrace => Bracket.CloseCharacter;

        public override async Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            return await CheckCurrentPositionAsync(context.Document, context.CaretLocation, cancellationToken).ConfigureAwait(false)
                && await CheckClosingTokenKindAsync(context.Document, context.ClosingPoint, cancellationToken).ConfigureAwait(false);
        }

        protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.OpenBracketToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CloseBracketToken);
    }
}
