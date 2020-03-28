﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class FalseKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public FalseKeywordRecommender()
            : base(SyntaxKind.FalseKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsAnyExpressionContext ||
                context.IsPreProcessorExpressionContext ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.TargetToken.IsUnaryOperatorContext();
        }
    }
}
