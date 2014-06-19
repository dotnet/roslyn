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

    Partial Public Structure SyntaxList(Of TNode As SyntaxNode)
        Implements IReadOnlyList(Of TNode)
        Implements IEquatable(Of SyntaxList(Of TNode))

        Private _node As VisualBasicSyntaxNode

        Friend Sub New(node As SyntaxNode)
            Me._node = DirectCast(node, VisualBasicSyntaxNode)
        End Sub

        Friend ReadOnly Property Node As VisualBasicSyntaxNode
            Get
                Return Me._node
            End Get
        End Property

        Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of TNode).Count
            Get
                Return If((Me._node Is Nothing), 0, If(Me._node.IsList, Me._node.SlotCount, 1))
            End Get
        End Property

        Default Public ReadOnly Property Item(index As Integer) As TNode Implements IReadOnlyList(Of TNode).Item
            Get
                If Me.Any Then
                    Dim node = Me._node
                    If node.IsList Then
                        If CUInt(index) < CUInt(node.SlotCount) Then
                            Return DirectCast(DirectCast(node.GetNodeSlot(index), SyntaxNode), TNode)
                        End If
                    ElseIf index = 0 Then
                        Return DirectCast(DirectCast(node, SyntaxNode), TNode)
                    End If
                End If
                Throw New IndexOutOfRangeException
            End Get
        End Property

        Public Function IndexOf(node As TNode) As Integer
            Dim index = 0
            For Each child In Me
                If Object.Equals(child, node) Then
                    Return index
                End If

                index = index + 1
            Next

            Return -1
        End Function

        Public Function IndexOf(predicate As Func(Of TNode, Boolean)) As Integer
            Dim index = 0
            For Each child In Me
                If predicate(child) Then
                    Return index
                End If

                index = index + 1
            Next

            Return -1
        End Function

        Friend ReadOnly Property ItemInternal(index As Integer) As SyntaxNode
            Get
                Dim node = Me._node
                If node.IsList Then
                    Return node.GetNodeSlot(index)
                End If
                Debug.Assert(index = 0)
                Return node
            End Get
        End Property

        ''' <summary>
        ''' Creates a New list with the specified node added at the end.
        ''' </summary>
        ''' <param name="node">The node to add.</param>
        Public Function Add(node As TNode) As SyntaxList(Of TNode)
            Return Insert(Me.Count, node)
        End Function

        ''' <summary>
        ''' Creates a New list with the specified nodes added at the end.
        ''' </summary>
        ''' <param name="nodes">The nodes to add.</param>
        Public Function AddRange(nodes As IEnumerable(Of TNode)) As SyntaxList(Of TNode)
            Return InsertRange(Me.Count, nodes)
        End Function

        ''' <summary>
        ''' Creates a New list with the specified node inserted at the index.
        ''' </summary>
        ''' <param name="index">The index to insert at.</param>
        ''' <param name="node">The node to insert.</param>
        Public Function Insert(index As Integer, node As TNode) As SyntaxList(Of TNode)
            If node Is Nothing Then
                Throw New ArgumentNullException("node")
            End If
            Return InsertRange(index, {node})
        End Function

        ''' <summary>
        ''' Creates a New list with the specified nodes inserted at the index.
        ''' </summary>
        ''' <param name="index">The index to insert at.</param>
        ''' <param name="nodes">The nodes to insert.</param>
        Public Function InsertRange(index As Integer, nodes As IEnumerable(Of TNode)) As SyntaxList(Of TNode)
            If index < 0 OrElse index > Me.Count Then
                Throw New ArgumentOutOfRangeException("index")
            End If

            If nodes Is Nothing Then
                Throw New ArgumentNullException("nodes")
            End If

            If Me.Count = 0 Then
                Return SyntaxFactory.List(Of TNode)(nodes)
            Else
                Dim list = Me.ToList()
                list.InsertRange(index, nodes)
                Return SyntaxFactory.List(Of TNode)(list)
            End If
        End Function

        ''' <summary>
        ''' Creates a New list with the element at specified index removed.
        ''' </summary>
        ''' <param name="index">The index of the element to remove.</param>
        Public Function RemoveAt(index As Integer) As SyntaxList(Of TNode)
            If index < 0 OrElse index >= Me.Count Then
                Throw New ArgumentOutOfRangeException("index")
            End If

            Return Remove(Me(index))
        End Function

        ''' <summary>
        ''' Creates a New list with the element removed.
        ''' </summary>
        ''' <param name="node">The element to remove.</param>
        Public Function Remove(node As TNode) As SyntaxList(Of TNode)
            Return SyntaxFactory.List(Me.Where(Function(x) x IsNot node))
        End Function

        ''' <summary>
        ''' Creates a New list with the specified element replaced with the New node.
        ''' </summary>
        ''' <param name="nodeInList">The element to replace.</param>
        ''' <param name="newNode">The New node.</param>
        Public Function Replace(nodeInList As TNode, newNode As TNode) As SyntaxList(Of TNode)
            Return ReplaceRange(nodeInList, {newNode})
        End Function

        ''' <summary>
        ''' Creates a New list with the specified element replaced with New nodes.
        ''' </summary>
        ''' <param name="nodeInList">The element to replace.</param>
        ''' <param name="newNodes">The New nodes.</param>
        Public Function ReplaceRange(nodeInList As TNode, newNodes As IEnumerable(Of TNode)) As SyntaxList(Of TNode)
            If nodeInList Is Nothing Then
                Throw New ArgumentNullException("nodeInList")
            End If

            If newNodes Is Nothing Then
                Throw New ArgumentNullException("newNodes")
            End If

            Dim index = IndexOf(nodeInList)
            If index >= 0 AndAlso index < Me.Count Then
                Dim list = Me.ToList()
                list.RemoveAt(index)
                list.InsertRange(index, newNodes)
                Return SyntaxFactory.List(list)
            Else
                Throw New ArgumentException("nodeInList")
            End If
        End Function

        Public Function First() As TNode
            Return Me(0)
        End Function

        Public Function FirstOrDefault() As TNode
            If Me.Any Then
                Return Me(0)
            End If
            Return Nothing
        End Function

        Public Function Last() As TNode
            Return Me(Me.Count - 1)
        End Function

        Public Function LastOrDefault() As TNode
            If Me.Any Then
                Return Me(Me.Count - 1)
            End If
            Return Nothing
        End Function

        Public Function LastIndexOf(node As TNode) As Integer
            For i = Me.Count - 1 To 0 Step -1
                If Object.Equals(Me(i), node) Then
                    Return i
                End If
            Next

            Return -1
        End Function

        Public Function LastIndexOf(predicate As Func(Of TNode, Boolean)) As Integer
            For i = Me.Count - 1 To 0 Step -1
                If predicate(Me(i)) Then
                    Return i
                End If
            Next

            Return -1
        End Function

        Public Function Any() As Boolean
            Debug.Assert(Me._node Is Nothing OrElse Me.Count > 0)
            Return Me._node IsNot Nothing
        End Function

        Public Function Any(kind As SyntaxKind) As Boolean
            For Each element In Me
                If element.VisualBasicKind = kind Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private ReadOnly Property Nodes As TNode()
            Get
                Return Me.ToArray()
            End Get
        End Property

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(Me)
        End Function

        Private Function GetEnumerator1() As IEnumerator(Of TNode) Implements IEnumerable(Of TNode).GetEnumerator
            If Me.Any Then
                Return Me.GetEnumerator
            End If

            Return SpecializedCollections.EmptyEnumerator(Of TNode)()
        End Function

        Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            If Me.Any Then
                Return Me.GetEnumerator
            End If
            Return SpecializedCollections.EmptyEnumerator(Of TNode)()
        End Function

        Friend Sub CopyTo(offset As Integer, array As SyntaxNode(), arrayOffset As Integer, count As Integer)
            Dim i As Integer
            For i = 0 To count - 1
                array((arrayOffset + i)) = Me.Item((i + offset))
            Next i
        End Sub

        Public Shared Operator =(left As SyntaxList(Of TNode), right As SyntaxList(Of TNode)) As Boolean
            Return (left._node Is right._node)
        End Operator

        Public Shared Operator <>(left As SyntaxList(Of TNode), right As SyntaxList(Of TNode)) As Boolean
            Return (Not left._node Is right._node)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is SyntaxList(Of TNode) AndAlso
                   Equals(DirectCast(obj, SyntaxList(Of TNode)))
        End Function

        Overloads Function Equals(obj As SyntaxList(Of TNode)) As Boolean Implements IEquatable(Of SyntaxList(Of TNode)).Equals
            Return Me._node Is obj._node
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If((Not Me._node Is Nothing), Me._node.GetHashCode, 0)
        End Function

        ''' <summary>
        ''' Returns the string representation of the nodes in this list, not including 
        ''' the first node's leading trivia and the last node's trailing trivia.
        ''' </summary>
        ''' <returns>
        ''' The string representation of the nodes in this list, not including 
        ''' the first node's leading trivia and the last node's trailing trivia.
        ''' </returns>
        Public Overrides Function ToString() As String
            Return If(Node IsNot Nothing, Node.ToString(), String.Empty)
        End Function

        ''' <summary>
        ''' Returns the full string representation of the nodes in this list including 
        ''' the first node's leading trivia and the last node's trailing trivia.
        ''' </summary>
        ''' <returns>
        ''' The full string representation of the nodes in this list including 
        ''' the first node's leading trivia and the last node's trailing trivia.
        ''' </returns>
        Public Function ToFullString() As String
            Return If(Node IsNot Nothing, Node.ToFullString(), String.Empty)
        End Function

        Friend Function AsSeparatedList(Of TOther As SyntaxNode)() As SeparatedSyntaxList(Of TOther)
            Return New SeparatedSyntaxList(Of TOther)(New SyntaxNodeOrTokenList(Me._node, 0))
        End Function

        Public Shared Widening Operator CType(node As TNode) As SyntaxList(Of TNode)
            Return New SyntaxList(Of TNode)(node)
        End Operator

        Public Shared Widening Operator CType(nodes As SyntaxList(Of VisualBasicSyntaxNode)) As SyntaxList(Of TNode)
            Return New SyntaxList(Of TNode)(nodes._node)
        End Operator

        Public Shared Widening Operator CType(nodes As SyntaxList(Of TNode)) As SyntaxList(Of VisualBasicSyntaxNode)
            Return New SyntaxList(Of VisualBasicSyntaxNode)(nodes._node)
        End Operator

    End Structure

End Namespace