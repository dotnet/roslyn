// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

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

            // At start of a file
            if (node == null)
            {
                return false;
            }

            // After a cast or parenthesized expression: (Span<int>)stackalloc
            if (context.TargetToken.IsAfterPossibleCast())
            {
                node = node.Parent;
            }

            // Inside a conditional expression: value ? stackalloc : stackalloc
            while (node.IsKind(SyntaxKind.ConditionalExpression) &&
                (context.TargetToken.IsKind(SyntaxKind.QuestionToken, SyntaxKind.ColonToken) || context.TargetToken.IsAfterPossibleCast()))
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

                        return node.IsKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement);
                    }
                }
            }

            return false;
        }
    }
}
