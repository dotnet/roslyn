// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable;

internal partial class CSharpIntroduceVariableService
{
    private class Rewriter : CSharpSyntaxRewriter
    {
        private readonly SyntaxAnnotation _replacementAnnotation = new SyntaxAnnotation();
        private readonly SyntaxNode _replacementNode;
        private readonly ISet<ExpressionSyntax> _matches;

        private Rewriter(SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
        {
            _replacementNode = replacementNode;
            _matches = matches;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node is ExpressionSyntax expression &&
                _matches.Contains(expression))
            {
                return _replacementNode
                    .WithLeadingTrivia(expression.GetLeadingTrivia())
                    .WithTrailingTrivia(expression.GetTrailingTrivia())
                    .WithAdditionalAnnotations(_replacementAnnotation);
            }

            return base.Visit(node);
        }

        public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            var newNode = base.VisitParenthesizedExpression(node);
            if (node != newNode &&
                newNode is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                var innerExpression = parenthesizedExpression.OpenParenToken.GetNextToken().Parent;
                if (innerExpression.HasAnnotation(_replacementAnnotation))
                {
                    return newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                }
            }

            return newNode;
        }

        public static SyntaxNode Visit(SyntaxNode node, SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
            => new Rewriter(replacementNode, matches).Visit(node);
    }
}
