
Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundSequenceValueSideEffects

        Public Overrides ReadOnly Property IsLValue As Boolean
            Get
                Return Me.Value.IsLValue
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundSequenceValueSideEffects
            If Value.IsLValue Then
                Return Update(_LocalsOpt, Value.MakeRValue(), _SideEffects, Type)
            End If

            Return Me
        End Function
    End Class

End Namespace
