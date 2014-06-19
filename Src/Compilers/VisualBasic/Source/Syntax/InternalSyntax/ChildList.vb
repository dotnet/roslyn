Imports Roslyn.Compilers.Common
Namespace Roslyn.Compilers.VisualBasic.InternalSyntax
    Friend Structure ChildList
        Implements IEnumerable(Of SyntaxNode)

        Private ReadOnly _node As SyntaxNode
        Private _count As Integer

        Friend Sub New(node As SyntaxNode)
            Me._node = node
            Me._count = -1
        End Sub

        Public ReadOnly Property Count As Integer
            Get
                If (Me._count = -1) Then
                    Me._count = Me.CountNodes
                End If
                Return Me._count
            End Get
        End Property

        Private Function CountNodes() As Integer
            Dim n As Integer = 0

            For Each i In Me
                n += 1
            Next

            Return n
        End Function

        Default Public ReadOnly Property Item(index As Integer) As SyntaxNode
            Get
                If ((index < 0) OrElse (index > Me.Count)) Then
                    Throw New IndexOutOfRangeException
                End If
                Dim n As Integer = 0
                Dim child As SyntaxNode
                For Each child In Me
                    If (n = index) Then
                        Return child
                    End If
                    n += 1
                Next
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property Nodes As SyntaxNode()
            Get
                Return Me.ToArray()
            End Get
        End Property

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(Me._node)
        End Function

        Private Function GetEnumerator1() As IEnumerator(Of SyntaxNode) Implements IEnumerable(Of SyntaxNode).GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of SyntaxNode)()
            End If
            Return Me.GetEnumerator
        End Function

        Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of SyntaxNode)()
            End If
            Return Me.GetEnumerator
        End Function

        Public Structure Enumerator
            Implements IEnumerator(Of SyntaxNode), IDisposable, IEnumerator

            Private ReadOnly _node As SyntaxNode
            Private childIndex As Integer
            Private list As SyntaxNode
            Private listIndex As Integer
            Private offset As Integer
            Private _current As SyntaxNode

            Friend Sub New(node As SyntaxNode)
                If (Not node Is Nothing) Then
                    Me._node = node
                    Me.childIndex = -1
                    Me.listIndex = -1
                Else
                    Me._node = Nothing
                    Me.childIndex = 0
                    Me.listIndex = 0
                End If
                Me.list = Nothing
                Me._current = Nothing
                Me.offset = 0
            End Sub

            Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                If (Not Me._current Is Nothing) Then
                    Me.offset = (Me.offset + Me._current.FullWidth)
                End If
                If (Not Me._node Is Nothing) Then
                    If (Not Me.list Is Nothing) Then
                        Me.listIndex += 1
                        If (Me.listIndex < Me.list.SlotCount) Then
                            Me._current = Me.list.GetSlot(Me.listIndex)
                            Return True
                        End If
                        Me.list = Nothing
                        Me.listIndex = -1
                    End If
                    Me.childIndex += 1
                    Do While (Me.childIndex < Me._node.SlotCount)
                        Dim child As SyntaxNode = Me._node.GetSlot(Me.childIndex)
                        If (Not child Is Nothing) Then
                            If child.IsList Then
                                Me.list = child
                                Me.listIndex += 1
                                If (Me.listIndex < Me.list.SlotCount) Then
                                    Me._current = Me.list.GetSlot(Me.listIndex)
                                    Return True
                                End If
                                Me.list = Nothing
                                Me.listIndex = -1
                            Else
                                Me._current = child
                                Return True
                            End If
                        End If
                        Me.childIndex += 1
                    Loop
                End If
                Me._current = Nothing
                Return False
            End Function

            Public ReadOnly Property Current As SyntaxNode Implements IEnumerator(Of SyntaxNode).Current
                Get
                    Return Me._current
                End Get
            End Property

            Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return Me.Current
                End Get
            End Property

            Private Sub Reset() Implements IEnumerator.Reset
            End Sub

            Private Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Structure
    End Structure
End Namespace
