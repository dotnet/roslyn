' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicPreferIsKindFix))>
    <[Shared]>
    Public NotInheritable Class BasicPreferIsKindFix
        Inherits PreferIsKindFix

        Protected Overrides Async Function ConvertKindToIsKindAsync(document As Document, sourceSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim binaryExpression = root.FindNode(sourceSpan, getInnermostNodeForTie:=True).FirstAncestorOrSelf(Of BinaryExpressionSyntax)()
            Dim invocation = TryCast(binaryExpression.Left, InvocationExpressionSyntax)
            invocation = If(invocation, TryConvertMemberAccessToInvocation(binaryExpression.Left))
            If invocation Is Nothing Then
                Return document
            End If

            Dim newInvocation = invocation _
                .WithExpression(ConvertKindNameToIsKind(invocation.Expression)) _
                .AddArgumentListArguments(SyntaxFactory.SimpleArgument(binaryExpression.Right.WithoutTrailingTrivia())) _
                .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia())
            Dim negate = binaryExpression.OperatorToken.IsKind(SyntaxKind.LessThanGreaterThanToken)
            Dim newRoot As SyntaxNode
            If negate Then
                newRoot = root.ReplaceNode(binaryExpression, SyntaxFactory.NotExpression(newInvocation.WithoutLeadingTrivia()).WithLeadingTrivia(newInvocation.GetLeadingTrivia()))
            Else
                newRoot = root.ReplaceNode(binaryExpression, newInvocation)
            End If

            Return document.WithSyntaxRoot(newRoot)
        End Function

        Private Shared Function TryConvertMemberAccessToInvocation(expression As ExpressionSyntax) As InvocationExpressionSyntax
            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression IsNot Nothing Then
                Return SyntaxFactory.InvocationExpression(memberAccessExpression.WithoutTrailingTrivia()) _
                    .WithTrailingTrivia(memberAccessExpression.GetTrailingTrivia())
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
