// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class NullKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public NullKeywordRecommender()
            : base(SyntaxKind.NullKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;

            return
                context.IsAnyExpressionContext ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                IsInSelectCaseContext(context, cancellationToken);
        }

        private bool IsInSelectCaseContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;
            if (token.Kind() != SyntaxKind.CaseKeyword)
            {
                return false;
            }

            var switchStatement = token.GetAncestor<SwitchStatementSyntax>();
            if (switchStatement != null)
            {
                var info = context.SemanticModel.GetTypeInfo(switchStatement.Expression, cancellationToken);
                if (info.Type != null &&
                    info.Type.IsValueType)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
