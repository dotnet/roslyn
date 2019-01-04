// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
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

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                // Ignore name node in member access expression, since we can't replace it with the new variable.
                // For example:
                //
                //  class C
                //  {
                //      C c;
                //      void Test()
                //      {
                //          var x = [|c|].c.c;
                //      }
                //  }

                var expression = (ExpressionSyntax)Visit(node.Expression);
                return node.Update(expression, node.OperatorToken, node.Name);
            }

            public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            {
                var newNode = base.VisitParenthesizedExpression(node);
                if (node != newNode &&
                    newNode.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    var parenthesizedExpression = (ParenthesizedExpressionSyntax)newNode;
                    var innerExpression = parenthesizedExpression.OpenParenToken.GetNextToken().Parent;
                    if (innerExpression.HasAnnotation(_replacementAnnotation))
                    {
                        return newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                    }
                }

                return newNode;
            }

            public static SyntaxNode Visit(SyntaxNode node, SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
            {
                return new Rewriter(replacementNode, matches).Visit(node);
            }
        }
    }
}
