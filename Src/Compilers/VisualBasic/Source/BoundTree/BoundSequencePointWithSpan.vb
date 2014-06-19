Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Partial Friend Class BoundSequencePointWithSpan

        Public Overrides ReadOnly Property Span As TextSpan
            Get
                Return SequenceSpan
            End Get
        End Property

    End Class

End Namespace

