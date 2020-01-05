' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundTernaryConditionalExpression
        Implements IBoundConditional

        Private ReadOnly Property IBoundConditional_Condition As BoundExpression Implements IBoundConditional.Condition
            Get
                Return Me.Condition
            End Get
        End Property

        Private ReadOnly Property IBoundConditional_WhenTrue As BoundNode Implements IBoundConditional.WhenTrue
            Get
                Return Me.WhenTrue
            End Get
        End Property

        Private ReadOnly Property IBoundConditional_WhenFalseOpt As BoundNode Implements IBoundConditional.WhenFalseOpt
            Get
                Return Me.WhenFalse
            End Get
        End Property

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
