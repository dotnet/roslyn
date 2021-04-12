﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
