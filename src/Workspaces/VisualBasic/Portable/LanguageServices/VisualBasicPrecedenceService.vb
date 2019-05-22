' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(IPrecedenceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicPrecedenceService
        Inherits AbstractPrecedenceService(Of ExpressionSyntax, OperatorPrecedence)

        Public Shared ReadOnly Instance As New VisualBasicPrecedenceService()

        <ImportingConstructor>
        Public Sub New()
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
