// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class CheckedKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public CheckedKeywordRecommender()
            : base(SyntaxKind.CheckedKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsNonAttributeExpressionContext)
            {
                return true;
            }

            var targetToken = context.TargetToken;

            if (targetToken.Kind() == SyntaxKind.OperatorKeyword)
            {
                var previousPossiblySkippedToken = targetToken.GetPreviousToken(includeSkipped: true);

                if (previousPossiblySkippedToken.IsLastTokenOfNode<TypeSyntax>())
                {
                    return true;
                }

                SyntaxToken previousToken;

                if (previousPossiblySkippedToken.IsLastTokenOfNode<ExplicitInterfaceSpecifierSyntax>())
                {
                    var firstSpecifierToken = previousPossiblySkippedToken.GetAncestor<ExplicitInterfaceSpecifierSyntax>().GetFirstToken(includeSkipped: true);

                    if (firstSpecifierToken.GetPreviousToken(includeSkipped: true).IsLastTokenOfNode<TypeSyntax>())
                    {
                        return true;
                    }

                    previousToken = firstSpecifierToken.GetPreviousToken(includeSkipped: false);
                }
                else
                {
                    previousToken = targetToken.GetPreviousToken(includeSkipped: false);
                }

                if (previousToken.Kind() == SyntaxKind.ExplicitKeyword)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
