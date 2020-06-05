﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                context.IsFunctionPointerTypeArgumentContext ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.RefKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ReadOnlyKeyword, cancellationToken) ||
                syntaxTree.IsUsingAliasContext(position, cancellationToken);
        }
    }
}
