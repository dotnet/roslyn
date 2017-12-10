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
            var node = context.TargetToken.Parent;

            // In case of an empty string
            if (node == null)
            {
                return false;
            }

            // After a cast
            if (context.TargetToken.IsKind(SyntaxKind.CloseParenToken) &&
                (node.IsKind(SyntaxKind.ParenthesizedExpression) || node.IsKind(SyntaxKind.CastExpression)))
            {
                node = node.Parent;
            }

            // Inside a conditional expression: value ? stackalloc : stackalloc
            while (node.IsKind(SyntaxKind.ConditionalExpression) &&
                (context.TargetToken.IsKind(SyntaxKind.CloseParenToken) || context.TargetToken.IsKind(SyntaxKind.QuestionToken) || context.TargetToken.IsKind(SyntaxKind.ColonToken)))
            {
                node = node.Parent;
            }

            // assignment: x = stackalloc
            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return node.Parent.IsKind(SyntaxKind.ExpressionStatement);
            }

            // declaration: var x = stackalloc
            if (node.IsKind(SyntaxKind.EqualsValueClause))
            {
                node = node.Parent;

                if (node.IsKind(SyntaxKind.VariableDeclarator))
                {
                    node = node.Parent;

                    if (node.IsKind(SyntaxKind.VariableDeclaration))
                    {
                        node = node.Parent;

                        return node.IsKind(SyntaxKind.LocalDeclarationStatement) || node.IsKind(SyntaxKind.ForStatement);
                    }
                }
            }

            return false;
        }
    }
}
