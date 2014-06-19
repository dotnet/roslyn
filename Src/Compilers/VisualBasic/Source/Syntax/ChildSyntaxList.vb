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
    Partial Public Structure ChildSyntaxList
        Implements IReadOnlyCollection(Of SyntaxNodeOrToken), IEquatable(Of ChildSyntaxList)

        Friend ReadOnly _node As VisualBasicSyntaxNode
        Private _count As Integer

        Friend Sub New(node As VisualBasicSyntaxNode)
            If node IsNot Nothing Then
                Me._node = node
                Me._count = CountNodes(node.Green)
            End If
        End Sub

        Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of SyntaxNodeOrToken).Count
            Get
                Return Me._count
            End Get
        End Property

        Friend Shared Function CountNodes(green As InternalSyntax.VisualBasicSyntaxNode) As Integer
            Dim n As Integer = 0
            Dim cnt = green.SlotCount

            While cnt > 0
                cnt -= 1
                Dim child = green.GetSlot(cnt)
                If child IsNot Nothing Then
                    If Not child.IsList Then
                        n += 1
                    Else
                        n += child.SlotCount
                    End If
                End If
            End While

            Return n
        End Function

        ''' <summary>
        ''' How many actual nodes is represented by a node (note that some nodes are lists).
        ''' </summary>
        Private Shared Function Occupancy(gChild As InternalSyntax.VisualBasicSyntaxNode) As Integer
            Return If(gChild.IsList, gChild.SlotCount, 1)
        End Function

        Default Friend ReadOnly Property Item(index As Integer) As SyntaxNodeOrToken
            Get
                If index < 0 OrElse index >= Count Then
                    Throw New IndexOutOfRangeException
                End If
                Return ItemInternal(Me._node, index)
            End Get
        End Property

#If DEBUG Then
        Friend Shared Function ItemInternal(node As VisualBasicSyntaxNode, index As Integer, Optional fromTokenCtor As Boolean = False) As SyntaxNodeOrToken
#Else
        Friend Shared Function ItemInternal(node As SyntaxNode, index As Integer) As SyntaxNodeOrToken
#End If
            Dim green = node.Green
            Dim idx = index
            Dim slotIdx As Integer = 0
            Dim position = node.Position
            Dim gChild As InternalSyntax.VisualBasicSyntaxNode

            ' find a slot that contains the node or its parent list (if node is in a list)
            ' we will be skipping whole slots here so we will not loop for long
            ' the max possible number of slots is 12 (DeclareStatement)
            ' and typically much less than that
            '
            ' at the end of this we will have
            ' 1) slot index in slotIdx
            ' 2) if the slot is a list, our node index will be in idx
            ' 3) slot position at position (handy if need to create a token)
            Do
                Debug.Assert(slotIdx < green.SlotCount)

                gChild = green.GetSlot(slotIdx)
                If gChild IsNot Nothing Then
                    Dim curOccupancy = Occupancy(gChild)
                    If idx < curOccupancy Then
                        Exit Do
                    End If
                    idx -= curOccupancy
                    position += gChild.FullWidth
                End If

                slotIdx += 1
            Loop

            ' get node that represents this slot
            Dim red = node.GetNodeSlot(slotIdx)

            If Not gChild.IsList Then
                Debug.Assert(idx = 0)

                ' this is a single node or token
                ' if it is a node, we already have it
                ' otherwise will have to make a token with current gChild and position
                If red IsNot Nothing Then
                    Return red
                End If

            ElseIf red IsNot Nothing Then
                ' it is a list of nodes (separated or not), most common case

                Dim rChild = red.GetNodeSlot(idx)
                If rChild IsNot Nothing Then
                    ' this is our node
                    Return rChild
                Else
                    ' must be a separator
                    ' update gChild and position
                    gChild = gChild.GetSlot(idx)
                    position = red.GetChildPosition(idx)
                End If

            Else
                ' it is a token from a token list, very uncommon case
                ' update gChild and position
                position = position + gChild.GetChildOffset(idx)
                gChild = gChild.GetSlot(idx)
            End If

#If DEBUG Then
            Return New SyntaxNodeOrToken(node, DirectCast(gChild, InternalSyntax.SyntaxToken), position, index, fromTokenCtor:=fromTokenCtor)
#Else
            Return New SyntaxNodeOrToken(node, DirectCast(gChild, InternalSyntax.SyntaxToken), position, index)
#End If
        End Function

        Private ReadOnly Property Nodes As SyntaxNodeOrToken()
            Get
                Return Me.ToArray
            End Get
        End Property

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(Me)
        End Function

        Public Function Reverse() As Reversed
            Return New Reversed(Me)
        End Function

        Private Function GetEnumerator1() As IEnumerator(Of SyntaxNodeOrToken) Implements IEnumerable(Of SyntaxNodeOrToken).GetEnumerator
            If (Me._node Is Nothing) Then
                Return SpecializedCollections.EmptyEnumerator(Of SyntaxNodeOrToken)()
            End If
            Return New EnumeratorImpl(Me)
        End Function

        Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Me.GetEnumerator1
        End Function

        Public Overloads Function Equals(other As ChildSyntaxList) As Boolean Implements IEquatable(Of ChildSyntaxList).Equals
            Return Me._node Is other._node
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If(_node Is Nothing, 0, _node.GetHashCode)
        End Function

        Public Shared Operator =(left As ChildSyntaxList, right As ChildSyntaxList) As Boolean
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As ChildSyntaxList, right As ChildSyntaxList) As Boolean
            Return Not (left = right)
        End Operator
    End Structure
#End If
End Namespace

