' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class SyntaxListBuilder
        Private _count As Integer
        Private _nodes As ArrayElement(Of VisualBasicSyntaxNode)()

        Public Shared Function Create() As SyntaxListBuilder
            Return New SyntaxListBuilder(8)
        End Function

        Public Sub New(size As Integer)
            Me._nodes = New ArrayElement(Of VisualBasicSyntaxNode)(size - 1) {}
        End Sub

        Public Function Add(item As VisualBasicSyntaxNode) As SyntaxListBuilder
            EnsureAdditionalCapacity(1)
            Return Me.AddUnsafe(item)
        End Function

        Private Function AddUnsafe(item As GreenNode) As SyntaxListBuilder
            Debug.Assert(item IsNot Nothing)
            Me._nodes(Me._count).Value = DirectCast(item, VisualBasicSyntaxNode)
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
                If (Me._nodes(i).Value.Kind = kind) Then
                    Return True
                End If
            Next i
            Return False
        End Function

        Friend Sub RemoveLast()
            Me._count -= 1
            Me._nodes(Me._count) = Nothing
        End Sub

        Public Sub Clear()
            Me._count = 0
        End Sub

        Private Sub EnsureAdditionalCapacity(additionalCount As Integer)
            Dim currentSize As Integer = Me._nodes.Length
            Dim requiredSize As Integer = Me._count + additionalCount

            If requiredSize <= currentSize Then
                Return
            End If

            Dim newSize As Integer =
                If(requiredSize < 8, 8,
                If(requiredSize >= Integer.MaxValue / 2, Integer.MaxValue,
                Math.Max(requiredSize, currentSize * 2))) ' Guaranteed to at least double
            Debug.Assert(newSize >= requiredSize)

            Array.Resize(Me._nodes, newSize)
        End Sub

        Friend Function ToArray() As ArrayElement(Of VisualBasicSyntaxNode)()
            Dim dst As ArrayElement(Of VisualBasicSyntaxNode)() = New ArrayElement(Of VisualBasicSyntaxNode)(Me._count - 1) {}

            'TODO: workaround for range check hoisting bug
            ' <<< FOR LOOP
            Dim i As Integer = 0
            GoTo enter
            Do
                dst(i) = Me._nodes(i)
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
                    Return Me._nodes(0)
                Case 2
                    Return SyntaxList.List(Me._nodes(0), Me._nodes(1))
                Case 3
                    Return SyntaxList.List(Me._nodes(0), Me._nodes(1), Me._nodes(2))
            End Select
            Return SyntaxList.List(Me.ToArray)
        End Function

        <Conditional("DEBUG")>
        Private Sub Validate(start As Integer, [end] As Integer)
            Dim i As Integer
            For i = start To [end] - 1
                Debug.Assert(Me._nodes(i).Value IsNot Nothing)
            Next i
        End Sub

        Public ReadOnly Property Count As Integer
            Get
                Return Me._count
            End Get
        End Property

        Default Public Property Item(index As Integer) As VisualBasicSyntaxNode
            Get
                Return Me._nodes(index)
            End Get
            Set(value As VisualBasicSyntaxNode)
                Me._nodes(index).Value = value
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