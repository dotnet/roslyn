// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class DelegateKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public DelegateKeywordRecommender()
            : base(SyntaxKind.DelegateKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsGlobalStatementContext ||
                (context.IsNonAttributeExpressionContext && !context.IsConstantExpressionContext) ||
                IsAfterAsyncKeywordInExpressionContext(context, cancellationToken) ||
                context.IsTypeContext;
        }

        private static bool IsAfterAsyncKeywordInExpressionContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) &&
                context.SyntaxTree.IsExpressionContext(
                    context.TargetToken.SpanStart,
                    context.TargetToken,
                    attributes: false,
                    cancellationToken: cancellationToken,
                    semanticModelOpt: context.SemanticModel);
        }
    }
}
