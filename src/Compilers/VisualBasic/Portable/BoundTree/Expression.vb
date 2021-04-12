' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundBadExpression
        Implements IBoundInvalidNode

        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.ChildBoundNodes)
            End Get
        End Property

        Private ReadOnly Property IBoundInvalidNode_InvalidNodeChildren As ImmutableArray(Of BoundNode) Implements IBoundInvalidNode.InvalidNodeChildren
            Get
                Return StaticCast(Of BoundNode).From(ChildBoundNodes)
            End Get
        End Property
    End Class

    Partial Friend Class BoundAssignmentOperator
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Left, Me.Right)
            End Get
        End Property
    End Class

    Partial Friend Class BoundDelegateCreationExpression
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.ReceiverOpt)
            End Get
        End Property
    End Class

    Partial Friend Class BoundAddressOfOperator
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.MethodGroup)
            End Get
        End Property
    End Class

    Partial Friend Class BoundMethodOrPropertyGroup
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                If Me.ReceiverOpt IsNot Nothing Then
                    Return ImmutableArray.Create(Of BoundNode)(Me.ReceiverOpt)
                Else
                    Return ImmutableArray(Of BoundNode).Empty
                End If
            End Get
        End Property
    End Class

    Partial Friend Class BoundNullableIsTrueOperator
        Implements IBoundInvalidNode

        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Operand)
            End Get
        End Property

        Private ReadOnly Property IBoundInvalidNode_InvalidNodeChildren As ImmutableArray(Of BoundNode) Implements IBoundInvalidNode.InvalidNodeChildren
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Operand)
            End Get
        End Property
    End Class

    Partial Friend Class BoundAttribute
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.ConstructorArguments.AddRange(Me.NamedArguments))
            End Get
        End Property
    End Class

    Partial Friend Class BoundLateInvocation
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.ArgumentsOpt.Insert(0, Me.Member))
            End Get
        End Property
    End Class

    Partial Friend Class BoundLateAddressOfOperator
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.MemberAccess)
            End Get
        End Property
    End Class

    Partial Friend Class BoundAnonymousTypeCreationExpression
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.Arguments)
            End Get
        End Property
    End Class

    Partial Friend Class BoundAnonymousTypeFieldInitializer
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Value)
            End Get
        End Property
    End Class

    Partial Friend Class BoundArrayLiteral
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return StaticCast(Of BoundNode).From(Me.Bounds.Add(Me.Initializer))
            End Get
        End Property
    End Class

    Partial Friend Class BoundQueryExpression
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.LastOperator)
            End Get
        End Property
    End Class

    Partial Friend Class BoundQueryPart
        Protected MustOverride Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
    End Class

    Partial Friend Class BoundQuerySource
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Expression)
            End Get
        End Property
    End Class

    Partial Friend Class BoundToQueryableCollectionConversion
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.ConversionCall)
            End Get
        End Property
    End Class

    Partial Friend Class BoundQueryableSource
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Source)
            End Get
        End Property
    End Class

    Partial Friend Class BoundQueryClause
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.UnderlyingExpression)
            End Get
        End Property
    End Class

    Partial Friend Class BoundOrdering
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.UnderlyingExpression)
            End Get
        End Property
    End Class

    Partial Friend Class BoundQueryLambda
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Expression)
            End Get
        End Property
    End Class

    Partial Friend Class BoundRangeVariableAssignment
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Value)
            End Get
        End Property
    End Class

    Partial Friend Class BoundAggregateClause
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.CapturedGroupOpt, Me.UnderlyingExpression)
            End Get
        End Property
    End Class

    Partial Friend Class BoundGroupAggregation
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Group)
            End Get
        End Property
    End Class

    Partial Friend Class BoundMidResult
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Original, Me.Start, Me.LengthOpt, Me.Source)
            End Get
        End Property
    End Class

    Partial Friend Class BoundNameOfOperator
        Protected Overrides ReadOnly Property Children As ImmutableArray(Of BoundNode)
            Get
                Return ImmutableArray.Create(Of BoundNode)(Me.Argument)
            End Get
        End Property
    End Class
End Namespace
