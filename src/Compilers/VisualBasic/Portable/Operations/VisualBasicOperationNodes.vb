' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Operations
    Friend NotInheritable Class VisualBasicLazyNameOfOperation
        Inherits LazyNameOfOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _argument As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, argument As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _argument = argument
        End Sub

        Protected Overrides Function CreateArgument() As IOperation
            Return _operationFactory.Create(_argument)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyThrowOperation
        Inherits LazyThrowOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _exception As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, exception As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _exception = exception
        End Sub

        Protected Overrides Function CreateException() As IOperation
            Return _operationFactory.Create(_exception)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArgumentOperation
        Inherits LazyArgumentOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, argumentKind As ArgumentKind, inConversionOpt As IConvertibleConversion, outConversionOpt As IConvertibleConversion, parameter As IParameterSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, isImplicit As Boolean)
            MyBase.New(argumentKind, inConversionOpt, outConversionOpt, parameter, semanticModel, syntax, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayCreationOperation
        Inherits LazyArrayCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _dimensionSizes As ImmutableArray(Of BoundExpression)
        Private ReadOnly _initializer As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, dimensionSizes As ImmutableArray(Of BoundExpression), initializer As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _dimensionSizes = dimensionSizes
            _initializer = initializer
        End Sub

        Protected Overrides Function CreateDimensionSizes() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_dimensionSizes)
        End Function

        Protected Overrides Function CreateInitializer() As IArrayInitializerOperation
            Return DirectCast(_operationFactory.Create(_initializer), IArrayInitializerOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayElementReferenceOperation
        Inherits LazyArrayElementReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _arrayReference As BoundNode
        Private ReadOnly _indices As ImmutableArray(Of BoundExpression)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, arrayReference As BoundNode, indices As ImmutableArray(Of BoundExpression), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _arrayReference = arrayReference
            _indices = indices
        End Sub

        Protected Overrides Function CreateArrayReference() As IOperation
            Return _operationFactory.Create(_arrayReference)
        End Function

        Protected Overrides Function CreateIndices() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_indices)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayInitializerOperation
        Inherits LazyArrayInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _elementValues As ImmutableArray(Of BoundExpression)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, elementValues As ImmutableArray(Of BoundExpression), semanticModel As SemanticModel, syntax As SyntaxNode, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, constantValue, isImplicit)
            _operationFactory = operationFactory
            _elementValues = elementValues
        End Sub

        Protected Overrides Function CreateElementValues() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_elementValues)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySimpleAssignmentOperation
        Inherits LazySimpleAssignmentOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _target As BoundNode
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, target As BoundNode, value As BoundNode, isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isRef, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _target = target
            _value = value
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
            Return _operationFactory.Create(_target)
        End Function

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAwaitOperation
        Inherits LazyAwaitOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operation As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operation As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operation = operation
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_operation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyBinaryOperation
        Inherits LazyBinaryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operator As BoundExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, [operator] As BoundExpression, operatorKind As BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, isCompareText As Boolean, operatorMethod As IMethodSymbol, unaryOperatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operator = [operator]
        End Sub

        Protected Overrides Function CreateLeftOperand() As IOperation
            Return _operationFactory.CreateBoundBinaryOperatorChild(_operator, isLeft:=True)
        End Function

        Protected Overrides Function CreateRightOperand() As IOperation
            Return _operationFactory.CreateBoundBinaryOperatorChild(_operator, isLeft:=False)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyBlockOperation
        Inherits LazyBlockOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operations As ImmutableArray(Of BoundStatement)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operations As ImmutableArray(Of BoundStatement), locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operations = operations
        End Sub

        Protected Overrides Function CreateOperations() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundStatement, IOperation)(_operations)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCatchClauseOperation
        Inherits LazyCatchClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundCatchBlock As BoundCatchBlock

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundCatchBlock As BoundCatchBlock, exceptionType As ITypeSymbol, locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _boundCatchBlock = boundCatchBlock
        End Sub

        Protected Overrides Function CreateExceptionDeclarationOrExpression() As IOperation
            Return _operationFactory.CreateBoundCatchBlockExceptionDeclarationOrExpression(_boundCatchBlock)
        End Function

        Protected Overrides Function CreateFilter() As IOperation
            Return _operationFactory.Create(_boundCatchBlock.ExceptionFilterOpt)
        End Function

        Protected Overrides Function CreateHandler() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_boundCatchBlock.Body), IBlockOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCompoundAssignmentOperation
        Inherits LazyCompoundAssignmentOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _assignment As BoundAssignmentOperator

        Friend Sub New(operationFactory As VisualBasicOperationFactory, assignment As BoundAssignmentOperator, inConversionConvertible As IConvertibleConversion, outConversionConvertible As IConvertibleConversion, operatorKind As BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(inConversionConvertible, outConversionConvertible, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _assignment = assignment
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
            Return _operationFactory.Create(_assignment.Left)
        End Function

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.CreateCompoundAssignmentRightOperand(_assignment)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConditionalAccessOperation
        Inherits LazyConditionalAccessOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operation As BoundNode
        Private ReadOnly _whenNotNull As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operation As BoundNode, whenNotNull As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operation = operation
            _whenNotNull = whenNotNull
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_operation)
        End Function

        Protected Overrides Function CreateWhenNotNull() As IOperation
            Return _operationFactory.Create(_whenNotNull)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConditionalOperation
        Inherits LazyConditionalOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _condition As BoundNode
        Private ReadOnly _whenTrue As BoundNode
        Private ReadOnly _whenFalse As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, condition As BoundNode, whenTrue As BoundNode, whenFalse As BoundNode, isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isRef, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _condition = condition
            _whenTrue = whenTrue
            _whenFalse = whenFalse
        End Sub

        Protected Overrides Function CreateCondition() As IOperation
            Return _operationFactory.Create(_condition)
        End Function

        Protected Overrides Function CreateWhenTrue() As IOperation
            Return _operationFactory.Create(_whenTrue)
        End Function

        Protected Overrides Function CreateWhenFalse() As IOperation
            Return _operationFactory.Create(_whenFalse)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyEventAssignmentOperation
        Inherits LazyEventAssignmentOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _eventReference As BoundNode
        Private ReadOnly _handlerValue As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, eventReference As BoundNode, handlerValue As BoundNode, adds As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(adds, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _eventReference = eventReference
            _handlerValue = handlerValue
        End Sub

        Protected Overrides Function CreateEventReference() As IOperation
            Return _operationFactory.Create(_eventReference)
        End Function

        Protected Overrides Function CreateHandlerValue() As IOperation
            Return _operationFactory.Create(_handlerValue)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyEventReferenceOperation
        Inherits LazyEventReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, [event] As IEventSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New([event], semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, [Event])
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyExpressionStatementOperation
        Inherits LazyExpressionStatementOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operation As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operation As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operation = operation
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_operation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableInitializerOperation
        Inherits LazyVariableInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), initializedFields As ImmutableArray(Of IFieldSymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyFieldReferenceOperation
        Inherits LazyFieldReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, field As IFieldSymbol, isDeclaration As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, Field)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyForEachLoopOperation
        Inherits LazyForEachLoopOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _forEachLoop As BoundForEachStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, forEachLoop As BoundForEachStatement, locals As ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _forEachLoop = forEachLoop
        End Sub

        Protected Overrides Function CreateLoopInfo() As ForEachLoopOperationInfo
            Return _operationFactory.GetForEachLoopOperationInfo(_forEachLoop)
        End Function

        Protected Overrides Function CreateLoopControlVariable() As IOperation
            Return _operationFactory.CreateBoundControlVariableOperation(_forEachLoop)
        End Function

        Protected Overrides Function CreateCollection() As IOperation
            Return _operationFactory.Create(_forEachLoop.Collection)
        End Function

        Protected Overrides Function CreateNextVariables() As ImmutableArray(Of IOperation)
            Return If(_forEachLoop.NextVariablesOpt.IsDefault, ImmutableArray(Of IOperation).Empty, _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_forEachLoop.NextVariablesOpt))
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_forEachLoop.Body)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyForToLoopOperation
        Inherits LazyForToLoopOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundForToLoop As BoundForToStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundForToLoop As BoundForToStatement, locals As ImmutableArray(Of ILocalSymbol), isChecked As Boolean, info As (LoopObject As ILocalSymbol, UserDefinedInfo As ForToLoopOperationUserDefinedInfo), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, isChecked, info, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _boundForToLoop = boundForToLoop
        End Sub

        Protected Overrides Function CreateLoopControlVariable() As IOperation
            Return _operationFactory.CreateBoundControlVariableOperation(_boundForToLoop)
        End Function

        Protected Overrides Function CreateInitialValue() As IOperation
            Return _operationFactory.Create(_boundForToLoop.InitialValue)
        End Function

        Protected Overrides Function CreateLimitValue() As IOperation
            Return _operationFactory.Create(_boundForToLoop.LimitValue)
        End Function

        Protected Overrides Function CreateStepValue() As IOperation
            Return _operationFactory.Create(_boundForToLoop.StepValue)
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_boundForToLoop.Body)
        End Function

        Protected Overrides Function CreateNextVariables() As ImmutableArray(Of IOperation)
            Return If(_boundForToLoop.NextVariablesOpt.IsDefault,
                      ImmutableArray(Of IOperation).Empty,
_operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_boundForToLoop.NextVariablesOpt))
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolatedStringOperation
        Inherits LazyInterpolatedStringOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _parts As ImmutableArray(Of BoundNode)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, parts As ImmutableArray(Of BoundNode), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _parts = parts
        End Sub

        Protected Overrides Function CreateParts() As ImmutableArray(Of IInterpolatedStringContentOperation)
            Return _operationFactory.CreateBoundInterpolatedStringContentOperation(_parts)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolatedStringTextOperation
        Inherits LazyInterpolatedStringTextOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _text As BoundLiteral

        Friend Sub New(operationFactory As VisualBasicOperationFactory, text As BoundLiteral, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
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
        Private ReadOnly _expression As BoundNode
        Private ReadOnly _alignment As BoundNode
        Private ReadOnly _formatString As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, expression As BoundNode, alignment As BoundNode, formatString As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _expression = expression
            _alignment = alignment
            _formatString = formatString
        End Sub

        Protected Overrides Function CreateExpression() As IOperation
            Return _operationFactory.Create(_expression)
        End Function

        Protected Overrides Function CreateAlignment() As IOperation
            Return _operationFactory.Create(_alignment)
        End Function

        Protected Overrides Function CreateFormatString() As IOperation
            Return _operationFactory.Create(_formatString)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInvalidOperation
        Inherits LazyInvalidOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _children As ImmutableArray(Of BoundNode)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, children As ImmutableArray(Of BoundNode), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _children = children
        End Sub

        Protected Overrides Function CreateChildren() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundNode, IOperation)(_children)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInvocationOperation
        Inherits LazyInvocationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundCall As BoundCall
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, boundCall As BoundCall, targetMethod As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
            _boundCall = boundCall
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, TargetMethod)
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IArgumentOperation)
            Return If(_boundCall IsNot Nothing, _operationFactory.DeriveArguments(_boundCall), ImmutableArray(Of IArgumentOperation).Empty)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRaiseEventOperation
        Inherits LazyRaiseEventOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _raiseEventStatement As BoundRaiseEventStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, raiseEventStatement As BoundRaiseEventStatement, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _raiseEventStatement = raiseEventStatement
        End Sub

        Protected Overrides Function CreateEventReference() As IEventReferenceOperation
            Return _operationFactory.CreateBoundRaiseEventStatementEventReference(_raiseEventStatement)
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IArgumentOperation)
            Return _operationFactory.DeriveArguments(_raiseEventStatement)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyIsTypeOperation
        Inherits LazyIsTypeOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _valueOperand As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, valueOperand As BoundNode, isType As ITypeSymbol, isNotTypeExpression As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isType, isNotTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _valueOperand = valueOperand
        End Sub

        Protected Overrides Function CreateValueOperand() As IOperation
            Return _operationFactory.Create(_valueOperand)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyLabeledOperation
        Inherits LazyLabeledOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operation As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operation As BoundNode, label As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(label, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operation = operation
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_operation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAnonymousFunctionOperation
        Inherits LazyAnonymousFunctionOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _body As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, body As BoundNode, symbol As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(symbol, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _body = body
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_body), IBlockOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDelegateCreationOperation
        Inherits LazyDelegateCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _delegateCreation As BoundDelegateCreationExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, delegateCreation As BoundDelegateCreationExpression, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _delegateCreation = delegateCreation
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
            Return _operationFactory.CreateBoundDelegateCreationExpressionChildOperation(_delegateCreation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicMemberReferenceOperation
        Inherits LazyDynamicMemberReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, memberName As String, typeArguments As ImmutableArray(Of ITypeSymbol), containingType As ITypeSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.Create(_instance)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyLockOperation
        Inherits LazyLockOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _lockedValue As BoundNode
        Private ReadOnly _body As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, lockedValue As BoundNode, body As BoundNode, lockTakenSymbol As ILocalSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _lockedValue = lockedValue
            _body = body
        End Sub

        Protected Overrides Function CreateLockedValue() As IOperation
            Return _operationFactory.Create(_lockedValue)
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_body)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyMethodReferenceOperation
        Inherits LazyMethodReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, method As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, Method)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCoalesceOperation
        Inherits LazyCoalesceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode
        Private ReadOnly _whenNull As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, whenNull As BoundNode, convertibleValueConversion As IConvertibleConversion, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
            _whenNull = whenNull
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function

        Protected Overrides Function CreateWhenNull() As IOperation
            Return _operationFactory.Create(_whenNull)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyObjectCreationOperation
        Inherits LazyObjectCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _objectCreation As BoundObjectCreationExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, objectCreation As BoundObjectCreationExpression, constructor As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(constructor, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _objectCreation = objectCreation
        End Sub

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
            Return DirectCast(_operationFactory.Create(_objectCreation.InitializerOpt), IObjectOrCollectionInitializerOperation)
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IArgumentOperation)
            Return _operationFactory.DeriveArguments(_objectCreation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAnonymousObjectCreationOperation
        Inherits LazyAnonymousObjectCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _anonymousTypeCreation As BoundAnonymousTypeCreationExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, anonymousTypeCreation As BoundAnonymousTypeCreationExpression, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _anonymousTypeCreation = anonymousTypeCreation
        End Sub

        Protected Overrides Function CreateInitializers() As ImmutableArray(Of IOperation)
            Return _operationFactory.GetAnonymousTypeCreationInitializers(_anonymousTypeCreation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyParameterInitializerOperation
        Inherits LazyParameterInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), parameter As IParameterSymbol, kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyParenthesizedOperation
        Inherits LazyParenthesizedOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operand As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operand As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operand = operand
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
            Return _operationFactory.Create(_operand)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyPropertyInitializerOperation
        Inherits LazyPropertyInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), initializedProperties As ImmutableArray(Of IPropertySymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, initializedProperties, kind, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyPropertyReferenceOperation
        Inherits LazyPropertyReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode
        Private ReadOnly _boundProperty As BoundPropertyAccess

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, boundProperty As BoundPropertyAccess, [property] As IPropertySymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New([property], semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
            _boundProperty = boundProperty
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, [Property])
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IArgumentOperation)
            Return _operationFactory.DeriveArguments(_boundProperty)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRangeCaseClauseOperation
        Inherits LazyRangeCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _minimumValue As BoundNode
        Private ReadOnly _maximumValue As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, minimumValue As BoundNode, maximumValue As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _minimumValue = minimumValue
            _maximumValue = maximumValue
        End Sub

        Protected Overrides Function CreateMinimumValue() As IOperation
            Return _operationFactory.Create(_minimumValue)
        End Function

        Protected Overrides Function CreateMaximumValue() As IOperation
            Return _operationFactory.Create(_maximumValue)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRelationalCaseClauseOperation
        Inherits LazyRelationalCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, relation As BinaryOperatorKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(relation, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReturnOperation
        Inherits LazyReturnOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _returnedValue As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, returnedValue As BoundNode, kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(kind, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _returnedValue = returnedValue
        End Sub

        Protected Overrides Function CreateReturnedValue() As IOperation
            Return _operationFactory.Create(_returnedValue)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySingleValueCaseClauseOperation
        Inherits LazySingleValueCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, label As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(label, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySwitchCaseOperation
        Inherits LazySwitchCaseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _caseBlock As BoundCaseBlock

        Friend Sub New(operationFactory As VisualBasicOperationFactory, caseBlock As BoundCaseBlock, locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _caseBlock = caseBlock
        End Sub

        Protected Overrides Function CreateClauses() As ImmutableArray(Of ICaseClauseOperation)
            Return _operationFactory.CreateBoundCaseBlockClauses(_caseBlock)
        End Function

        Protected Overrides Function CreateCondition() As IOperation
            Return _operationFactory.CreateBoundCaseBlockCondition(_caseBlock)
        End Function

        Protected Overrides Function CreateBody() As ImmutableArray(Of IOperation)
            Return ImmutableArray.Create(_operationFactory.Create(_caseBlock.Body))
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySwitchOperation
        Inherits LazySwitchOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _value As BoundNode
        Private ReadOnly _cases As ImmutableArray(Of BoundCaseBlock)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, cases As ImmutableArray(Of BoundCaseBlock), locals As ImmutableArray(Of ILocalSymbol), exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _value = value
            _cases = cases
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function

        Protected Overrides Function CreateCases() As ImmutableArray(Of ISwitchCaseOperation)
            Return _operationFactory.CreateFromArray(Of BoundCaseBlock, ISwitchCaseOperation)(_cases)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTryOperation
        Inherits LazyTryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _body As BoundNode
        Private ReadOnly _catches As ImmutableArray(Of BoundCatchBlock)
        Private ReadOnly _finally As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, body As BoundNode, catches As ImmutableArray(Of BoundCatchBlock), [finally] As BoundNode, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _body = body
            _catches = catches
            _finally = [finally]
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_body), IBlockOperation)
        End Function

        Protected Overrides Function CreateCatches() As ImmutableArray(Of ICatchClauseOperation)
            Return _operationFactory.CreateFromArray(Of BoundCatchBlock, ICatchClauseOperation)(_catches)
        End Function

        Protected Overrides Function CreateFinally() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_finally), IBlockOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTupleOperation
        Inherits LazyTupleOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _elements As ImmutableArray(Of BoundExpression)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, elements As ImmutableArray(Of BoundExpression), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, naturalType As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, naturalType, constantValue, isImplicit)
            _operationFactory = operationFactory
            _elements = elements
        End Sub

        Protected Overrides Function CreateElements() As ImmutableArray(Of IOperation)
            Return SetParentOperation(_operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_elements), Me)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTypeParameterObjectCreationOperation
        Inherits LazyTypeParameterObjectCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _initializer As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, initializer As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _initializer = initializer
        End Sub

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
            Return DirectCast(_operationFactory.Create(_initializer), IObjectOrCollectionInitializerOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicInvocationOperation
        Inherits LazyDynamicInvocationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operation As BoundNode
        Private ReadOnly _arguments As ImmutableArray(Of BoundExpression)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operation As BoundNode, arguments As ImmutableArray(Of BoundExpression), argumentNames As ImmutableArray(Of String), argumentRefKinds As ImmutableArray(Of RefKind), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operation = operation
            _arguments = arguments
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_operation)
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_arguments)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyUnaryOperation
        Inherits LazyUnaryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operator As BoundExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, [operator] As BoundExpression, unaryOperationKind As UnaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operator = [operator]
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
            Return _operationFactory.CreateBoundUnaryOperatorChild(_operator)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyUsingOperation
        Inherits LazyUsingOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundUsingStatement As BoundUsingStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundUsingStatement As BoundUsingStatement, locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _boundUsingStatement = boundUsingStatement
        End Sub

        Protected Overrides Function CreateResources() As IOperation
            Return _operationFactory.CreateBoundUsingStatementResources(_boundUsingStatement)
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_boundUsingStatement.Body)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableDeclarationGroupOperation
        Inherits LazyVariableDeclarationGroupOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _declarations As ImmutableArray(Of BoundLocalDeclarationBase)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, declarations As ImmutableArray(Of BoundLocalDeclarationBase), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _declarations = declarations
        End Sub

        Protected Overrides Function CreateDeclarations() As ImmutableArray(Of IVariableDeclarationOperation)
            Return _operationFactory.GetVariableDeclarationStatementVariables(_declarations)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyWhileLoopOperation
        Inherits LazyWhileLoopOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _condition As BoundNode
        Private ReadOnly _body As BoundNode
        Private ReadOnly _ignoredCondition As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, condition As BoundNode, body As BoundNode, ignoredCondition As BoundNode, locals As ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, conditionIsTop As Boolean, conditionIsUntil As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _condition = condition
            _body = body
            _ignoredCondition = ignoredCondition
        End Sub

        Protected Overrides Function CreateCondition() As IOperation
            Return _operationFactory.Create(_condition)
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_body)
        End Function

        Protected Overrides Function CreateIgnoredCondition() As IOperation
            Return _operationFactory.Create(_ignoredCondition)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyWithOperation
        Inherits LazyWithOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _body As BoundNode
        Private ReadOnly _value As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, body As BoundNode, value As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _body = body
            _value = value
        End Sub

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_body)
        End Function

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_value)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyLocalFunctionOperation
        Inherits LazyLocalFunctionOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _body As BoundNode
        Private ReadOnly _ignoredBody As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, body As BoundNode, ignoredBody As BoundNode, symbol As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(symbol, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _body = body
            _ignoredBody = ignoredBody
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_body), IBlockOperation)
        End Function

        Protected Overrides Function CreateIgnoredBody() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_ignoredBody), IBlockOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyObjectOrCollectionInitializerOperation
        Inherits LazyObjectOrCollectionInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _initializers As ImmutableArray(Of BoundExpression)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, initializers As ImmutableArray(Of BoundExpression), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _initializers = initializers
        End Sub

        Protected Overrides Function CreateInitializers() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_initializers)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTranslatedQueryOperation
        Inherits LazyTranslatedQueryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operation As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operation As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operation = operation
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_operation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAggregateQueryOperation
        Inherits LazyAggregateQueryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _group As BoundNode
        Private ReadOnly _aggregation As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, group As BoundNode, aggregation As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _group = group
            _aggregation = aggregation
        End Sub

        Protected Overrides Function CreateGroup() As IOperation
            Return _operationFactory.Create(_group)
        End Function

        Protected Overrides Function CreateAggregation() As IOperation
            Return _operationFactory.Create(_aggregation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyNoPiaObjectCreationOperation
        Inherits LazyNoPiaObjectCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _initializer As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, initializer As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _initializer = initializer
        End Sub

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
            Return DirectCast(_operationFactory.Create(_initializer), IObjectOrCollectionInitializerOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReDimOperation
        Inherits LazyReDimOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _clauses As ImmutableArray(Of BoundRedimClause)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, clauses As ImmutableArray(Of BoundRedimClause), preserve As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(preserve, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _clauses = clauses
        End Sub

        Protected Overrides Function CreateClauses() As ImmutableArray(Of IReDimClauseOperation)
            Return _operationFactory.CreateFromArray(Of BoundRedimClause, IReDimClauseOperation)(_clauses)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReDimClauseOperation
        Inherits LazyReDimClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _operand As BoundNode
        Private ReadOnly _dimensionSizes As ImmutableArray(Of BoundExpression)

        Friend Sub New(operationFactory As VisualBasicOperationFactory, operand As BoundNode, dimensionSizes As ImmutableArray(Of BoundExpression), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _operand = operand
            _dimensionSizes = dimensionSizes
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
            Return _operationFactory.Create(_operand)
        End Function

        Protected Overrides Function CreateDimensionSizes() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_dimensionSizes)
        End Function
    End Class
End Namespace
