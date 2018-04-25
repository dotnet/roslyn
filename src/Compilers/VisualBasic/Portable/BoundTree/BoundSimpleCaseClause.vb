' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundSimpleCaseClause

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert((ValueOpt Is Nothing) Xor (ConditionOpt Is Nothing))

            If ConditionOpt IsNot Nothing Then
                Select Case ConditionOpt.Kind
                    Case BoundKind.BinaryOperator
                        Dim binaryOp As BoundBinaryOperator = DirectCast(ConditionOpt, BoundBinaryOperator)
                        Debug.Assert((binaryOp.OperatorKind And VisualBasic.BinaryOperatorKind.OpMask) = VisualBasic.BinaryOperatorKind.Equals)

                    Case BoundKind.UserDefinedBinaryOperator
                        Dim binaryOp As BoundUserDefinedBinaryOperator = DirectCast(ConditionOpt, BoundUserDefinedBinaryOperator)
                        Debug.Assert((binaryOp.OperatorKind And VisualBasic.BinaryOperatorKind.OpMask) = VisualBasic.BinaryOperatorKind.Equals)

                    Case Else
                        ExceptionUtilities.UnexpectedValue(ConditionOpt.Kind) ' This is going to assert
                End Select
            End If

        End Sub
#End If

    End Class
End Namespace
