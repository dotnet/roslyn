// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class StackAllocKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public StackAllocKeywordRecommender()
            : base(SyntaxKind.StackAllocKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // type t = |
            var token = context.TargetToken;
            if (token.IsUnsafeContext())
            {
                if (token.Kind() == SyntaxKind.EqualsToken &&
                    token.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
                    token.Parent.IsParentKind(SyntaxKind.VariableDeclarator) &&
                    token.Parent.Parent.IsParentKind(SyntaxKind.VariableDeclaration))
                {
                    var variableDeclaration = (VariableDeclarationSyntax)token.Parent.Parent.Parent;
                    if (variableDeclaration.IsParentKind(SyntaxKind.LocalDeclarationStatement) ||
                        variableDeclaration.IsParentKind(SyntaxKind.ForStatement))
                    {
                        return variableDeclaration.Type.IsVar || variableDeclaration.Type.IsKind(SyntaxKind.PointerType);
                    }
                }
            }

            return false;
        }
    }
}
