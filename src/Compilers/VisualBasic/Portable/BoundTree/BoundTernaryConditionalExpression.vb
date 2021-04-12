' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundTernaryConditionalExpression

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()

            If Not HasErrors Then
                Condition.AssertRValue()
                If (Not Type.IsVoidType) Then
                    WhenTrue.AssertRValue()
                    WhenFalse.AssertRValue()
                End If
                Debug.Assert(Condition.IsNothingLiteral() OrElse Condition.Type.IsBooleanType() OrElse Not Condition.Type.IsValueType)
                Debug.Assert(WhenTrue.Type.IsSameTypeIgnoringAll(WhenFalse.Type))
                Debug.Assert(Type.IsSameTypeIgnoringAll(WhenTrue.Type))
            End If
        End Sub
#End If

    End Class

End Namespace
