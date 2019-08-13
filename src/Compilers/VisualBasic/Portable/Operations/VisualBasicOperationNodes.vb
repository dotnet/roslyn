' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Operations
    Friend NotInheritable Class VisualBasicLazyNoneOperation
        Inherits LazyNoneOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundNode As BoundNode

        Public Sub New(operationFactory As VisualBasicOperationFactory, boundNode As BoundNode, semanticModel As SemanticModel, node As SyntaxNode, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, node, constantValue, isImplicit)
            _operationFactory = operationFactory
            _boundNode = boundNode
        End Sub

        Protected Overrides Function GetChildren() As ImmutableArray(Of IOperation)
            Return _operationFactory.GetIOperationChildren(_boundNode)
        End Function
    End Class

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
        Private ReadOnly _arrayCreation As BoundArrayCreation

        Friend Sub New(operationFactory As VisualBasicOperationFactory, arrayCreation As BoundArrayCreation, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _arrayCreation = arrayCreation
        End Sub

        Protected Overrides Function CreateDimensionSizes() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_arrayCreation.Bounds)
        End Function

        Protected Overrides Function CreateInitializer() As IArrayInitializerOperation
            Return DirectCast(_operationFactory.Create(_arrayCreation.InitializerOpt), IArrayInitializerOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayElementReferenceOperation
        Inherits LazyArrayElementReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _arrayAccess As BoundArrayAccess

        Friend Sub New(operationFactory As VisualBasicOperationFactory, arrayAccess As BoundArrayAccess, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _arrayAccess = arrayAccess
        End Sub

        Protected Overrides Function CreateArrayReference() As IOperation
            Return _operationFactory.Create(_arrayAccess.Expression)
        End Function

        Protected Overrides Function CreateIndices() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_arrayAccess.Indices)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayInitializerOperation
        Inherits LazyArrayInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _arrayInitialization As BoundArrayInitialization

        Friend Sub New(operationFactory As VisualBasicOperationFactory, arrayInitialization As BoundArrayInitialization, semanticModel As SemanticModel, syntax As SyntaxNode, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type:=Nothing, constantValue, isImplicit)
            _operationFactory = operationFactory
            _arrayInitialization = arrayInitialization
        End Sub

        Protected Overrides Function CreateElementValues() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_arrayInitialization.Initializers)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySimpleAssignmentOperation
        Inherits LazySimpleAssignmentOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _assignment As BoundAssignmentOperator

        Friend Sub New(operationFactory As VisualBasicOperationFactory, assignment As BoundAssignmentOperator, isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isRef, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _assignment = assignment
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
            Return _operationFactory.Create(_assignment.Left)
        End Function

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_assignment.Right)
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
        Private ReadOnly _block As BoundBlock

        Friend Sub New(operationFactory As VisualBasicOperationFactory, block As BoundBlock, locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _block = block
        End Sub

        Protected Overrides Function CreateOperations() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundStatement, IOperation)(_block.Statements)
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
        Private ReadOnly _conditionalAccess As BoundConditionalAccess

        Friend Sub New(operationFactory As VisualBasicOperationFactory, conditionalAccess As BoundConditionalAccess, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _conditionalAccess = conditionalAccess
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
            Return _operationFactory.Create(_conditionalAccess.Receiver)
        End Function

        Protected Overrides Function CreateWhenNotNull() As IOperation
            Return _operationFactory.Create(_conditionalAccess.AccessExpression)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConditionalOperation
        Inherits LazyConditionalOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _conditional As IBoundConditional

        Friend Sub New(operationFactory As VisualBasicOperationFactory, conditional As IBoundConditional, isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isRef, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _conditional = conditional
        End Sub

        Protected Overrides Function CreateCondition() As IOperation
            Return _operationFactory.Create(_conditional.Condition)
        End Function

        Protected Overrides Function CreateWhenTrue() As IOperation
            Return _operationFactory.Create(_conditional.WhenTrue)
        End Function

        Protected Overrides Function CreateWhenFalse() As IOperation
            Return _operationFactory.Create(_conditional.WhenFalseOpt)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyEventAssignmentOperation
        Inherits LazyEventAssignmentOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _addRemoveHandlerStatement As BoundAddRemoveHandlerStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, addRemoveHandlerStatement As BoundAddRemoveHandlerStatement, adds As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(adds, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _addRemoveHandlerStatement = addRemoveHandlerStatement
        End Sub

        Protected Overrides Function CreateEventReference() As IOperation
            Return _operationFactory.Create(_addRemoveHandlerStatement.EventAccess)
        End Function

        Protected Overrides Function CreateHandlerValue() As IOperation
            Return _operationFactory.Create(_addRemoveHandlerStatement.Handler)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, locals As ImmutableArray(Of ILocalSymbol), initializedFields As ImmutableArray(Of IFieldSymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(initializedFields, locals, semanticModel, syntax, type, constantValue, isImplicit)
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
            MyBase.New(LoopKind.ForEach, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
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
            MyBase.New(isChecked, info, LoopKind.ForTo, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
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
        Private ReadOnly _interpolatedStringExpression As BoundInterpolatedStringExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, interpolatedStringExpression As BoundInterpolatedStringExpression, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _interpolatedStringExpression = interpolatedStringExpression
        End Sub

        Protected Overrides Function CreateParts() As ImmutableArray(Of IInterpolatedStringContentOperation)
            Return _operationFactory.CreateBoundInterpolatedStringContentOperation(_interpolatedStringExpression.Contents)
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
        Private ReadOnly _interpolation As BoundInterpolation

        Friend Sub New(operationFactory As VisualBasicOperationFactory, interpolation As BoundInterpolation, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, originalNode As IBoundInvalidNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _originalNode = originalNode
        End Sub

        Protected Overrides Function CreateChildren() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundNode, IOperation)(_originalNode.InvalidNodeChildren)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInvocationOperation
        Inherits LazyInvocationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _invocable As BoundExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, invocable As BoundCall, targetMethod As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            Me.New(operationFactory, DirectCast(invocable, BoundExpression), targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Friend Sub New(operationFactory As VisualBasicOperationFactory, invocable As BoundNullableIsTrueOperator, targetMethod As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            Me.New(operationFactory, DirectCast(invocable, BoundExpression), targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Private Sub New(operationFactory As VisualBasicOperationFactory, invocable As BoundExpression, targetMethod As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _invocable = invocable
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Dim receiver As BoundExpression
            Select Case _invocable.Kind
                Case BoundKind.Call
                    Dim [call] = DirectCast(_invocable, BoundCall)
                    receiver = If([call].ReceiverOpt, [call].MethodGroupOpt?.ReceiverOpt)
                Case BoundKind.NullableIsTrueOperator
                    receiver = DirectCast(_invocable, BoundNullableIsTrueOperator).Operand
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(_invocable.Kind)
            End Select
            Return _operationFactory.CreateReceiverOperation(receiver, TargetMethod)
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IArgumentOperation)
            If _invocable.Kind = BoundKind.NullableIsTrueOperator Then
                Return ImmutableArray(Of IArgumentOperation).Empty
            End If
            Return _operationFactory.DeriveArguments(_invocable)
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
        Private ReadOnly _lockStatement As BoundSyncLockStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, lockStatement As BoundSyncLockStatement, lockTakenSymbol As ILocalSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _lockStatement = lockStatement
        End Sub

        Protected Overrides Function CreateLockedValue() As IOperation
            Return _operationFactory.Create(_lockStatement.LockExpression)
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_lockStatement.Body)
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
        Private ReadOnly _conditionalExpression As BoundBinaryConditionalExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, conditionalExpression As BoundBinaryConditionalExpression, convertibleValueConversion As IConvertibleConversion, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _conditionalExpression = conditionalExpression
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_conditionalExpression.TestExpression)
        End Function

        Protected Overrides Function CreateWhenNull() As IOperation
            Return _operationFactory.Create(_conditionalExpression.ElseExpression)
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
            MyBase.New(parameter, locals, semanticModel, syntax, type, constantValue, isImplicit)
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
            MyBase.New(initializedProperties, locals, semanticModel, syntax, type, constantValue, isImplicit)
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
        Private ReadOnly _boundProperty As BoundPropertyAccess

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundProperty As BoundPropertyAccess, [property] As IPropertySymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New([property], semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _boundProperty = boundProperty
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(
                If(_boundProperty.ReceiverOpt, _boundProperty.PropertyGroupOpt?.ReceiverOpt),
                [Property])
        End Function

        Protected Overrides Function CreateArguments() As ImmutableArray(Of IArgumentOperation)
            Return _operationFactory.DeriveArguments(_boundProperty)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRangeCaseClauseOperation
        Inherits LazyRangeCaseClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _rangeCaseClause As BoundRangeCaseClause

        Friend Sub New(operationFactory As VisualBasicOperationFactory, rangeCaseClause As BoundRangeCaseClause, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, value As BoundNode, relation As BinaryOperatorKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(relation, CaseKind.Relational, label:=Nothing, semanticModel, syntax, type, constantValue, isImplicit)
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
            MyBase.New(CaseKind.SingleValue, label, semanticModel, syntax, type, constantValue, isImplicit)
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
        Private ReadOnly _selectStatement As BoundSelectStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, selectStatement As BoundSelectStatement, locals As ImmutableArray(Of ILocalSymbol), exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _selectStatement = selectStatement
        End Sub

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_selectStatement.ExpressionStatement.Expression)
        End Function

        Protected Overrides Function CreateCases() As ImmutableArray(Of ISwitchCaseOperation)
            Return _operationFactory.CreateFromArray(Of BoundCaseBlock, ISwitchCaseOperation)(_selectStatement.CaseBlocks)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTryOperation
        Inherits LazyTryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _tryStatement As BoundTryStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, tryStatement As BoundTryStatement, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _tryStatement = tryStatement
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_tryStatement.TryBlock), IBlockOperation)
        End Function

        Protected Overrides Function CreateCatches() As ImmutableArray(Of ICatchClauseOperation)
            Return _operationFactory.CreateFromArray(Of BoundCatchBlock, ICatchClauseOperation)(_tryStatement.CatchBlocks)
        End Function

        Protected Overrides Function CreateFinally() As IBlockOperation
            Return DirectCast(_operationFactory.Create(_tryStatement.FinallyBlockOpt), IBlockOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTupleOperation
        Inherits LazyTupleOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _tupleExpression As BoundTupleExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, tupleExpression As BoundTupleExpression, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, naturalType As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(naturalType, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _tupleExpression = tupleExpression
        End Sub

        Protected Overrides Function CreateElements() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_tupleExpression.Arguments)
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
        Private ReadOnly _lateInvocation As BoundLateInvocation

        Friend Sub New(operationFactory As VisualBasicOperationFactory, lateInvocation As BoundLateInvocation, argumentNames As ImmutableArray(Of String), argumentRefKinds As ImmutableArray(Of RefKind), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
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

        Public Overrides ReadOnly Property IsAsynchronous As Boolean
            Get
                Return False
            End Get
        End Property

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
        Private ReadOnly _localDeclarations As IBoundLocalDeclarations

        Friend Sub New(operationFactory As VisualBasicOperationFactory, localDeclarations As IBoundLocalDeclarations, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _localDeclarations = localDeclarations
        End Sub

        Protected Overrides Function CreateDeclarations() As ImmutableArray(Of IVariableDeclarationOperation)
            Return _operationFactory.GetVariableDeclarationStatementVariables(_localDeclarations.Declarations)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyWhileLoopOperation
        Inherits LazyWhileLoopOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _conditionalLoop As IBoundConditionalLoop

        Friend Sub New(operationFactory As VisualBasicOperationFactory, conditionalLoop As IBoundConditionalLoop, locals As ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, conditionIsTop As Boolean, conditionIsUntil As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(conditionIsTop, conditionIsUntil, LoopKind.While, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _conditionalLoop = conditionalLoop
        End Sub

        Protected Overrides Function CreateCondition() As IOperation
            Return _operationFactory.Create(_conditionalLoop.Condition)
        End Function

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_conditionalLoop.Body)
        End Function

        Protected Overrides Function CreateIgnoredCondition() As IOperation
            Return _operationFactory.Create(_conditionalLoop.IgnoredCondition)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyWithOperation
        Inherits LazyWithOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _withStatement As BoundWithStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, withStatement As BoundWithStatement, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _withStatement = withStatement
        End Sub

        Protected Overrides Function CreateBody() As IOperation
            Return _operationFactory.Create(_withStatement.Body)
        End Function

        Protected Overrides Function CreateValue() As IOperation
            Return _operationFactory.Create(_withStatement.OriginalExpression)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyObjectOrCollectionInitializerOperation
        Inherits LazyObjectOrCollectionInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _objectOrCollectionInitializer As BoundObjectInitializerExpressionBase

        Friend Sub New(operationFactory As VisualBasicOperationFactory, objectOrCollectionInitializer As BoundObjectInitializerExpressionBase, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _objectOrCollectionInitializer = objectOrCollectionInitializer
        End Sub

        Protected Overrides Function CreateInitializers() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_objectOrCollectionInitializer.Initializers)
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
        Private ReadOnly _aggregateClause As BoundAggregateClause

        Friend Sub New(operationFactory As VisualBasicOperationFactory, aggregateClause As BoundAggregateClause, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _aggregateClause = aggregateClause
        End Sub

        Protected Overrides Function CreateGroup() As IOperation
            Return _operationFactory.Create(_aggregateClause.CapturedGroupOpt)
        End Function

        Protected Overrides Function CreateAggregation() As IOperation
            Return _operationFactory.Create(_aggregateClause.UnderlyingExpression)
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
        Private ReadOnly _redimStatement As BoundRedimStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, redimStatement As BoundRedimStatement, preserve As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(preserve, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _redimStatement = redimStatement
        End Sub

        Protected Overrides Function CreateClauses() As ImmutableArray(Of IReDimClauseOperation)
            Return _operationFactory.CreateFromArray(Of BoundRedimClause, IReDimClauseOperation)(_redimStatement.Clauses)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReDimClauseOperation
        Inherits LazyReDimClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _redimClause As BoundRedimClause

        Friend Sub New(operationFactory As VisualBasicOperationFactory, redimClause As BoundRedimClause, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _redimClause = redimClause
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
            Return _operationFactory.Create(_redimClause.Operand)
        End Function

        Protected Overrides Function CreateDimensionSizes() As ImmutableArray(Of IOperation)
            Return _operationFactory.CreateFromArray(Of BoundExpression, IOperation)(_redimClause.Indices)
        End Function
    End Class
End Namespace
