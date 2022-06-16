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
            if (token.Parent is not BinaryExpressionSyntax node || node.Kind() != SyntaxKind.LessThanExpression || node.OperatorToken != token)
                return false;

            // use binding to see whether it is actually generic type or method 
            // Analyze node on the left of < operator to verify if it is a generic type or method.
            var leftNode = node.Left;
            if (leftNode is ConditionalAccessExpressionSyntax leftConditionalAccessExpression)
            {
                // If node on the left is a conditional access expression, get the member binding expression 
                // from the innermost conditional access expression, which is the left of < operator. 
                // e.g: Case a?.b?.c< : we need to get the conditional access expression .b?.c and analyze its
                // member binding expression (the .c) to see if it is a generic type/method.
                // Case a?.b?.c.d< : we need to analyze .c.d
                // Case a?.M(x => x?.P)?.M2< : We need to analyze .M2
                var innerMostConditionalAccessExpression = leftConditionalAccessExpression.GetInnerMostConditionalAccessExpression();
                if (innerMostConditionalAccessExpression != null)
                    leftNode = innerMostConditionalAccessExpression.WhenNotNull;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(leftNode.SpanStart, cancellationToken).ConfigureAwait(false);
            var info = semanticModel.GetSymbolInfo(leftNode, cancellationToken);
            return info.CandidateSymbols.Any(IsGenericTypeOrMethod);
        }

        private static bool IsGenericTypeOrMethod(ISymbol symbol)
            => symbol.GetArity() > 0;
    }
}
