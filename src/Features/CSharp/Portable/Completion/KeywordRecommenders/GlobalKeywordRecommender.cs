// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class GlobalKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public GlobalKeywordRecommender()
            : base(SyntaxKind.GlobalKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;

            if (syntaxTree.IsMemberDeclarationContext(position, context.LeftToken))
            {
                var token = context.TargetToken;

                if (token.GetAncestor<EnumDeclarationSyntax>() == null)
                {
                    return true;
                }
            }

            return
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsAnyExpressionContext ||
                context.IsObjectCreationTypeContext ||
                context.IsIsOrAsTypeContext ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.RefKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ReadOnlyKeyword, cancellationToken) ||
                syntaxTree.IsUsingAliasContext(position, cancellationToken);
        }
    }
}
