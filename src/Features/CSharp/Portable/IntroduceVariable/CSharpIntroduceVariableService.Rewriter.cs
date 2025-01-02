// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable;

using static SyntaxFactory;

internal sealed partial class CSharpIntroduceVariableService
{
    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        private static readonly SyntaxAnnotation s_replacementAnnotation = new();

        private readonly SyntaxNode _replacementNode;
        private readonly ISet<ExpressionSyntax> _matches;

        private Rewriter(SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
        {
            _replacementNode = replacementNode;
            _matches = matches;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node is ExpressionSyntax expression && _matches.Contains(expression))
            {
                return _replacementNode
                    .WithLeadingTrivia(expression.GetLeadingTrivia())
                    .WithTrailingTrivia(expression.GetTrailingTrivia())
                    .WithAdditionalAnnotations(s_replacementAnnotation);
            }

            return base.Visit(node);
        }

        public override SyntaxNode? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
        {
            var newNode = (AnonymousObjectMemberDeclaratorSyntax)base.VisitAnonymousObjectMemberDeclarator(node)!;
            if (node.NameEquals == null &&
                newNode != node &&
                newNode.Expression.HasAnnotation(s_replacementAnnotation))
            {
                var inferredName = node.Expression.TryGetInferredMemberName();
                if (inferredName != null)
                {
                    return newNode.WithNameEquals(NameEquals(IdentifierName(inferredName))).WithAdditionalAnnotations(Simplifier.Annotation);
                }
            }

            return newNode;
        }

        public override SyntaxNode? VisitArgument(ArgumentSyntax node)
        {
            var newNode = (ArgumentSyntax)base.VisitArgument(node)!;
            if (node.NameColon == null &&
                newNode != node &&
                newNode.Expression.HasAnnotation(s_replacementAnnotation) &&
                node.Parent is TupleExpressionSyntax)
            {
                var inferredName = node.Expression.TryGetInferredMemberName();
                if (inferredName != null)
                {
                    return newNode.WithNameColon(NameColon(IdentifierName(inferredName))).WithAdditionalAnnotations(Simplifier.Annotation);
                }
            }

            return newNode;
        }

        public override SyntaxNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            var newNode = base.VisitParenthesizedExpression(node);
            if (node != newNode &&
                newNode is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                var innerExpression = parenthesizedExpression.OpenParenToken.GetNextToken().GetRequiredParent();
                if (innerExpression.HasAnnotation(s_replacementAnnotation))
                {
                    return newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                }
            }

            return newNode;
        }

        public static SyntaxNode Visit(SyntaxNode node, SyntaxNode replacementNode, ISet<ExpressionSyntax> matches)
            => new Rewriter(replacementNode, matches).Visit(node)!;
    }
}
