' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class BoundTreeRewriter
        Inherits BoundTreeVisitor

        Public Overridable Function VisitType(type As TypeSymbol) As TypeSymbol
            Return type
        End Function

        Public Overridable Function VisitList(Of T As BoundNode)(list As ImmutableArray(Of T)) As ImmutableArray(Of T)
            Dim newList As ArrayBuilder(Of T) = Nothing
            Dim i As Integer = 0
            Dim n As Integer = If(list.IsDefault, 0, list.Length)

            While (i < n)
                Dim item As T = list.Item(i)
                Debug.Assert(item IsNot Nothing)

                Dim visited = Me.Visit(item)

                If item IsNot visited AndAlso newList Is Nothing Then
                    newList = ArrayBuilder(Of T).GetInstance
                    If i > 0 Then
                        newList.AddRange(list, i)
                    End If
                End If

                If newList IsNot Nothing AndAlso visited IsNot Nothing Then
                    newList.Add(DirectCast(visited, T))
                End If

                i += 1
            End While

            If newList IsNot Nothing Then
                Return newList.ToImmutableAndFree
            Else
                Return list
            End If
        End Function

        Public Sub VisitList(Of T As BoundNode)(list As ImmutableArray(Of T), results As ArrayBuilder(Of T))
            For Each item In list
                results.Add(DirectCast(Visit(item), T))
            Next
        End Sub

    End Class

    Friend MustInherit Class BoundTreeRewriterWithStackGuard
        Inherits BoundTreeRewriter

        Private _recursionDepth As Integer

        Protected Sub New()
        End Sub

        Protected Sub New(recursionDepth As Integer)
            _recursionDepth = recursionDepth
        End Sub

        Protected ReadOnly Property RecursionDepth As Integer
            Get
                Return _recursionDepth
            End Get
        End Property

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            Dim expression = TryCast(node, BoundExpression)

            If expression IsNot Nothing Then
                Return VisitExpressionWithStackGuard(_recursionDepth, expression)
            End If

            Return MyBase.Visit(node)
        End Function

        Protected Overloads Function VisitExpressionWithStackGuard(expression As BoundExpression) As BoundExpression
            Return VisitExpressionWithStackGuard(_recursionDepth, expression)
        End Function

        Protected NotOverridable Overrides Function VisitExpressionWithoutStackGuard(node As BoundExpression) As BoundExpression
            Return DirectCast(MyBase.Visit(node), BoundExpression)
        End Function

    End Class

    Friend MustInherit Class BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        Inherits BoundTreeRewriterWithStackGuard

        Protected Sub New()
        End Sub

        Protected Sub New(recursionDepth As Integer)
            MyBase.New(recursionDepth)
        End Sub

        Public NotOverridable Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
            Dim child As BoundExpression = node.Left

            If child.Kind <> BoundKind.BinaryOperator Then
                Return MyBase.VisitBinaryOperator(node)
            End If

            Dim stack = ArrayBuilder(Of BoundBinaryOperator).GetInstance()
            stack.Push(node)

            Dim binary As BoundBinaryOperator = DirectCast(child, BoundBinaryOperator)

            Do
                stack.Push(binary)
                child = binary.Left

                If child.Kind <> BoundKind.BinaryOperator Then
                    Exit Do
                End If

                binary = DirectCast(child, BoundBinaryOperator)
            Loop


            Dim left = DirectCast(Me.Visit(child), BoundExpression)

            Do
                binary = stack.Pop()

                Dim right = DirectCast(Me.Visit(binary.Right), BoundExpression)
                Dim type As TypeSymbol = Me.VisitType(binary.Type)
                left = binary.Update(binary.OperatorKind, left, right, binary.Checked, binary.ConstantValueOpt, type)
            Loop While stack.Count > 0

            Debug.Assert(binary Is node)
            stack.Free()

            Return left
        End Function
    End Class
End Namespace
