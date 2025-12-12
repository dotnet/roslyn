' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicPreferIsKindFix))>
    <[Shared]>
    Public NotInheritable Class BasicPreferIsKindFix
        Inherits PreferIsKindFix

        <ImportingConstructor>
        <Obsolete("This exported object must be obtained through the MEF export provider.", True)>
        Public Sub New()
        End Sub

        Protected Overrides Function TryGetNodeToFix(root As SyntaxNode, span As TextSpan) As SyntaxNode
            Dim binaryExpression = root.FindNode(span, getInnermostNodeForTie:=True).FirstAncestorOrSelf(Of BinaryExpressionSyntax)()
            If binaryExpression.Left.IsKind(SyntaxKind.InvocationExpression) OrElse
                binaryExpression.Left.IsKind(SyntaxKind.SimpleMemberAccessExpression) OrElse
                binaryExpression.Left.IsKind(SyntaxKind.ConditionalAccessExpression) Then
                Return binaryExpression
            End If

            Return Nothing
        End Function

        Protected Overrides Sub FixDiagnostic(editor As DocumentEditor, nodeToFix As SyntaxNode)
            editor.ReplaceNode(
                nodeToFix,
                Function(nodeToFix2, generator)
                    Dim binaryExpression = DirectCast(nodeToFix2, BinaryExpressionSyntax)
                    Dim invocation = TryCast(binaryExpression.Left, InvocationExpressionSyntax)
                    invocation = If(invocation, TryConvertMemberAccessToInvocation(binaryExpression.Left))
                    invocation = If(invocation, TryConvertConditionalAccessToInvocation(binaryExpression.Left))
                    If invocation Is Nothing Then
                        Return binaryExpression
                    End If

                    Dim newInvocation = invocation _
                        .WithExpression(ConvertKindNameToIsKind(invocation.Expression)) _
                        .AddArgumentListArguments(SyntaxFactory.SimpleArgument(binaryExpression.Right.WithoutTrailingTrivia())) _
                        .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia())
                    Dim negate = binaryExpression.OperatorToken.IsKind(SyntaxKind.LessThanGreaterThanToken)
                    If negate Then
                        Return SyntaxFactory.NotExpression(newInvocation.WithoutLeadingTrivia()).WithLeadingTrivia(newInvocation.GetLeadingTrivia())
                    Else
                        Return newInvocation
                    End If
                End Function)
        End Sub

        Private Shared Function TryConvertMemberAccessToInvocation(expression As ExpressionSyntax) As InvocationExpressionSyntax
            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression IsNot Nothing Then
                Return SyntaxFactory.InvocationExpression(memberAccessExpression.WithoutTrailingTrivia()) _
                    .WithTrailingTrivia(memberAccessExpression.GetTrailingTrivia())
            Else
                Return Nothing
            End If
        End Function

        Private Shared Function TryConvertConditionalAccessToInvocation(expression As ExpressionSyntax) As InvocationExpressionSyntax
            Dim conditionalAccessExpression = TryCast(expression, ConditionalAccessExpressionSyntax)
            If conditionalAccessExpression IsNot Nothing Then
                Dim simpleMemberAccess = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    conditionalAccessExpression.Expression.WithoutTrailingTrivia(),
                    SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName("Kind"))
                Return SyntaxFactory.InvocationExpression(simpleMemberAccess.WithTrailingTrivia(conditionalAccessExpression.GetTrailingTrivia()))
            Else
                Return Nothing
            End If
        End Function

        Private Shared Function ConvertKindNameToIsKind(expression As ExpressionSyntax) As ExpressionSyntax
            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression IsNot Nothing Then
                Return memberAccessExpression.WithName(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("IsKind")))
            Else
                Return expression
            End If
        End Function
    End Class
End Namespace
