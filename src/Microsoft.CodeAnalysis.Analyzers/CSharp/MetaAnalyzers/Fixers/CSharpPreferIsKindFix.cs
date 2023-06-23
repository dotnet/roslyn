// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpPreferIsKindFix))]
    [Shared]
    public sealed class CSharpPreferIsKindFix : PreferIsKindFix
    {
        protected override SyntaxNode? TryGetNodeToFix(SyntaxNode root, TextSpan span)
        {
            var binaryExpression = root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<BinaryExpressionSyntax>();
            if (binaryExpression is null)
                return null;

            if (binaryExpression.Left.IsKind(SyntaxKind.InvocationExpression) ||
                binaryExpression.Left.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                return binaryExpression;
            }

            return null;
        }

        protected override void FixDiagnostic(DocumentEditor editor, SyntaxNode nodeToFix)
        {
            editor.ReplaceNode(
                nodeToFix,
                (nodeToFix, generator) =>
                {
                    var binaryExpression = (BinaryExpressionSyntax)nodeToFix;
                    InvocationExpressionSyntax? newInvocation = null;
                    if (binaryExpression.Left.IsKind(SyntaxKind.ConditionalAccessExpression))
                    {
                        var conditionalAccess = (ConditionalAccessExpressionSyntax)binaryExpression.Left;
                        newInvocation = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                conditionalAccess.Expression,
                                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("IsKind"))));
                    }
                    else if (binaryExpression.Left.IsKind(SyntaxKind.InvocationExpression))
                    {
                        var invocation = (InvocationExpressionSyntax)binaryExpression.Left;
                        newInvocation = invocation.WithExpression(ConvertKindNameToIsKind(invocation.Expression));
                    }
                    else
                    {
                        Debug.Fail("Unreachable.");
                        return nodeToFix;
                    }

                    newInvocation = newInvocation
                        .AddArgumentListArguments(SyntaxFactory.Argument(binaryExpression.Right.WithoutTrailingTrivia()))
                        .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia());
                    var negate = binaryExpression.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken);
                    if (negate)
                    {
                        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, newInvocation.WithoutLeadingTrivia()).WithLeadingTrivia(newInvocation.GetLeadingTrivia());
                    }
                    else
                    {
                        return newInvocation;
                    }
                });
        }

        private static ExpressionSyntax ConvertKindNameToIsKind(ExpressionSyntax expression)
        {
            if (expression is MemberAccessExpressionSyntax memberAccessExpression)
            {
                return memberAccessExpression.WithName(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("IsKind")));
            }
            else
            {
                return expression;
            }
        }
    }
}
