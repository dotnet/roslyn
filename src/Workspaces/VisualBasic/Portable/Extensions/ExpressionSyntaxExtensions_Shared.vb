' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module ExpressionSyntaxExtensions
        <Extension()>
        Public Function IsRightSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Right Is expression
        End Function

        <Extension()>
        Public Function IsLeftSideOfQualifiedName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso
                   DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression
        End Function

        <Extension()>
        Public Function IsMemberAccessExpressionName(expression As ExpressionSyntax) As Boolean
            Return expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                   DirectCast(expression.Parent, MemberAccessExpressionSyntax).Name Is expression
        End Function

        <Extension()>
        Public Function WalkUpParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsParentKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression.Parent, ExpressionSyntax)
            End While

            Return expression
        End Function

        <Extension()>
        Public Function WalkDownParentheses(expression As ExpressionSyntax) As ExpressionSyntax
            While expression.IsKind(SyntaxKind.ParenthesizedExpression)
                expression = DirectCast(expression, ParenthesizedExpressionSyntax).Expression
            End While

            Return expression
        End Function

        <Extension()>
        Public Function Parenthesize(expression As ExpressionSyntax, Optional addSimplifierAnnotation As Boolean = True) As ParenthesizedExpressionSyntax
            Dim result = SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia()) _
                                      .WithTriviaFrom(expression)
            Return If(addSimplifierAnnotation,
                      result.WithAdditionalAnnotations(Simplifier.Annotation),
                      result)
        End Function

        <Extension()>
        Public Function IsLeftSideOfDot(expression As ExpressionSyntax) As Boolean
            If expression Is Nothing Then
                Return False
            End If

            Return _
                (expression.IsParentKind(SyntaxKind.QualifiedName) AndAlso DirectCast(expression.Parent, QualifiedNameSyntax).Left Is expression) OrElse
                (expression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso DirectCast(expression.Parent, MemberAccessExpressionSyntax).Expression Is expression)
        End Function

    End Module
End Namespace
