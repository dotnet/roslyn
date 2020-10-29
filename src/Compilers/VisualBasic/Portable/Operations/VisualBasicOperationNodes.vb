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

    Friend NotInheritable Class VisualBasicLazyVariableInitializerOperation
        Inherits LazyVariableInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(locals:=ImmutableArray(Of ILocalSymbol).Empty, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyFieldInitializerOperation
        Inherits LazyFieldInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), initializedFields As ImmutableArray(Of IFieldSymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(initializedFields, locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolatedStringTextOperation
        Inherits LazyInterpolatedStringTextOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _text As BoundLiteral

        Friend Sub New(operationFactory As VisualBasicOperationFactory, text As BoundLiteral, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _text = text
        End Sub

        Protected Overrides Function CreateText() As IOperation
            Return _operationFactory.CreateBoundLiteralOperation(_text, implicit:=True)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolationOperation
        Inherits LazyInterpolationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _interpolation As BoundInterpolation

        Friend Sub New(operationFactory As VisualBasicOperationFactory, interpolation As BoundInterpolation, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _interpolation = interpolation
        End Sub

        Protected Overrides Function CreateExpression() As IOperation
            Return _operationFactory.Create(_interpolation.Expression)
        End Function

        Protected Overrides Function CreateAlignment() As IOperation
            Return _operationFactory.Create(_interpolation.AlignmentOpt)
        End Function

        Protected Overrides Function CreateFormatString() As IOperation
            Return _operationFactory.Create(_interpolation.FormatStringOpt)
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

    Friend NotInheritable Class VisualBasicLazyDynamicMemberReferenceOperation
        Inherits LazyDynamicMemberReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, memberName As String, typeArguments As ImmutableArray(Of ITypeSymbol), containingType As ITypeSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.Create(_instance)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyParameterInitializerOperation
        Inherits LazyParameterInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), parameter As IParameterSymbol, kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(parameter, locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyPropertyInitializerOperation
        Inherits LazyPropertyInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), initializedProperties As ImmutableArray(Of IPropertySymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(initializedProperties, locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRangeCaseClauseOperation
        Inherits LazyRangeCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _rangeCaseClause As BoundRangeCaseClause

        Friend Sub New(operationFactory As VisualBasicOperationFactory, rangeCaseClause As BoundRangeCaseClause, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(CaseKind.Range, label:=Nothing, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _rangeCaseClause = rangeCaseClause
        End Sub

        Protected Overrides Function CreateMinimumValue() As IOperation
            Return _operationFactory.Create(VisualBasicOperationFactory.GetCaseClauseValue(_rangeCaseClause.LowerBoundOpt, _rangeCaseClause.LowerBoundConditionOpt))
        End Function

        Protected Overrides Function CreateMaximumValue() As IOperation
            Return _operationFactory.Create(VisualBasicOperationFactory.GetCaseClauseValue(_rangeCaseClause.UpperBoundOpt, _rangeCaseClause.UpperBoundConditionOpt))
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRelationalCaseClauseOperation
        Inherits LazyRelationalCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, relation As BinaryOperatorKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(relation, CaseKind.Relational, label:=Nothing, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySingleValueCaseClauseOperation
        Inherits LazySingleValueCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, label As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(CaseKind.SingleValue, label, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicInvocationOperation
        Inherits LazyDynamicInvocationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _lateInvocation As BoundLateInvocation

        Friend Sub New(operationFactory As VisualBasicOperationFactory, lateInvocation As BoundLateInvocation, argumentNames As ImmutableArray(Of String), argumentRefKinds As ImmutableArray(Of RefKind), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _lateInvocation = lateInvocation
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_lateInvocation.Member)
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_lateInvocation.ArgumentsOpt)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableDeclarationGroupOperation
        Inherits LazyVariableDeclarationGroupOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _localDeclarations As IBoundLocalDeclarations

        Friend Sub New(operationFactory As VisualBasicOperationFactory, localDeclarations As IBoundLocalDeclarations, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _localDeclarations = localDeclarations
        End Sub

        Protected Overrides Function CreateDeclarations() As ImmutableArray(Of IVariableDeclarationOperation)
            Return _operationFactory.GetVariableDeclarationStatementVariables(_localDeclarations.Declarations)
        End Function
    End Class
End Namespace
