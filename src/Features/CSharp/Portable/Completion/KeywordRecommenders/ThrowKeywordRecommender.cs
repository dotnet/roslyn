// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ThrowKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ThrowKeywordRecommender()
            : base(SyntaxKind.ThrowKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsStatementContext || context.IsGlobalStatementContext)
            {
                return true;
            }

            // void M() => throw
            if (context.TargetToken.Kind() == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }

            // val ?? throw
            if (context.TargetToken.Kind() == SyntaxKind.QuestionQuestionToken)
            {
                return true;
            }

            //  expr ? throw : ...
            //  expr ? ... : throw
            if (context.TargetToken.Kind() == SyntaxKind.QuestionToken ||
                context.TargetToken.Kind() == SyntaxKind.ColonToken)
            {
                return context.TargetToken.Parent.Kind() == SyntaxKind.ConditionalExpression;
            }

            return false;
        }
    }
}
