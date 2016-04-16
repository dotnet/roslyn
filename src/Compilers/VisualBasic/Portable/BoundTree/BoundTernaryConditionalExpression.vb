' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundTernaryConditionalExpression

#If DEBUG Then
        Private Sub Validate()
            ValidateConstantValue()

            If Not HasErrors Then
                Condition.AssertRValue()
                If (Not Type.IsVoidType) Then
                    WhenTrue.AssertRValue()
                    WhenFalse.AssertRValue()
                End If
                Debug.Assert(Condition.IsNothingLiteral() OrElse Condition.Type.IsBooleanType() OrElse Condition.Type.IsReferenceType())
                Debug.Assert(WhenTrue.Type.IsSameTypeIgnoringCustomModifiers(WhenFalse.Type))
                Debug.Assert(Type.IsSameTypeIgnoringCustomModifiers(WhenTrue.Type))
            End If
        End Sub
#End If

    End Class

End Namespace
