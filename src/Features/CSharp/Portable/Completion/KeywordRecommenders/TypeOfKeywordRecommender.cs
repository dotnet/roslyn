// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class TypeOfKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public TypeOfKeywordRecommender()
            : base(SyntaxKind.TypeOfKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                (context is { IsAnyExpressionContext: true, IsConstantExpressionContext: false }) ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                IsAttributeArgumentContext(context);
        }

        private bool IsAttributeArgumentContext(CSharpSyntaxContext context)
        {
            return
                context.IsAnyExpressionContext &&
                context.LeftToken.GetAncestor<AttributeSyntax>() != null;
        }
    }
}
