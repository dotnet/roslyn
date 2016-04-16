' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundSpillSequence

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return If(Me.ValueOpt IsNot Nothing, Me.ValueOpt.IsLValue, False)
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundSpillSequence
            If Me.IsLValue Then
                Debug.Assert(Me.ValueOpt IsNot Nothing)
                Return Update(Locals, SpillFields, Statements, ValueOpt.MakeRValue(), Type)
            End If

            Return Me
        End Function

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Me.ValueOpt Is Nothing OrElse Me.ValueOpt.Kind <> BoundKind.SpillSequence)
        End Sub
#End If

    End Class

End Namespace
