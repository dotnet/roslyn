' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class BoundTreeWalker
        Inherits BoundTreeVisitor

        Protected Sub New()
        End Sub

        Public Overridable Sub VisitList(Of T As BoundNode)(list As ImmutableArray(Of T))
            If Not list.IsDefault Then
                For Each item In list
                    Me.Visit(item)
                Next
            End If
        End Sub

    End Class

    Friend MustInherit Class BoundTreeWalkerWithStackGuard
        Inherits BoundTreeWalker

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

    Friend MustInherit Class BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        Inherits BoundTreeWalkerWithStackGuard

        Protected Sub New()
        End Sub

        Protected Sub New(recursionDepth As Integer)
            MyBase.New(recursionDepth)
        End Sub

        Public NotOverridable Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
            If node.Left.Kind <> BoundKind.BinaryOperator Then
                Return MyBase.VisitBinaryOperator(node)
            End If

            Dim rightOperands = ArrayBuilder(Of BoundExpression).GetInstance()

            rightOperands.Push(node.Right)

            Dim binary = DirectCast(node.Left, BoundBinaryOperator)

            rightOperands.Push(binary.Right)

            Dim current As BoundExpression = binary.Left

            While current.Kind = BoundKind.BinaryOperator
                binary = DirectCast(current, BoundBinaryOperator)
                rightOperands.Push(binary.Right)
                current = binary.Left
            End While

            Me.Visit(current)

            While rightOperands.Count > 0
                Me.Visit(rightOperands.Pop())
            End While

            rightOperands.Free()
            Return Nothing
        End Function

    End Class

    Friend Class StatementWalker
        Inherits BoundTreeWalker

#If DEBUG Then
        Public Overrides Function Visit(node As BoundNode) As BoundNode
            Debug.Assert(TypeOf node IsNot BoundExpression)
            Return MyBase.Visit(node)
        End Function
#End If

        Protected Overrides Function VisitExpressionWithoutStackGuard(node As BoundExpression) As BoundExpression
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
