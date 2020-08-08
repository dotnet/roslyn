' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundNode
        Implements IBoundNodeWithIOperationChildren

        Public ReadOnly Property IBoundNodeWithIOperationChildren_Children As ImmutableArray(Of BoundNode) Implements IBoundNodeWithIOperationChildren.Children
            Get
                Return Me.Children
            End Get
        End Property

        ''' <summary>
        ''' Override this property to return the child nodes if the IOperation API corresponding to this bound node is not yet designed or implemented.
        ''' </summary>
        ''' <remarks>
        ''' Note that any of the child bound nodes may be null.
        ''' </remarks>
        Protected Overridable ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray(Of BoundNode).Empty
            End Get
        End Property
    End Class

    Partial Friend Class BoundCaseBlock
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.CaseStatement, Me.Body)
            End Get
        End Property
    End Class

    Partial Friend Class BoundCaseStatement
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.CaseClauses).Add(Me.ConditionOpt)
            End Get
        End Property
    End Class

    Partial Friend Class BoundBadStatement
        Implements IBoundInvalidNode
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return Me.ChildBoundNodes
            End Get
        End Property

        Private ReadOnly Property IBoundInvalidNode_InvalidNodeChildren As ImmutableArray(Of BoundNode) Implements IBoundInvalidNode.InvalidNodeChildren
            Get
                Return ChildBoundNodes
            End Get
        End Property
    End Class

    Partial Friend Class BoundEraseStatement
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.Clauses)
            End Get
        End Property
    End Class

    Partial Friend Class BoundRaiseEventStatement
        Implements IBoundInvalidNode

        Private ReadOnly Property IBoundInvalidNode_InvalidNodeChildren As ImmutableArray(Of BoundNode) Implements IBoundInvalidNode.InvalidNodeChildren
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.EventInvocation)
            End Get
        End Property
    End Class

    Partial Friend Class BoundResumeStatement
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.LabelExpressionOpt)
            End Get
        End Property
    End Class

    Partial Friend Class BoundOnErrorStatement
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.LabelExpressionOpt)
            End Get
        End Property
    End Class

    Partial Friend Class BoundUnstructuredExceptionHandlingStatement
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Body)
            End Get
        End Property
    End Class
End Namespace
