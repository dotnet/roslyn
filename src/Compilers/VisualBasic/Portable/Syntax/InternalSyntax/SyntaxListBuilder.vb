' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class SyntaxListBuilder
        Inherits AbstractSyntaxListBuilder

        Private _count As Integer
        Private Nodes As ArrayElement(Of VisualBasicSyntaxNode)()

        Public Shared Function Create() As SyntaxListBuilder
            Return New SyntaxListBuilder(8)
        End Function

        Public Sub New(size As Integer)
            Me.Nodes = New ArrayElement(Of VisualBasicSyntaxNode)(size - 1) {}
        End Sub

        Public Function Add(item As VisualBasicSyntaxNode) As SyntaxListBuilder
            EnsureAdditionalCapacity(1)
            Return Me.AddUnsafe(item)
        End Function

        Private Function AddUnsafe(item As GreenNode) As SyntaxListBuilder
            Debug.Assert(item IsNot Nothing)
            Me.Nodes(Me._count).Value = DirectCast(item, VisualBasicSyntaxNode)
            Me._count += 1
            Return Me
        End Function

        Public Function AddRange(Of TNode As VisualBasicSyntaxNode)(list As SyntaxList(Of TNode)) As SyntaxListBuilder
            Return Me.AddRange(Of TNode)(list, 0, list.Count)
        End Function

        Public Function AddRange(Of TNode As VisualBasicSyntaxNode)(list As SyntaxList(Of TNode), offset As Integer, length As Integer) As SyntaxListBuilder
            EnsureAdditionalCapacity(length - offset)

            Dim oldCount = Me._count

            For i = offset To offset + length - 1
                AddUnsafe(list.ItemUntyped(i))
            Next i

            Me.Validate(oldCount, Me._count)
            Return Me
        End Function

        Public Function Any(kind As SyntaxKind) As Boolean
            Dim i As Integer
            For i = 0 To Me._count - 1
                If (Me.Nodes(i).Value.Kind = kind) Then
                    Return True
                End If
            Next i
            Return False
        End Function

        Friend Sub RemoveLast()
            Me._count -= 1
            Me.Nodes(Me._count) = Nothing
        End Sub

        Public Sub Clear()
            Me._count = 0
        End Sub

        Private Sub EnsureAdditionalCapacity(additionalCount As Integer)
            Dim currentSize As Integer = Me.Nodes.Length
            Dim requiredSize As Integer = Me._count + additionalCount

            If requiredSize <= currentSize Then
                Return
            End If

            Dim newSize As Integer =
                If(requiredSize < 8, 8,
                If(requiredSize >= Integer.MaxValue / 2, Integer.MaxValue,
                Math.Max(requiredSize, currentSize * 2))) ' Guaranteed to at least double
            Debug.Assert(newSize >= requiredSize)

            Array.Resize(Me.Nodes, newSize)
        End Sub

        Friend Function ToArray() As ArrayElement(Of VisualBasicSyntaxNode)()
            Dim dst As ArrayElement(Of VisualBasicSyntaxNode)() = New ArrayElement(Of VisualBasicSyntaxNode)(Me._count - 1) {}

            'TODO: workaround for range check hoisting bug
            ' <<< FOR LOOP
            Dim i As Integer = 0
            GoTo enter
            Do
                dst(i) = Me.Nodes(i)
                i += 1
enter:
            Loop While i < dst.Length
            ' >>> FOR LOOP

            Return dst
        End Function

        Friend Function ToListNode() As VisualBasicSyntaxNode
            Select Case Me._count
                Case 0
                    Return Nothing
                Case 1
                    Return Me.Nodes(0)
                Case 2
                    Return SyntaxList.List(Me.Nodes(0), Me.Nodes(1))
                Case 3
                    Return SyntaxList.List(Me.Nodes(0), Me.Nodes(1), Me.Nodes(2))
            End Select
            Return SyntaxList.List(Me.ToArray)
        End Function

        <Conditional("DEBUG")>
        Private Sub Validate(start As Integer, [end] As Integer)
            Dim i As Integer
            For i = start To [end] - 1
                Debug.Assert(Me.Nodes(i).Value IsNot Nothing)
            Next i
        End Sub

        Public ReadOnly Property Count As Integer
            Get
                Return Me._count
            End Get
        End Property

        Default Public Property Item(index As Integer) As VisualBasicSyntaxNode
            Get
                Return Me.Nodes(index)
            End Get
            Set(value As VisualBasicSyntaxNode)
                Me.Nodes(index).Value = value
            End Set
        End Property

        Public Function ToList() As SyntaxList(Of VisualBasicSyntaxNode)
            Return New SyntaxList(Of VisualBasicSyntaxNode)(ToListNode)
        End Function

        Public Function ToList(Of TDerived As VisualBasicSyntaxNode)() As SyntaxList(Of TDerived)
            Return New SyntaxList(Of TDerived)(ToListNode)
        End Function
    End Class
End Namespace