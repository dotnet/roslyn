Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class BoundMeReference
        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return If(Me.MeSymbolOpt Is Nothing, New MeParameterSymbol(Nothing, Me.Type), Me.MeSymbolOpt)
            End Get
        End Property

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                If MeSymbolOpt Is Nothing Then
                    ' If the Me symbol is not there, we must have be in a case where we can't bind use Me.
                    Return LookupResultKind.NotReferencable
                Else
                    Return LookupResultKind.Good
                End If
            End Get
        End Property
    End Class

End Namespace

