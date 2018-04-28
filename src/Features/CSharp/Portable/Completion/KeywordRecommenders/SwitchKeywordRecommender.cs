// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class SwitchKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public SwitchKeywordRecommender()
            : base(SyntaxKind.SwitchKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                IsAfterExpression(context);
        }

        // `switch` follows expressions in switch expressions
        internal static bool IsAfterExpression(CSharpSyntaxContext context)
        {
            var token = context.TargetToken;
            if (token.Parent == null)
            {
                return false;
            }

            var ancestors = token.Parent.AncestorsAndSelf();

            foreach (var ancestor in ancestors)
            {
                if (ancestor is ExpressionSyntax)
                {
                    return true;
                }

                if (ancestor is LambdaExpressionSyntax || ancestor is StatementSyntax)
                {
                    break;
                }
            }

            return false;
        }
    }
}
