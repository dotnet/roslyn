
Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundCastOfObjectOperator

        Public Sub New([call] As BoundCall)
            Me.New([call].Syntax, [call], [call].Type)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return CastOfObjectCall.ExpressionSymbol
            End Get
        End Property
    End Class

End Namespace