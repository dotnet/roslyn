Imports System.Collections
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

#If REMOVE Then
    Partial Public Structure SyntaxTriviaList
        Implements IEquatable(Of SyntaxTriviaList), IEnumerable(Of SyntaxTrivia), IEnumerable

        Public Structure Enumerator
            Private ReadOnly _token As SyntaxToken
            Private ReadOnly _singleNodeOrList As InternalSyntax.VisualBasicSyntaxNode
            Private ReadOnly _baseIndex As Integer
            Private ReadOnly _count As Integer

            Private _index As Integer
            Private _current As InternalSyntax.VisualBasicSyntaxNode
            Private _position As Integer

            Friend Sub New(ByRef list As SyntaxTriviaList)
                _token = list._token
                _singleNodeOrList = list._node
                _baseIndex = list._index
                _count = list.Count

                _index = -1
                _current = Nothing
                _position = list._position
            End Sub

            Public Function MoveNext() As Boolean
                If _count = 0 OrElse _count <= _index + 1 Then
                    ' invalidate iterator
                    _current = Nothing
                    Return False
                End If

                _index += 1

                If _current IsNot Nothing Then
                    _position += _current.FullWidth
                End If

                _current = GetGreenNodeAt(_singleNodeOrList, _index)
                Return True
            End Function

            Public ReadOnly Property Current As SyntaxTrivia
                Get
                    If _current Is Nothing Then
                        Throw New InvalidOperationException()
                    End If

                    Return New SyntaxTrivia(_token, _current, _position, _baseIndex + _index)
                End Get
            End Property
        End Structure

        Private Class EnumeratorImpl
            Implements IEnumerator(Of SyntaxTrivia), IDisposable, IEnumerator

            Private _enum As Enumerator

            ' SyntaxTriviaList is a relatively big struct so we will pass it byref
            Friend Sub New(ByRef list As SyntaxTriviaList)
                Me._enum = New Enumerator(list)
            End Sub

            Public ReadOnly Property Current As SyntaxTrivia Implements IEnumerator(Of SyntaxTrivia).Current
                Get
                    Return _enum.Current
                End Get
            End Property

            Public ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return _enum.Current
                End Get
            End Property

            Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                Return _enum.MoveNext
            End Function

            Public Sub Reset() Implements IEnumerator.Reset
                Throw New NotSupportedException
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class
    End Structure
#End If
End Namespace