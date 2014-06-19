Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Partial Structure SyntaxList(Of TNode As SyntaxNode)

        ' Public struct enumerator
        ' Only implements enumerator pattern as used by foreach
        ' Does not implement IEnumerator. Doing so would require the struct to implment IDisposable too.
        Public Structure Enumerator
            Implements IEnumerator(Of TNode)

            Private _list As SyntaxList(Of TNode)
            Private _index As Integer

            Friend Sub New(list As SyntaxList(Of TNode))
                Me._list = list
                Me._index = -1
            End Sub

            Public Function MoveNext() As Boolean Implements IEnumerator(Of TNode).MoveNext
                Me._index += 1
                Return (Me._index < Me._list.Count)
            End Function

            Public ReadOnly Property Current As TNode Implements IEnumerator(Of TNode).Current
                Get
                    Return Me._list.Item(Me._index)
                End Get
            End Property

            Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return Me.Current
                End Get
            End Property

            Public Sub Reset() Implements IEnumerator.Reset
                _index = -1
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Structure

        ' IEnumerator wrapper for Enumerator.
        Public Class EnumeratorImpl
            Implements IEnumerator(Of TNode)

            Private _e As Enumerator

            Friend Sub New(list As SyntaxList(Of TNode))
                Me._e = New Enumerator(list)
            End Sub

            Public Function MoveNext() As Boolean Implements IEnumerator(Of TNode).MoveNext
                Return Me._e.MoveNext
            End Function

            Public ReadOnly Property Current As TNode Implements IEnumerator(Of TNode).Current
                Get
                    Return Me._e.Current
                End Get
            End Property

            Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return Me._e.Current
                End Get
            End Property

            Public Sub Reset() Implements IEnumerator.Reset
                Me._e.Reset()
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class
    End Structure
End Namespace