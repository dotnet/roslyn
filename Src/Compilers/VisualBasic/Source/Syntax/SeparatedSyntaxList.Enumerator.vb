
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Partial Public Structure SeparatedSyntaxList(Of TNode As SyntaxNode)

        ' Public struct enumerator
        ' Only implements enumerator pattern as used by foreach
        ' Does not implement IEnumerator. Doing so would require the struct to implment IDisposable too.
        Public Structure Enumerator
            Private ReadOnly _list As SeparatedSyntaxList(Of TNode)
            Private _current As Integer

            Friend Sub New(list As SeparatedSyntaxList(Of TNode))
                Me._list = list
                Me._current = -1
            End Sub

            Public Function MoveNext() As Boolean
                Dim newCnt = Me._current + 1
                If newCnt < _list.Count Then
                    Me._current = newCnt
                    Return True
                End If
                Return False
            End Function

            Public ReadOnly Property Current As TNode
                Get
                    Return _list(Me._current)
                End Get
            End Property

            Public Sub Reset()
                _current = -1
            End Sub
        End Structure

        ' IEnumerator wrapper for Enumerator.
        Private Class EnumeratorImpl
            Implements IEnumerator(Of TNode)

            Private _e As Enumerator

            Friend Sub New(list As SeparatedSyntaxList(Of TNode))
                Me._e = New Enumerator(list)
            End Sub

            Public ReadOnly Property Current As TNode Implements IEnumerator(Of TNode).Current
                Get
                    Return _e.Current
                End Get
            End Property

            Public ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return _e.Current
                End Get
            End Property

            Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                Return _e.MoveNext
            End Function

            Private Sub Dispose() Implements IDisposable.Dispose
            End Sub

            Private Sub Reset() Implements IEnumerator.Reset
                _e.Reset()
            End Sub
        End Class
    End Structure
End Namespace