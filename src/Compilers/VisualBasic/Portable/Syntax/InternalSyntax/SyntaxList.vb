' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend MustInherit Class SyntaxList
        Inherits VisualBasicSyntaxNode

        Protected Sub New(errors As DiagnosticInfo(), annotations As SyntaxAnnotation())
            MyBase.New(SyntaxKind.List, errors, annotations)
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
        End Sub

        Protected Sub New()
            MyBase.New(SyntaxKind.List)
        End Sub

        Friend Shared Function List(child As VisualBasicSyntaxNode) As VisualBasicSyntaxNode
            Return child
        End Function

        Friend Shared Function List(child0 As VisualBasicSyntaxNode, child1 As VisualBasicSyntaxNode) As WithTwoChildren

            Dim hash As Integer
            Dim cached As GreenNode = SyntaxNodeCache.TryGetNode(SyntaxKind.List, child0, child1, hash)
            If cached IsNot Nothing Then
                Return DirectCast(cached, WithTwoChildren)
            End If

            Dim result = New WithTwoChildren(child0, child1)
            If hash >= 0 Then
                SyntaxNodeCache.AddNode(result, hash)
            End If

            Return result
        End Function

        Friend Shared Function List(child0 As VisualBasicSyntaxNode, child1 As VisualBasicSyntaxNode, child2 As VisualBasicSyntaxNode) As WithThreeChildren
            Dim hash As Integer
            Dim cached As GreenNode = SyntaxNodeCache.TryGetNode(SyntaxKind.List, child0, child1, child2, hash)
            If cached IsNot Nothing Then
                Return DirectCast(cached, WithThreeChildren)
            End If

            Dim result = New WithThreeChildren(child0, child1, child2)
            If hash >= 0 Then
                SyntaxNodeCache.AddNode(result, hash)
            End If

            Return result
        End Function

        Friend Shared Function List(nodes As ArrayElement(Of VisualBasicSyntaxNode)()) As SyntaxList
            ' "WithLotsOfChildren" list will allocate a separate array to hold
            ' precomputed node offsets. It may not be worth it for smallish lists.
            If nodes.Length < 10 Then
                Return New WithManyChildren(nodes)
            Else
                Return New WithLotsOfChildren(nodes)
            End If
        End Function

        Friend Shared Function List(nodes As VisualBasicSyntaxNode()) As SyntaxList
            Return List(nodes, nodes.Length)
        End Function

        Friend Shared Function List(nodes As VisualBasicSyntaxNode(), count As Integer) As SyntaxList
            Dim array = New ArrayElement(Of VisualBasicSyntaxNode)(count - 1) {}
            Debug.Assert(array.Length = count)
            For i = 0 To count - 1
                array(i).Value = nodes(i)
                Debug.Assert(array(i).Value IsNot Nothing)
            Next
            Return List(array)
        End Function

        Friend MustOverride Sub CopyTo(array As ArrayElement(Of VisualBasicSyntaxNode)(), offset As Integer)

        Friend Shared Function Concat(left As VisualBasicSyntaxNode, right As VisualBasicSyntaxNode) As VisualBasicSyntaxNode

            If (left Is Nothing) Then
                Return right
            End If
            If (right Is Nothing) Then
                Return left
            End If

            Dim tmp As ArrayElement(Of VisualBasicSyntaxNode)()
            Dim leftList As SyntaxList = TryCast(left, SyntaxList)
            Dim rightList As SyntaxList = TryCast(right, SyntaxList)

            If leftList IsNot Nothing Then
                If rightList IsNot Nothing Then
                    tmp = New ArrayElement(Of VisualBasicSyntaxNode)(left.SlotCount + right.SlotCount - 1) {}
                    leftList.CopyTo(tmp, 0)
                    rightList.CopyTo(tmp, left.SlotCount)
                    Return SyntaxList.List(tmp)
                End If
                tmp = New ArrayElement(Of VisualBasicSyntaxNode)((left.SlotCount + 1) - 1) {}
                leftList.CopyTo(tmp, 0)
                tmp(left.SlotCount).Value = right
                Return SyntaxList.List(tmp)
            End If

            If rightList IsNot Nothing Then
                tmp = New ArrayElement(Of VisualBasicSyntaxNode)((rightList.SlotCount + 1) - 1) {}
                tmp(0).Value = left
                rightList.CopyTo(tmp, 1)
                Return SyntaxList.List(tmp)
            End If

            Return SyntaxList.List(left, right)
        End Function

        Friend NotInheritable Class WithTwoChildren
            Inherits SyntaxList

            Private ReadOnly _child0 As VisualBasicSyntaxNode
            Private ReadOnly _child1 As VisualBasicSyntaxNode

            Private Sub New(errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), child0 As VisualBasicSyntaxNode, child1 As VisualBasicSyntaxNode)
                MyBase.New(errors, annotations)

                MyBase._slotCount = 2

                MyBase.AdjustFlagsAndWidth(child0)
                Me._child0 = child0

                MyBase.AdjustFlagsAndWidth(child1)
                Me._child1 = child1
            End Sub

            Friend Sub New(child0 As VisualBasicSyntaxNode, child1 As VisualBasicSyntaxNode)
                MyBase.New()

                MyBase._slotCount = 2

                MyBase.AdjustFlagsAndWidth(child0)
                Me._child0 = child0

                MyBase.AdjustFlagsAndWidth(child1)
                Me._child1 = child1
            End Sub

            Friend Sub New(reader As ObjectReader)
                MyBase.New(reader)

                MyBase._slotCount = 2

                Me._child0 = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                MyBase.AdjustFlagsAndWidth(_child0)
                Me._child1 = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                MyBase.AdjustFlagsAndWidth(_child1)
            End Sub

            Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
                Return Function(r) New WithTwoChildren(r)
            End Function

            Friend Overrides Sub WriteTo(writer As ObjectWriter)
                MyBase.WriteTo(writer)
                writer.WriteValue(Me._child0)
                writer.WriteValue(Me._child1)
            End Sub

            Friend Overrides Sub CopyTo(array As ArrayElement(Of VisualBasicSyntaxNode)(), offset As Integer)
                array(offset).Value = Me._child0
                array((offset + 1)).Value = Me._child1
            End Sub

            Friend Overrides Function GetSlot(index As Integer) As GreenNode
                Select Case index
                    Case 0
                        Return Me._child0
                    Case 1
                        Return Me._child1
                End Select
                Return Nothing
            End Function

            Friend Overrides Function CreateRed(parent As SyntaxNode, startLocation As Integer) As SyntaxNode
                Return New VisualBasic.Syntax.SyntaxList.WithTwoChildren(Me, parent, startLocation)
            End Function

            Friend Overrides Function SetDiagnostics(errors() As DiagnosticInfo) As GreenNode
                Return New WithTwoChildren(errors, Me.GetAnnotations(), Me._child0, Me._child1)
            End Function

            Friend Overrides Function SetAnnotations(annotations() As SyntaxAnnotation) As GreenNode
                Return New WithTwoChildren(Me.GetDiagnostics(), annotations, Me._child0, Me._child1)
            End Function
        End Class

        Friend NotInheritable Class WithThreeChildren
            Inherits SyntaxList

            Private ReadOnly _child0 As VisualBasicSyntaxNode
            Private ReadOnly _child1 As VisualBasicSyntaxNode
            Private ReadOnly _child2 As VisualBasicSyntaxNode

            Private Sub New(errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), child0 As VisualBasicSyntaxNode, child1 As VisualBasicSyntaxNode, child2 As VisualBasicSyntaxNode)
                MyBase.New(errors, annotations)

                MyBase._slotCount = 3

                MyBase.AdjustFlagsAndWidth(child0)
                Me._child0 = child0

                MyBase.AdjustFlagsAndWidth(child1)
                Me._child1 = child1

                MyBase.AdjustFlagsAndWidth(child2)
                Me._child2 = child2
            End Sub

            Friend Sub New(child0 As VisualBasicSyntaxNode, child1 As VisualBasicSyntaxNode, child2 As VisualBasicSyntaxNode)
                MyBase.New()

                MyBase._slotCount = 3

                MyBase.AdjustFlagsAndWidth(child0)
                Me._child0 = child0

                MyBase.AdjustFlagsAndWidth(child1)
                Me._child1 = child1

                MyBase.AdjustFlagsAndWidth(child2)
                Me._child2 = child2
            End Sub

            Friend Sub New(reader As ObjectReader)
                MyBase.New(reader)
                MyBase._slotCount = 3

                Me._child0 = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                MyBase.AdjustFlagsAndWidth(_child0)
                Me._child1 = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                MyBase.AdjustFlagsAndWidth(_child1)
                Me._child2 = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                MyBase.AdjustFlagsAndWidth(_child2)
            End Sub

            Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
                Return Function(r) New WithThreeChildren(r)
            End Function

            Friend Overrides Sub WriteTo(writer As ObjectWriter)
                MyBase.WriteTo(writer)
                writer.WriteValue(Me._child0)
                writer.WriteValue(Me._child1)
                writer.WriteValue(Me._child2)
            End Sub

            Friend Overrides Sub CopyTo(array As ArrayElement(Of VisualBasicSyntaxNode)(), offset As Integer)
                array(offset).Value = Me._child0
                array(offset + 1).Value = Me._child1
                array(offset + 2).Value = Me._child2
            End Sub

            Friend Overrides Function GetSlot(index As Integer) As GreenNode
                Select Case index
                    Case 0
                        Return Me._child0
                    Case 1
                        Return Me._child1
                    Case 2
                        Return Me._child2
                End Select
                Return Nothing
            End Function

            Friend Overrides Function CreateRed(parent As SyntaxNode, startLocation As Integer) As SyntaxNode
                Return New VisualBasic.Syntax.SyntaxList.WithThreeChildren(Me, parent, startLocation)
            End Function

            Friend Overrides Function SetDiagnostics(errors() As DiagnosticInfo) As GreenNode
                Return New WithThreeChildren(errors, Me.GetAnnotations(), Me._child0, Me._child1, Me._child2)
            End Function

            Friend Overrides Function SetAnnotations(annotations() As SyntaxAnnotation) As GreenNode
                Return New WithThreeChildren(Me.GetDiagnostics(), annotations, Me._child0, Me._child1, Me._child2)
            End Function
        End Class

        Friend MustInherit Class WithManyChildrenBase
            Inherits SyntaxList

            Protected ReadOnly _children As ArrayElement(Of VisualBasicSyntaxNode)()

            Protected Sub New(errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), children As ArrayElement(Of VisualBasicSyntaxNode)())
                MyBase.New(errors, annotations)
                Me._children = children
                InitChildren()
            End Sub

            Friend Sub New(children As ArrayElement(Of VisualBasicSyntaxNode)())
                MyBase.New()
                Me._children = children
                InitChildren()
            End Sub

            Private Sub InitChildren()
                Dim n = _children.Length
                If (n < Byte.MaxValue) Then
                    Me._slotCount = CByte(n)
                Else
                    Me._slotCount = Byte.MaxValue
                End If

                For i = 0 To _children.Length - 1
                    Dim child = _children(i)
                    MyBase.AdjustFlagsAndWidth(child)
                Next i
            End Sub

            Protected Overrides Function GetSlotCount() As Integer
                Return Me._children.Length
            End Function

            Protected Sub New(reader As ObjectReader)
                MyBase.New(reader)

                Dim length = reader.ReadInt32()

                Me._children = New ArrayElement(Of VisualBasicSyntaxNode)(length - 1) {}
                For i = 0 To length - 1
                    Me._children(i).Value = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                Next

                InitChildren()
            End Sub

            Friend Overrides Sub WriteTo(writer As ObjectWriter)
                MyBase.WriteTo(writer)

                ' PERF Write the array out manually.Profiling shows that this Is cheaper than converting to 
                ' an array in order to use writer.WriteValue.
                writer.WriteInt32(Me._children.Length)

                For i = 0 To Me._children.Length - 1
                    writer.WriteValue(Me._children(i).Value)
                Next
            End Sub

            Friend Overrides Sub CopyTo(nodes As ArrayElement(Of VisualBasicSyntaxNode)(), offset As Integer)
                Array.Copy(Me._children, 0, nodes, offset, Me._children.Length)
            End Sub

            Friend Overrides Function GetSlot(index As Integer) As GreenNode
                Return Me._children(index)
            End Function


            'TODO: weakening heuristic may need some tuning.
            Private Shared Function ShouldMakeWeakList(parent As SyntaxNode) As Boolean
                Return parent IsNot Nothing AndAlso TypeOf parent Is VisualBasic.Syntax.MethodBlockBaseSyntax
            End Function

            Friend Overrides Function CreateRed(parent As SyntaxNode, startLocation As Integer) As SyntaxNode
                Dim isSeparated = SlotCount > 1 AndAlso HasNodeTokenPattern()
                If ShouldMakeWeakList(parent) Then
                    If isSeparated Then
                        Return New VisualBasic.Syntax.SyntaxList.WeakSeparatedWithManyChildren(Me, parent, startLocation)
                    End If
                    Return New VisualBasic.Syntax.SyntaxList.WeakWithManyChildren(Me, parent, startLocation)
                Else
                    If isSeparated Then
                        Return New VisualBasic.Syntax.SyntaxList.SeparatedWithManyChildren(Me, parent, startLocation)
                    End If
                    Return New VisualBasic.Syntax.SyntaxList.WithManyChildren(Me, parent, startLocation)
                End If
            End Function

            Private Function HasNodeTokenPattern() As Boolean
                For i = 0 To Me.SlotCount - 1
                    ' even slots must not be tokens and odd slots must be
                    If Me.GetSlot(i).IsToken = ((i And 1) = 0) Then
                        Return False
                    End If
                Next

                Return True
            End Function

            Friend MustOverride Overrides Function SetDiagnostics(newErrors() As DiagnosticInfo) As GreenNode

            Friend MustOverride Overrides Function SetAnnotations(annotations() As SyntaxAnnotation) As GreenNode

        End Class

        Friend NotInheritable Class WithManyChildren
            Inherits WithManyChildrenBase

            Friend Sub New(children As ArrayElement(Of VisualBasicSyntaxNode)())
                MyBase.New(children)
            End Sub

            Private Sub New(errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), children As ArrayElement(Of VisualBasicSyntaxNode)())
                MyBase.New(errors, annotations, children)
            End Sub

            Friend Sub New(reader As ObjectReader)
                MyBase.New(reader)
            End Sub

            Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
                Return Function(r) New WithManyChildren(r)
            End Function

            Friend Overrides Function SetDiagnostics(errors() As DiagnosticInfo) As GreenNode
                Return New WithManyChildren(errors, Me.GetAnnotations(), Me._children)
            End Function

            Friend Overrides Function SetAnnotations(annotations() As SyntaxAnnotation) As GreenNode
                Return New WithManyChildren(Me.GetDiagnostics(), annotations, Me._children)
            End Function
        End Class

        Friend NotInheritable Class WithLotsOfChildren
            Inherits WithManyChildrenBase

            Private ReadOnly _childOffsets As Integer()
            Friend Sub New(children As ArrayElement(Of VisualBasicSyntaxNode)())
                MyBase.New(children)
                _childOffsets = CalculateOffsets(children)
            End Sub

            Private Sub New(errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), children As ArrayElement(Of VisualBasicSyntaxNode)(), childOffsets As Integer())
                MyBase.New(errors, annotations, children)
                _childOffsets = childOffsets
            End Sub

            Friend Sub New(reader As ObjectReader)
                MyBase.New(reader)
                _childOffsets = CalculateOffsets(_children)
            End Sub

            Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
                Return Function(r) New WithLotsOfChildren(r)
            End Function

            Public Overrides Function GetSlotOffset(index As Integer) As Integer
                Return _childOffsets(index)
            End Function

            ''' <summary>
            ''' Find the slot that contains the given offset.
            ''' </summary>
            ''' <param name="offset">The target offset. Must be between 0 and <see cref="GreenNode.FullWidth"/>.</param>
            ''' <returns>The slot index of the slot containing the given offset.</returns>
            ''' <remarks>
            ''' This implementation uses a binary search to find the first slot that contains
            ''' the given offset.
            ''' </remarks>
            Public Overrides Function FindSlotIndexContainingOffset(offset As Integer) As Integer
                Debug.Assert(offset >= 0 AndAlso offset < FullWidth)
                Return _childOffsets.BinarySearchUpperBound(offset) - 1
            End Function

            Friend Overrides Function SetDiagnostics(errors() As DiagnosticInfo) As GreenNode
                Return New WithLotsOfChildren(errors, Me.GetAnnotations(), Me._children, Me._childOffsets)
            End Function

            Friend Overrides Function SetAnnotations(annotations() As SyntaxAnnotation) As GreenNode
                Return New WithLotsOfChildren(Me.GetDiagnostics(), annotations, Me._children, Me._childOffsets)
            End Function

            Private Shared Function CalculateOffsets(children As ArrayElement(Of VisualBasicSyntaxNode)()) As Integer()
                Dim n = children.Length
                Dim childOffsets = New Integer(n - 1) {}
                Dim offset = 0
                For i = 0 To n - 1
                    childOffsets(i) = offset
                    offset += children(i).Value.FullWidth
                Next
                Return childOffsets
            End Function
        End Class

    End Class

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

    Friend Structure SyntaxListBuilder(Of TNode As VisualBasicSyntaxNode)
        Private ReadOnly _builder As SyntaxListBuilder

        Public Shared Function Create() As SyntaxListBuilder(Of TNode)
            Return New SyntaxListBuilder(Of TNode)(8)
        End Function

        Public Sub New(size As Integer)
            Me.New(New SyntaxListBuilder(size))
        End Sub

        Friend Sub New(builder As SyntaxListBuilder)
            Me._builder = builder
        End Sub

        Public ReadOnly Property IsNull As Boolean
            Get
                Return (Me._builder Is Nothing)
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return Me._builder.Count
            End Get
        End Property

        Default Public Property Item(index As Integer) As TNode
            Get
                Return DirectCast(Me._builder.Item(index), TNode)
            End Get
            Set(value As TNode)
                Me._builder.Item(index) = value
            End Set
        End Property

        Friend Sub RemoveLast()
            Me._builder.RemoveLast()
        End Sub

        Public Sub Clear()
            Me._builder.Clear()
        End Sub

        Public Sub Add(node As TNode)
            Me._builder.Add(node)
        End Sub

        Public Sub AddRange(nodes As SyntaxList(Of TNode))
            Me._builder.AddRange(Of TNode)(nodes)
        End Sub

        Public Sub AddRange(nodes As SyntaxList(Of TNode), offset As Integer, length As Integer)
            Me._builder.AddRange(Of TNode)(nodes, offset, length)
        End Sub

        Public Function Any(kind As SyntaxKind) As Boolean
            Return Me._builder.Any(kind)
        End Function

        Public Function ToList() As SyntaxList(Of TNode)
            Debug.Assert(Me._builder IsNot Nothing)
            Return Me._builder.ToList(Of TNode)()
        End Function

        Public Function ToList(Of TDerivedNode As TNode)() As SyntaxList(Of TDerivedNode)
            Debug.Assert(Me._builder IsNot Nothing)
            Return Me._builder.ToList(Of TDerivedNode)()
        End Function

        Public Shared Widening Operator CType(builder As SyntaxListBuilder(Of TNode)) As SyntaxListBuilder
            Return builder._builder
        End Operator

        Public Shared Widening Operator CType(builder As SyntaxListBuilder(Of TNode)) As SyntaxList(Of TNode)
            Return builder.ToList
        End Operator
    End Structure

    Friend Structure SeparatedSyntaxListBuilder(Of TNode As VisualBasicSyntaxNode)
        Private ReadOnly _builder As SyntaxListBuilder
        Public Sub New(size As Integer)
            Me.New(New SyntaxListBuilder(size))
        End Sub

        Friend Sub New(builder As SyntaxListBuilder)
            Me._builder = builder
        End Sub

        Public ReadOnly Property IsNull As Boolean
            Get
                Return (Me._builder Is Nothing)
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return Me._builder.Count
            End Get
        End Property

        Default Public Property Item(index As Integer) As VisualBasicSyntaxNode
            Get
                Return Me._builder.Item(index)
            End Get
            Set(value As VisualBasicSyntaxNode)
                Me._builder.Item(index) = value
            End Set
        End Property

        Public Sub Clear()
            Me._builder.Clear()
        End Sub

        Public Sub Add(node As TNode)
            Me._builder.Add(node)
        End Sub

        Friend Sub AddSeparator(separatorToken As SyntaxToken)
            Me._builder.Add(separatorToken)
        End Sub

        Friend Sub AddRange(nodes As SeparatedSyntaxList(Of TNode), count As Integer)
            Dim list = nodes.GetWithSeparators
            Me._builder.AddRange(list, Me.Count, Math.Min(count * 2, list.Count))
        End Sub

        Friend Sub RemoveLast()
            Me._builder.RemoveLast()
        End Sub

        Public Function Any(kind As SyntaxKind) As Boolean
            Return Me._builder.Any(kind)
        End Function

        Public Function ToList() As SeparatedSyntaxList(Of TNode)
            Return New SeparatedSyntaxList(Of TNode)(New SyntaxList(Of VisualBasicSyntaxNode)(Me._builder.ToListNode))
        End Function

        Public Function ToList(Of TDerivedNode As TNode)() As SeparatedSyntaxList(Of TDerivedNode)
            Return New SeparatedSyntaxList(Of TDerivedNode)(New SyntaxList(Of VisualBasicSyntaxNode)(Me._builder.ToListNode))
        End Function

        Public Shared Widening Operator CType(builder As SeparatedSyntaxListBuilder(Of TNode)) As SyntaxListBuilder
            Return builder._builder
        End Operator
    End Structure

    Friend Structure SyntaxList(Of TNode As VisualBasicSyntaxNode)
        Implements IEquatable(Of SyntaxList(Of TNode))

        Private ReadOnly _node As GreenNode

        Friend Sub New(node As GreenNode)
            Me._node = node
        End Sub

        Friend ReadOnly Property Node As VisualBasicSyntaxNode
            Get
                Return DirectCast(Me._node, VisualBasicSyntaxNode)
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return If((Me._node Is Nothing), 0, If(Me._node.IsList, Me._node.SlotCount, 1))
            End Get
        End Property

        Public ReadOnly Property Last As TNode
            Get
                Dim node = Me._node
                If node.IsList Then
                    Return DirectCast(node.GetSlot(node.SlotCount - 1), TNode)
                End If
                Return DirectCast(node, TNode)
            End Get
        End Property

        Default Public ReadOnly Property Item(index As Integer) As TNode
            Get
                Dim node = Me._node
                If node.IsList Then
                    Return DirectCast(node.GetSlot(index), TNode)
                End If
                Debug.Assert(index = 0)
                Return DirectCast(node, TNode)
            End Get
        End Property

        Friend ReadOnly Property ItemUntyped(index As Integer) As GreenNode
            Get
                Dim node = Me._node
                If node.IsList Then
                    Return node.GetSlot(index)
                End If
                Debug.Assert(index = 0)
                Return node
            End Get
        End Property

        Public Function Any() As Boolean
            Return Me._node IsNot Nothing
        End Function

        Public Function Any(kind As SyntaxKind) As Boolean
            For i = 0 To Me.Count - 1
                Dim element = Me.ItemUntyped(i)
                If (element.RawKind = kind) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Friend ReadOnly Property Nodes As TNode()
            Get
                Dim arr = New TNode(Me.Count - 1) {}
                For i = 0 To Me.Count - 1
                    arr(i) = Me.Item(i)
                Next
                Return arr
            End Get
        End Property

        Public Shared Operator =(left As SyntaxList(Of TNode), right As SyntaxList(Of TNode)) As Boolean
            Return (left._node Is right._node)
        End Operator

        Public Shared Operator <>(left As SyntaxList(Of TNode), right As SyntaxList(Of TNode)) As Boolean
            Return (Not left._node Is right._node)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return (TypeOf obj Is SyntaxList(Of TNode) AndAlso Me.Equals(DirectCast(obj, SyntaxList(Of TNode))))
        End Function

        Public Overloads Function Equals(other As SyntaxList(Of TNode)) As Boolean Implements IEquatable(Of SyntaxList(Of TNode)).Equals
            Return Me._node Is other._node
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If((Not Me._node Is Nothing), Me._node.GetHashCode, 0)
        End Function

        Friend Function AsSeparatedList(Of TOther As VisualBasicSyntaxNode)() As SeparatedSyntaxList(Of TOther)
            Return New SeparatedSyntaxList(Of TOther)(New SyntaxList(Of TOther)(Me._node))
        End Function

        Public Shared Widening Operator CType(node As TNode) As SyntaxList(Of TNode)
            Return New SyntaxList(Of TNode)(node)
        End Operator

        Public Shared Widening Operator CType(nodes As SyntaxList(Of TNode)) As SyntaxList(Of VisualBasicSyntaxNode)
            Return New SyntaxList(Of VisualBasicSyntaxNode)(nodes._node)
        End Operator
    End Structure
End Namespace
