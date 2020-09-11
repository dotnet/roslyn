' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundSequence

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Debug.Assert(Me.ValueOpt IsNot Nothing OrElse Me.HasErrors OrElse Me.Type.SpecialType = SpecialType.System_Void)
                Return Me.ValueOpt IsNot Nothing AndAlso Me.ValueOpt.IsLValue
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundSequence
            If Me.IsLValue Then
                Debug.Assert(Me.ValueOpt IsNot Nothing)
                Return Update(_Locals, _SideEffects, Me.ValueOpt.MakeRValue(), Type)
            End If

            Return Me
        End Function

#If DEBUG Then
        Private Sub Validate()
            If ValueOpt Is Nothing Then
                Debug.Assert(Type.IsVoidType())
            Else
                Debug.Assert(Type.IsSameTypeIgnoringAll(ValueOpt.Type))
                If Not ValueOpt.IsLValue Then
                    ValueOpt.AssertRValue() ' Value must return a result, if it doesn't, add that expression into side-effects instead.
                End If
            End If

            For Each val In SideEffects
                If Not val.HasErrors Then
                    Debug.Assert(val.IsValue AndAlso Not val.IsLValue AndAlso val.Type IsNot Nothing)
                End If

            Next
        End Sub
#End If

    End Class

End Namespace
