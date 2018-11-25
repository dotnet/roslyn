' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryParentheses
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicRemoveUnnecessaryParenthesesDiagnosticAnalyzer
        Inherits AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer(Of SyntaxKind, ParenthesizedExpressionSyntax)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetSyntaxNodeKind() As SyntaxKind
            Return SyntaxKind.ParenthesizedExpression
        End Function

        Protected Overrides Function CanRemoveParentheses(
                parenthesizedExpression As ParenthesizedExpressionSyntax, semanticModel As SemanticModel,
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
                precedence = GetPrecedenceKind(parentBinary)
                clarifiesPrecedence = Not innerExpressionIsSimple AndAlso
                                      parentBinary.GetOperatorPrecedence() <> innerExpressionPrecedence
                Return True
            End If

            precedence = PrecedenceKind.Other
            clarifiesPrecedence = False
            Return True
        End Function

        Public Shared Function GetPrecedenceKind(binary As BinaryExpressionSyntax) As PrecedenceKind
            Dim precedence = binary.GetOperatorPrecedence()
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
