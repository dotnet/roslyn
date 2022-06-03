' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundSpillSequence

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
