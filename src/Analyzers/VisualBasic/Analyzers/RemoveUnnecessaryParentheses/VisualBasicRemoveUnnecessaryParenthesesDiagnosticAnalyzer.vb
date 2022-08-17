' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Precedence
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.Precedence
Imports Microsoft.CodeAnalysis.LanguageService
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer
        Inherits AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer(Of SyntaxKind, ParenthesizedExpressionSyntax)

        Protected Overrides Function GetSyntaxKind() As SyntaxKind
            Return SyntaxKind.ParenthesizedExpression
        End Function

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function CanRemoveParentheses(
                parenthesizedExpression As ParenthesizedExpressionSyntax,
                semanticModel As SemanticModel, cancellationToken As CancellationToken,
                ByRef precedence As PrecedenceKind, ByRef clarifiesPrecedence As Boolean) As Boolean

            Return CanRemoveParenthesesHelper(
                parenthesizedExpression, semanticModel,
                precedence, clarifiesPrecedence)
        End Function

        Public Shared Function CanRemoveParenthesesHelper(
                parenthesizedExpression As ParenthesizedExpressionSyntax, semanticModel As SemanticModel,
                ByRef precedence As PrecedenceKind, ByRef clarifiesPrecedence As Boolean) As Boolean
            Dim result = parenthesizedExpression.CanRemoveParentheses(semanticModel)
            If Not result Then
                precedence = Nothing
                clarifiesPrecedence = False
                Return False
            End If

            Dim innerExpression = parenthesizedExpression.Expression
            Dim innerExpressionPrecedence = innerExpression.GetOperatorPrecedence()
            Dim innerExpressionIsSimple = innerExpressionPrecedence = OperatorPrecedence.PrecedenceNone

            Dim parentBinary = TryCast(parenthesizedExpression.Parent, BinaryExpressionSyntax)

            If parentBinary IsNot Nothing Then
                precedence = VisualBasicPrecedenceService.Instance.GetPrecedenceKind(parentBinary)
                clarifiesPrecedence = Not innerExpressionIsSimple AndAlso
                                      parentBinary.GetOperatorPrecedence() <> innerExpressionPrecedence
                Return True
            End If

            precedence = PrecedenceKind.Other
            clarifiesPrecedence = False
            Return True
        End Function
    End Class
End Namespace
