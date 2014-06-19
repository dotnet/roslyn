Imports System.Collections
Imports System.Collections.Generic

Namespace Roslyn.Compilers.VisualBasic
    Public Structure SyntaxTriviaStruct
        Implements IEquatable(Of SyntaxTriviaStruct)

        Private ReadOnly _token As SyntaxTokenStruct
        Private ReadOnly _node As InternalSyntax.SyntaxNode
        Private ReadOnly _position As Integer

        Friend Sub New(ByVal token As SyntaxTokenStruct, ByVal node As InternalSyntax.SyntaxNode, ByVal position As Integer)
            Me._token = token
            Me._node = node
            Me._position = position
        End Sub

        Public ReadOnly Property Token As SyntaxTokenStruct
            Get
                Return Me._token
            End Get
        End Property

        Friend ReadOnly Property UnderlyingNode As InternalSyntax.SyntaxNode
            Get
                Return Me._node
            End Get
        End Property

        Friend ReadOnly Property Offset As Integer
            Get
                Return Me._position - Me._token.Position
            End Get
        End Property

        Friend ReadOnly Property Position As Integer
            Get
                Return Me._position
            End Get
        End Property

        Friend ReadOnly Property [End] As Integer
            Get
                Return Me._position + Me.FullWidth
            End Get
        End Property

        Public ReadOnly Property Kind As SyntaxKind
            Get
                If (Me._node Is Nothing) Then
                    Return SyntaxKind.None
                End If
                Return Me._node.Kind
            End Get
        End Property

        Public ReadOnly Property Parent As SyntaxTokenStruct
            Get
                Return Me._token
            End Get
        End Property

        Public ReadOnly Property FullSpan As TextSpan
            Get
                Return TextSpan.FromBounds(Position, [End])
            End Get
        End Property

        Public ReadOnly Property Span As TextSpan
            Get
                If Me._node IsNot Nothing Then
                    Return New TextSpan(Me._position + Me._node.GetLeadingTriviaWidth, Me._node.Width)
                End If
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property FullWidth As Integer
            Get
                If Me._node IsNot Nothing Then
                    Return Me._node.FullWidth
                End If
                Return 0
            End Get
        End Property

        Public ReadOnly Property Width As Integer
            Get
                If Me._node IsNot Nothing Then
                    Return Me._node.Width
                End If
                Return 0
            End Get
        End Property

        Public ReadOnly Property [Text] As String
            Get
                If (Me._node Is Nothing) Then
                    Return String.Empty
                End If
                Return Me._node.GetText
            End Get
        End Property

        Public Function GetFullText() As String
            If (Me._node Is Nothing) Then
                Return String.Empty
            End If
            Return Me._node.GetFullText
        End Function

        Public Function GetText() As String
            If (Me._node Is Nothing) Then
                Return String.Empty
            End If
            Return Me._node.GetText
        End Function

        Public Overrides Function ToString() As String
            Return Me.GetFullText
        End Function

        Public ReadOnly Property HasStructure As Boolean
            Get
                Return ((Not Me._node Is Nothing) AndAlso Me._node.IsStructuredTrivia)
            End Get
        End Property

        Public Function GetStructure() As SyntaxNode
            If (Me._node Is Nothing) Then
                Return Nothing
            End If
            Dim st = DirectCast(Me._node, InternalSyntax.StructuredTriviaSyntax)
            Return st.CreateRed(Me._token.Parent, Me._position)
        End Function

        Friend ReadOnly Property Errors As InternalSyntax.SyntaxDiagnosticInfoList
            Get
                Return New InternalSyntax.SyntaxDiagnosticInfoList(Me.UnderlyingNode)
            End Get
        End Property

        Public Shared Operator =(ByVal a As SyntaxTriviaStruct, ByVal b As SyntaxTriviaStruct) As Boolean
            Return a.Equals(b)
        End Operator

        Public Shared Operator <>(ByVal a As SyntaxTriviaStruct, ByVal b As SyntaxTriviaStruct) As Boolean
            Return Not a.Equals(b)
        End Operator

        Public Overloads Function Equals(ByVal other As SyntaxTriviaStruct) As Boolean Implements IEquatable(Of SyntaxTriviaStruct).Equals
            Return (((Me._token = other._token) AndAlso (Me._node Is other._node)) AndAlso (Me._position = other._position))
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return (TypeOf obj Is SyntaxTriviaStruct AndAlso Me.Equals(DirectCast(obj, SyntaxTriviaStruct)))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ((Me._token.GetHashCode + If((Not Me._node Is Nothing), Me._node.GetHashCode, 0)) + Me._position)
        End Function

        Public Function IsEquivalentTo(ByVal other As SyntaxTriviaStruct) As Boolean
            Return InternalSyntax.SyntaxNode.AreEquivalent(Me._node, other._node)
        End Function

        Public Shared Widening Operator CType(ByVal trivia As SyntaxTriviaStruct) As CommonSyntaxTrivia
            Return New CommonSyntaxTrivia(CType(trivia.Token, CommonSyntaxToken), trivia.UnderlyingNode, trivia.Offset)
        End Operator

        Public Shared Narrowing Operator CType(ByVal trivia As CommonSyntaxTrivia) As SyntaxTriviaStruct
            Return New SyntaxTriviaStruct(CType(trivia.Token, SyntaxTokenStruct), DirectCast(trivia.UnderlyingNode, InternalSyntax.SyntaxNode), trivia.FullSpan.Start)
        End Operator
    End Structure


    Public Structure SyntaxTriviaList
        Implements IEquatable(Of SyntaxTriviaList), IEnumerable(Of SyntaxTriviaStruct), IEnumerable

        Private ReadOnly _token As SyntaxTokenStruct
        Private ReadOnly _node As InternalSyntax.SyntaxNode
        Private ReadOnly _position As Integer

        Public Shared ReadOnly Empty As SyntaxTriviaList

        Friend Sub New(ByVal token As SyntaxTokenStruct, ByVal node As InternalSyntax.SyntaxNode, ByVal position As Integer)
            Me._token = token
            Me._node = node
            Me._position = position
        End Sub

        Friend ReadOnly Property Token As SyntaxTokenStruct
            Get
                Return Me._token
            End Get
        End Property

        Friend ReadOnly Property Node As InternalSyntax.SyntaxNode
            Get
                Return Me._node
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                If (Me._node Is Nothing) Then
                    Return 0
                End If
                If Not Me._node.IsList Then
                    Return 1
                End If
                Return Me._node.ChildCount
            End Get
        End Property

        Default Public ReadOnly Property Item(ByVal index As Integer) As SyntaxTriviaStruct
            Get
                If (Me._node Is Nothing) Then
                    Return Nothing
                End If
                If Me._node.IsList Then
                    If ((index < 0) OrElse (index > Me._node.ChildCount)) Then
                        Throw New IndexOutOfRangeException
                    End If
                    Return New SyntaxTriviaStruct(Me._token, Me._node.GetChild(index), Me._node.GetChildOffset(index) + Me._position)
                End If
                If (index <> 0) Then
                    Throw New IndexOutOfRangeException
                End If
                Return New SyntaxTriviaStruct(Me._token, Me._node, Me._position)
            End Get
        End Property

        Public ReadOnly Property HasDiagnostics As Boolean
            Get
                If (Me._node Is Nothing) Then
                    Return False
                End If
                Return Me._node.HasDiagnostics
            End Get
        End Property

        Public Function Any() As Boolean
            Return (Me.Count > 0)
        End Function

        Public Function Any(ByVal kind As SyntaxKind) As Boolean
            For Each element In Me
                If (element.Kind = kind) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(Me)
        End Function

        Private ReadOnly Property Nodes As SyntaxTriviaStruct()
            Get
                Return Me.ToArray()
            End Get
        End Property

        Private Function GetEnumerator1() As IEnumerator(Of SyntaxTriviaStruct) Implements IEnumerable(Of SyntaxTriviaStruct).GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of SyntaxTriviaStruct)()
            End If
            Return Me.GetEnumerator
        End Function

        Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of InternalSyntax.SyntaxTrivia)()
            End If
            Return Me.GetEnumerator
        End Function

        Public Sub CopyTo(ByVal sourceOffset As Integer, ByVal array As SyntaxTriviaStruct(), ByVal arrayOffset As Integer, ByVal count As Integer)
            Dim i As Integer
            For i = 0 To count - 1
                array(arrayOffset + i) = Me(i + sourceOffset)
            Next i
        End Sub

        Public Overloads Function Equals(ByVal other As SyntaxTriviaList) As Boolean Implements IEquatable(Of SyntaxTriviaList).Equals
            Return ((Me._token = other._token) AndAlso (Me._node Is other._node))
        End Function

        Public Shared Operator =(ByVal left As SyntaxTriviaList, ByVal right As SyntaxTriviaList) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(ByVal left As SyntaxTriviaList, ByVal right As SyntaxTriviaList) As Boolean
            Return Not left.Equals(right)
        End Operator

        Public Shared Widening Operator CType(ByVal trivia As SyntaxTriviaStruct) As SyntaxTriviaList
            Return New SyntaxTriviaList(trivia.Token, trivia.UnderlyingNode, trivia.Position)
        End Operator

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return (TypeOf obj Is SyntaxTriviaList AndAlso Me.Equals(DirectCast(obj, SyntaxTriviaList)))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return (Me._token.GetHashCode + If((Not Me._node Is Nothing), Me._node.GetHashCode, 0))
        End Function

        Public Shared Operator &(ByVal list1 As SyntaxTriviaList, ByVal list2 As SyntaxTriviaList) As SyntaxTriviaList
            Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.Concat(list1._node, list2._node), 0)
        End Operator

        Public Structure Enumerator
            Implements IEnumerator(Of SyntaxTriviaStruct), IDisposable, IEnumerator

            Private list As SyntaxTriviaList
            Private index As Integer

            Friend Sub New(ByVal list As SyntaxTriviaList)
                Me.list = list
                Me.index = -1
            End Sub

            Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                Me.index += 1
                Return (Me.index < Me.list.Count)
            End Function

            Public ReadOnly Property Current As SyntaxTriviaStruct Implements IEnumerator(Of SyntaxTriviaStruct).Current
                Get
                    Return Me.list.Item(Me.index)
                End Get
            End Property

            Private Sub Dispose() Implements IDisposable.Dispose
            End Sub

            Private Sub Reset() Implements IEnumerator.Reset
                Throw New NotSupportedException
            End Sub

            Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return Me.Current
                End Get
            End Property
        End Structure
    End Structure

    Public Class SyntaxTriviaListBuilder
        Private _count As Integer
        Private _nodes As SyntaxTriviaStruct()

        Public ReadOnly Property Count As Integer
            Get
                Return Me._count
            End Get
        End Property

        Public Sub New(ByVal size As Integer)
            Me._nodes = New SyntaxTriviaStruct(size - 1) {}
        End Sub

        Public Sub Add(ByVal list As SyntaxTriviaList)
            Me.Add(list, 0, list.Count)
        End Sub

        Public Sub Add(ByVal items As SyntaxTriviaStruct())
            Me.Add(items, 0, items.Length)
        End Sub

        Public Function Add(ByVal item As SyntaxTriviaStruct) As SyntaxTriviaListBuilder
            If ((Me._nodes Is Nothing) OrElse (Me._count >= Me._nodes.Length)) Then
                Me.Grow(If((Me._count = 0), 8, (Me._nodes.Length * 2)))
            End If
            Me._nodes(Me._count) = item
            Me._count += 1
            Return Me
        End Function

        Public Sub Add(ByVal items As SyntaxTriviaStruct(), ByVal sourceOffset As Integer, ByVal length As Integer)
            If ((Me._nodes Is Nothing) OrElse ((Me._count + length) > Me._nodes.Length)) Then
                Me.Grow((Me._count + length))
            End If
            Array.Copy(items, sourceOffset, Me._nodes, Me._count, length)
            Me._count = (Me._count + length)
        End Sub

        Public Sub Add(ByVal list As SyntaxTriviaList, ByVal sourceOffset As Integer, ByVal length As Integer)
            If ((Me._nodes Is Nothing) OrElse ((Me._count + length) > Me._nodes.Length)) Then
                Me.Grow((Me._count + length))
            End If
            list.CopyTo(sourceOffset, Me._nodes, Me._count, length)
            Me._count = (Me._count + length)
        End Sub

        Public Sub Clear()
            Me._count = 0
        End Sub

        Public Shared Function Create() As SyntaxTriviaListBuilder
            Return New SyntaxTriviaListBuilder(4)
        End Function

        Private Sub Grow(ByVal size As Integer)
            Dim tmp As SyntaxTriviaStruct() = New SyntaxTriviaStruct(size - 1) {}
            Array.Copy(Me._nodes, tmp, Me._nodes.Length)
            Me._nodes = tmp
        End Sub

        Public Shared Widening Operator CType(ByVal builder As SyntaxTriviaListBuilder) As SyntaxTriviaList
            Return builder.ToList
        End Operator

        Public Function ToList() As SyntaxTriviaList
            If (Me._count <= 0) Then
                Return New SyntaxTriviaList
            End If
            Select Case Me._count
                Case 1
                    Return New SyntaxTriviaList(Nothing, Me._nodes(0).UnderlyingNode, 0)
                Case 2
                    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(Me._nodes(0).UnderlyingNode, Me._nodes(1).UnderlyingNode), 0)
                Case 3
                    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(Me._nodes(0).UnderlyingNode, Me._nodes(1).UnderlyingNode, Me._nodes(2).UnderlyingNode), 0)
            End Select
            Dim tmp = New InternalSyntax.SyntaxNode(Me._count - 1) {}
            Dim i As Integer
            For i = 0 To Me._count - 1
                tmp(i) = Me._nodes(i).UnderlyingNode
            Next i
            Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(tmp), 0)
        End Function

    End Class
End Namespace

