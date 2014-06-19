Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

#If REMOVE Then
    Partial Structure ChildSyntaxList
        Public Structure Enumerator
            Private ReadOnly _list As ChildSyntaxList
            Private _childIndex As Integer

            Friend Sub New(node As ChildSyntaxList)
                _list = node
                _childIndex = -1
            End Sub

            Public Function MoveNext() As Boolean
                Dim newIndex = _childIndex + 1
                If newIndex < _list.Count Then
                    _childIndex = newIndex
                    Return True
                Else
                    Return False
                End If
            End Function

            Public ReadOnly Property Current As SyntaxNodeOrToken
                Get
                    Return ChildSyntaxList.ItemInternal(_list._node, _childIndex)
                End Get
            End Property

            Public Sub Reset()
                _childIndex = -1
            End Sub
        End Structure

        Private Class EnumeratorImpl
            Implements IEnumerator(Of SyntaxNodeOrToken)

            Private _e As Enumerator

            Friend Sub New(node As ChildSyntaxList)
                _e = New Enumerator(node)
            End Sub

            Public ReadOnly Property Current As SyntaxNodeOrToken Implements IEnumerator(Of SyntaxNodeOrToken).Current
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
                Return _e.MoveNext()
            End Function

            Private Sub Reset() Implements IEnumerator.Reset
                _e.Reset()
            End Sub

            Private Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Class
    End Structure
#End If
End Namespace

