' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer
        Inherits AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer(Of SyntaxKind, ParenthesizedExpressionSyntax)

        Protected Overrides Function GetSyntaxNodeKind() As SyntaxKind
            Return SyntaxKind.ParenthesizedExpression
        End Function

        Protected Overrides Function CanRemoveParentheses(
                parenthesizedExpression As ParenthesizedExpressionSyntax, semanticModel As SemanticModel,
                ByRef precedenceKind As PrecedenceKind, ByRef clarifiesPrecedence As Boolean) As Boolean

            Return CanRemoveParenthesesHelper(parenthesizedExpression, semanticModel, precedenceKind, clarifiesPrecedence)
        End Function

        Public Shared Function CanRemoveParenthesesHelper(
                parenthesizedExpression As ParenthesizedExpressionSyntax, semanticModel As SemanticModel,
                ByRef precedenceKind As PrecedenceKind, ByRef clarifiesPrecedence As Boolean) As Boolean
            Dim result = parenthesizedExpression.CanRemoveParentheses(semanticModel)
            If result Then
                Dim parentBinary = TryCast(parenthesizedExpression.Parent, BinaryExpressionSyntax)
                If parentBinary IsNot Nothing Then
                    precedenceKind = GetPrecedenceKind(parentBinary, semanticModel)
                    clarifiesPrecedence = parentBinary.GetOperatorPrecedence() <> parenthesizedExpression.Expression.GetOperatorPrecedence()
                    Return True
                End If

                precedenceKind = PrecedenceKind.Other
                Return True
            End If

            precedenceKind = Nothing
            clarifiesPrecedence = False
            Return False
        End Function

        Public Shared Function GetPrecedenceKind(parentBinary As BinaryExpressionSyntax, semanticModel As SemanticModel) As PrecedenceKind
            Dim precedence = parentBinary.GetOperatorPrecedence()
            Select Case precedence
                Case OperatorPrecedence.PrecedenceXor,
                     OperatorPrecedence.PrecedenceOr,
                     OperatorPrecedence.PrecedenceAnd
                    Return PrecedenceKind.Logical

                Case OperatorPrecedence.PrecedenceRelational
                    Return PrecedenceKind.Relational

                Case OperatorPrecedence.PrecedenceShift
                    Return PrecedenceKind.Shift

                Case OperatorPrecedence.PrecedenceConcatenate,
                     OperatorPrecedence.PrecedenceAdd,
                     OperatorPrecedence.PrecedenceModulus,
                     OperatorPrecedence.PrecedenceIntegerDivide,
                     OperatorPrecedence.PrecedenceMultiply,
                     OperatorPrecedence.PrecedenceExponentiate
                    Return PrecedenceKind.Arithmetic
            End Select

            Throw ExceptionUtilities.UnexpectedValue(precedence)
        End Function
    End Class
End Namespace
