' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundUserDefinedShortCircuitingOperator

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(LeftTest Is Nothing OrElse LeftTest.Type.IsBooleanType())
            Debug.Assert((LeftOperand Is Nothing) = (LeftOperandPlaceholder Is Nothing))
            Debug.Assert(LeftOperand IsNot Nothing OrElse HasErrors)

            If LeftTest IsNot Nothing Then
                Debug.Assert(LeftTest.Kind = BoundKind.UserDefinedUnaryOperator OrElse
                             (LeftTest.Kind = BoundKind.NullableIsTrueOperator AndAlso
                              DirectCast(LeftTest, BoundNullableIsTrueOperator).Operand.Kind = BoundKind.UserDefinedUnaryOperator))
            End If
        End Sub
#End If

    End Class

End Namespace
