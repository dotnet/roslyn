Namespace Roslyn.Compilers.VisualBasic

    Partial Class BoundAsEnumerableOperator

        Public Sub New([call] As BoundCall)
            Me.New([call].Syntax, [call], [call].Type)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return AsEnumerableCall.ExpressionSymbol
            End Get
        End Property

    End Class

End Namespace