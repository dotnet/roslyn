// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class LessAndGreaterThanBraceCompletionService : AbstractCSharpBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LessAndGreaterThanBraceCompletionService()
        {
        }

        protected override bool NeedsSemantics => true;

        protected override char OpeningBrace => LessAndGreaterThan.OpenCharacter;
        protected override char ClosingBrace => LessAndGreaterThan.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken);

        protected override bool IsValidOpeningBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.LessThanToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.GreaterThanToken);

        protected override async ValueTask<bool> IsValidOpenBraceTokenAtPositionAsync(Document document, SyntaxToken token, int position, CancellationToken cancellationToken)
        {
            // check what parser thinks about the newly typed "<" and only proceed if parser thinks it is "<" of 
            // type argument or parameter list
            return token.CheckParent<TypeParameterListSyntax>(n => n.LessThanToken == token) ||
                token.CheckParent<TypeArgumentListSyntax>(n => n.LessThanToken == token) ||
                token.CheckParent<FunctionPointerParameterListSyntax>(n => n.LessThanToken == token) ||
                await PossibleTypeArgumentAsync(document, token, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask<bool> PossibleTypeArgumentAsync(Document document, SyntaxToken token, CancellationToken cancellationToken)
        {
            // type argument can be easily ambiguous with normal < operations
            if (token.Parent is not BinaryExpressionSyntax(SyntaxKind.LessThanExpression) node || node.OperatorToken != token)
                return false;

            // type_argument_list only shows up in the following grammar construct:
            //
            // generic_name
            //  : identifier_token type_argument_list
            //
            // So if the prior token is not an identifier, this could not be a type-argument-list.
            var previousToken = token.GetPreviousToken();
            if (previousToken.Parent is not IdentifierNameSyntax identifier)
                return false;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(node.SpanStart, cancellationToken).ConfigureAwait(false);
            var info = semanticModel.GetSymbolInfo(identifier, cancellationToken);
            return info.CandidateSymbols.Any(static s => s.GetArity() > 0);
        }
    }
}
