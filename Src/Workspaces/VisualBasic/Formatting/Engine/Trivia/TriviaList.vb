Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.VisualBasic.Formatting
    Friend Structure TriviaList
        Private ReadOnly triviaLists() As SyntaxTriviaList
        Private ReadOnly privateCount As Integer

        Public Sub New(tailing As SyntaxTriviaList, leading As SyntaxTriviaList)
            Me.triviaLists = New SyntaxTriviaList() {tailing, leading}

            Me.privateCount = 0
            For i As Integer = 0 To triviaLists.Length - 1
                Me.privateCount += triviaLists(i).Count
            Next i
        End Sub

        Public ReadOnly Property Count() As Integer
            Get
                Return privateCount
            End Get
        End Property

        Default Public ReadOnly Property Item(i As Integer) As SyntaxTrivia
            Get
                Contract.ThrowIfFalse(i >= 0 AndAlso i < Me.Count)

                Dim listIndex As Integer = 0
                Do While listIndex < triviaLists.Length
                    Dim list = triviaLists(listIndex)
                    If i < list.Count Then
                        Return list(i)
                    End If

                    i -= list.Count
                    listIndex += 1
                Loop

                Return Contract.FailWithReturn(Of SyntaxTrivia)("shouldn't reach here")
            End Get
        End Property
    End Structure
End Namespace