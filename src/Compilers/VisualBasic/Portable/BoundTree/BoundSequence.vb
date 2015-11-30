' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundSequence

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Debug.Assert(ValueOpt IsNot Nothing OrElse HasErrors OrElse Type.SpecialType = SpecialType.System_Void)
                Return ValueOpt IsNot Nothing AndAlso ValueOpt.IsLValue
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundSequence
            If IsLValue Then
                Debug.Assert(ValueOpt IsNot Nothing)
                Return Update(_Locals, _SideEffects, ValueOpt.MakeRValue(), Type)
            End If

            Return Me
        End Function

#If DEBUG Then
        Private Sub Validate()
            If ValueOpt Is Nothing Then
                Debug.Assert(Type.IsVoidType())
            Else
                Debug.Assert(Type.IsSameTypeIgnoringCustomModifiers(ValueOpt.Type))
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
