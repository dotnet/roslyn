' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Precedence
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Precedence
    Friend Class VisualBasicPrecedenceService
        Inherits AbstractPrecedenceService(Of ExpressionSyntax, OperatorPrecedence)

        Public Shared ReadOnly Instance As New VisualBasicPrecedenceService()

        Private Sub New()
        End Sub

        Public Overrides Function GetOperatorPrecedence(expression As ExpressionSyntax) As OperatorPrecedence
            Return expression.GetOperatorPrecedence()
        End Function

        Public Overrides Function GetPrecedenceKind(operatorPrecedence As OperatorPrecedence) As PrecedenceKind
            Select Case operatorPrecedence
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

                Case Else
                    Return PrecedenceKind.Other
            End Select
        End Function
    End Class
End Namespace
