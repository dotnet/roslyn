Imports System
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Friend MustInherit Class VisualBasicTriviaData
        Inherits TriviaData

        Public Sub New(options As FormattingOptions)
            MyBase.New(options)
        End Sub

        Public Overridable ReadOnly Property TriviaList() As List(Of SyntaxTrivia)
            Get
                Return Contract.FailWithReturn(Of List(Of SyntaxTrivia))("Should never be called")
            End Get
        End Property
    End Class
End Namespace