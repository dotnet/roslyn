' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Operations
    Friend NotInheritable Class VisualBasicLazyNoneOperation
        Inherits LazyNoneOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundNode As BoundNode

        Public Sub New(operationFactory As VisualBasicOperationFactory, boundNode As BoundNode, semanticModel As SemanticModel, node As SyntaxNode, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(semanticModel, node, constantValue, isImplicit, type:=Nothing)
            _operationFactory = operationFactory
            _boundNode = boundNode
        End Sub

        Protected Overrides Function GetChildren() As ImmutableArray(Of IOperation)
            Return _operationFactory.GetIOperationChildren(_boundNode)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInvalidOperation
        Inherits LazyInvalidOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _originalNode As IBoundInvalidNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, originalNode As IBoundInvalidNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _originalNode = originalNode
        End Sub

        Protected Overrides Function CreateChildren() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundNode, IOperation)(_originalNode.InvalidNodeChildren)
        End Function
    End Class
End Namespace
