Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundMyClassReference
        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.MeSymbolOpt
            End Get
        End Property
    End Class

End Namespace