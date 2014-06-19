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
        Implements IEquatable(Of SyntaxTriviaList), IReadOnlyCollection(Of SyntaxTrivia), IEnumerable

        Private ReadOnly _token As SyntaxToken
        Private ReadOnly _node As InternalSyntax.VisualBasicSyntaxNode
        Private ReadOnly _position As Integer
        Private ReadOnly _index As Integer

        Public Shared ReadOnly Empty As SyntaxTriviaList

        Friend Sub New(trivia As SyntaxTrivia)
            Me._token = trivia.Token
            Me._node = trivia.UnderlyingNode
            Me._position = trivia.Position
            Me._index = trivia.Index
        End Sub

        Friend Sub New(token As SyntaxToken, node As InternalSyntax.VisualBasicSyntaxNode, position As Integer, index As Integer)
            Me._token = token
            Me._node = node
            Me._position = position
            Me._index = index
        End Sub

        Friend ReadOnly Property Token As SyntaxToken
            Get
                Return Me._token
            End Get
        End Property

        Friend ReadOnly Property Node As InternalSyntax.VisualBasicSyntaxNode
            Get
                Return Me._node
            End Get
        End Property

        Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of SyntaxTrivia).Count
            Get
                If (Me._node Is Nothing) Then
                    Return 0
                End If
                If Not Me._node.IsList Then
                    Return 1
                End If
                Return Me._node.SlotCount
            End Get
        End Property

        Public Function ElementAt(index As Integer) As SyntaxTrivia
            Return Me(index)
        End Function

        Default Friend ReadOnly Property Item(index As Integer) As SyntaxTrivia
            Get
                If (Me._node Is Nothing) Then
                    Return Nothing
                End If
                If Me._node.IsList Then
                    If ((index < 0) OrElse (index > Me._node.SlotCount)) Then
                        Throw New IndexOutOfRangeException
                    End If
                    Return New SyntaxTrivia(Me._token, Me._node.GetSlot(index), Me._node.GetChildOffset(index) + Me._position, Me._index + index)
                End If
                If (index <> 0) Then
                    Throw New IndexOutOfRangeException
                End If
                Return New SyntaxTrivia(Me._token, Me._node, Me._position, Me._index)
            End Get
        End Property

        Public Function IndexOf(trivia As SyntaxTrivia) As Integer
            Dim index = 0
            For Each child In Me
                If Object.Equals(child, trivia) Then
                    Return index
                End If

                index = index + 1
            Next

            Return -1
        End Function

        ''' <summary>
        ''' Returns the string representation of the trivia in this list, not including 
        ''' the first trivia's leading sub-trivia or the last trivia's trailing sub-trivia
        ''' if they are structured.
        ''' </summary>
        ''' <returns>
        ''' The string representation of the trivia in this list, not including 
        ''' the first trivia's leading sub-trivia or the last trivia's trailing sub-trivia
        ''' if they are structured.
        ''' </returns>
        Public Overrides Function ToString() As String
            Return If(Me._node IsNot Nothing, Me._node.ToString(), String.Empty)
        End Function

        ''' <summary>
        ''' Returns the full string representation of the trivia in this list including 
        ''' the first trivia's leading sub-trivia and the last trivia's trailing sub-trivia
        ''' even if they are structured.
        ''' </summary>
        ''' <returns>
        ''' The full string representation of the trivia in this list including 
        ''' the first trivia's leading sub-trivia and the last trivia's trailing sub-trivia
        ''' even if they are structured.
        ''' </returns>
        Public Function ToFullString() As String
            Return If(Me._node IsNot Nothing, Me._node.ToFullString(), String.Empty)
        End Function

        Public Function First() As SyntaxTrivia
            Return Me(0)
        End Function

        Public Function NormalizeWhitespace(Optional indentation As String = SyntaxExtensions.DefaultIndentation, Optional elasticTrivia As Boolean = False, Optional useDefaultCasing As Boolean = False) As SyntaxTriviaList
            Return SyntaxFormatter.Format(Me, indentation, elasticTrivia, useDefaultCasing)
        End Function

        Public Function FirstOrDefault() As SyntaxTrivia
            If Me.Count > 0 Then
                Return Me(0)
            Else
                Return Nothing
            End If
        End Function

        Public Function Last() As SyntaxTrivia
            Return Me(Me.Count - 1)
        End Function

        Public Function LastOrDefault() As SyntaxTrivia
            If Me.Count > 0 Then
                Return Me(Me.Count - 1)
            Else
                Return Nothing
            End If
        End Function

        Public Function Any() As Boolean
            Return (Me.Count > 0)
        End Function

        Public Function Any(kind As SyntaxKind) As Boolean
            For Each element In Me
                If (element.Kind = kind) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Function Reverse() As Reversed
            Return New Reversed(Me)
        End Function

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(Me)
        End Function

        Private ReadOnly Property Nodes As SyntaxTrivia()
            Get
                Return Me.ToArray()
            End Get
        End Property

        Private Function GetEnumerator1() As IEnumerator(Of SyntaxTrivia) Implements IEnumerable(Of SyntaxTrivia).GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of SyntaxTrivia)()
            End If
            Return New EnumeratorImpl(Me)
        End Function

        Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of SyntaxTrivia)()
            End If
            Return New EnumeratorImpl(Me)
        End Function

        Friend Sub CopyTo(offset As Integer, array As SyntaxTrivia(), arrayOffset As Integer, count As Integer)
            Debug.Assert(offset >= 0)
            Debug.Assert(count >= 0)
            Debug.Assert(Me.Count >= offset + count)

            If count = 0 Then
                Return
            End If

            ' get first one without creating any red node
            Dim first = Item(offset)
            array(arrayOffset) = first

            ' calculate token position from the first ourselves from now on
            Dim position = first.Position
            Dim index = first.Index
            Dim current = first

            For i = 1 To count - 1 Step 1
                position += current.FullWidth
                index += 1
                current = New SyntaxTrivia(Me._token, GetGreenNodeAt(offset + i), position, index)

                array(arrayOffset + i) = current
            Next
        End Sub

        Private Function GetGreenNodeAt(i As Integer) As InternalSyntax.VisualBasicSyntaxNode
            Return GetGreenNodeAt(Me._node, i)
        End Function

        ''' <summary>
        ''' get the green node at the given slot
        ''' </summary>
        Private Shared Function GetGreenNodeAt(node As InternalSyntax.VisualBasicSyntaxNode, i As Integer) As InternalSyntax.VisualBasicSyntaxNode
            Debug.Assert(node.IsList OrElse (i = 0 AndAlso Not node.IsList))
            Return If(node.IsList, node.GetSlot(i), node)
        End Function

        Public Overloads Function Equals(other As SyntaxTriviaList) As Boolean Implements IEquatable(Of SyntaxTriviaList).Equals
            Return Me._token = other._token AndAlso
                   Me._node Is other._node AndAlso
                   Me._index = other._index
        End Function

        Public Shared Operator =(left As SyntaxTriviaList, right As SyntaxTriviaList) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As SyntaxTriviaList, right As SyntaxTriviaList) As Boolean
            Return Not left.Equals(right)
        End Operator

        Public Shared Widening Operator CType(trivia As SyntaxTrivia) As SyntaxTriviaList
            Return New SyntaxTriviaList(trivia.Token, trivia.UnderlyingNode, trivia.Position, trivia.Index)
        End Operator

        Public Shared Widening Operator CType(triviaList As SyntaxTriviaList) As SyntaxTriviaList
            Return New SyntaxTriviaList(triviaList._token, triviaList._node, triviaList._position, triviaList._index)
        End Operator

        Public Shared Narrowing Operator CType(commonTriviaList As SyntaxTriviaList) As SyntaxTriviaList
            Return New SyntaxTriviaList(CType(commonTriviaList.Token, SyntaxToken), CType(commonTriviaList.Node, InternalSyntax.VisualBasicSyntaxNode), commonTriviaList.Position, commonTriviaList.Index)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return (TypeOf obj Is SyntaxTriviaList AndAlso Me.Equals(DirectCast(obj, SyntaxTriviaList)))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return (Me._token.GetHashCode + If((Not Me._node Is Nothing), Me._node.GetHashCode, 0))
        End Function

        'EDMAURER using this method will likely produce different results than using Enumerable.Concat()
        'Using Enumerable.Concat() will produce a sequence containing items whose parent, span, index, and node are
        'relative to the list from which they were pulled. A concatenation of two lists from different parents may 
        'or may not make sense depending on the usage. A concatenation using this method will normalize all of the
        'spans, etc. of the elements of the resulting sequence to be relative to the parameters used in the call
        'to the SyntaxTriviaList constructor. Having these two Concat mechanisms that produce different results
        'is too confusing. We could hijack Enumerable.Concat(this SyntaxTriviaList), but that seems like a dark path.
        'Public Function Concat(tail As SyntaxTriviaList) As SyntaxTriviaList
        '    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.Concat(Me._node, tail._node), Me._position, 0)
        'End Function
    End Structure
#End If
End Namespace