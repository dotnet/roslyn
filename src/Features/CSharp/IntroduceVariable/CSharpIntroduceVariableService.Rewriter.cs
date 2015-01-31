// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    internal partial class CSharpIntroduceVariableService
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SyntaxAnnotation replacementAnnotation = new SyntaxAnnotation();
            private readonly SyntaxNode replacementNode;
            private readonly ISet<ExpressionSyntax> matches;

            private Rewriter(SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
            {
                this.replacementNode = replacementNode;
                this.matches = matches;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                var expression = node as ExpressionSyntax;
                if (expression != null &&
                    matches.Contains(expression))
                {
                    return replacementNode
                        .WithLeadingTrivia(expression.GetLeadingTrivia())
                        .WithTrailingTrivia(expression.GetTrailingTrivia())
                        .WithAdditionalAnnotations(replacementAnnotation);
                }

                return base.Visit(node);
            }

            public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            {
                var newNode = base.VisitParenthesizedExpression(node);
                if (node != newNode &&
                    newNode.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)newNode;
                    var innerExpression = parenthesizedExpression.OpenParenToken.GetNextToken().Parent;
                    if (innerExpression.HasAnnotation(replacementAnnotation))
                    {
                        return newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                    }
                }

                return newNode;
            }

            public static SyntaxNode Visit(SyntaxNode node, SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
            {
                return new Rewriter(replacementNode, matches).Visit((SyntaxNode)node);
            }
        }
    }
}
