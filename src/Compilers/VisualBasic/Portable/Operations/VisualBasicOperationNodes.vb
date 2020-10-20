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

    Friend NotInheritable Class VisualBasicLazyThrowOperation
        Inherits LazyThrowOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _exception As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, exception As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyArrayInitializerOperation
        Inherits LazyArrayInitializerOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _arrayInitialization As BoundArrayInitialization

        Friend Sub New(operationFactory As VisualBasicOperationFactory, arrayInitialization As BoundArrayInitialization, semanticModel As SemanticModel, syntax As SyntaxNode, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, assignment As BoundAssignmentOperator, isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyCatchClauseOperation
        Inherits LazyCatchClauseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundCatchBlock As BoundCatchBlock

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundCatchBlock As BoundCatchBlock, exceptionType As ITypeSymbol, locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, assignment As BoundAssignmentOperator, inConversionConvertible As IConvertibleConversion, outConversionConvertible As IConvertibleConversion, operatorKind As BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyEventReferenceOperation
        Inherits LazyEventReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, [event] As IEventSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New([event], semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, [Event])
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

    Friend NotInheritable Class VisualBasicLazyFieldReferenceOperation
        Inherits LazyFieldReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, field As IFieldSymbol, isDeclaration As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, forEachLoop As BoundForEachStatement, locals As ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(isAsynchronous:=False, LoopKind.ForEach, locals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundForToLoop As BoundForToStatement, locals As ImmutableArray(Of ILocalSymbol), isChecked As Boolean, info As (LoopObject As ILocalSymbol, UserDefinedInfo As ForToLoopOperationUserDefinedInfo), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyDelegateCreationOperation
        Inherits LazyDelegateCreationOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _delegateCreation As BoundDelegateCreationExpression

        Friend Sub New(operationFactory As VisualBasicOperationFactory, delegateCreation As BoundDelegateCreationExpression, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, memberName As String, typeArguments As ImmutableArray(Of ITypeSymbol), containingType As ITypeSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.Create(_instance)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyMethodReferenceOperation
        Inherits LazyMethodReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _instance As BoundNode

        Friend Sub New(operationFactory As VisualBasicOperationFactory, instance As BoundNode, method As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
            MyBase.New(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
            _operationFactory = operationFactory
            _instance = instance
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
            Return _operationFactory.CreateReceiverOperation(_instance, Method)
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

    Friend NotInheritable Class VisualBasicLazyPropertyReferenceOperation
        Inherits LazyPropertyReferenceOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _boundProperty As BoundPropertyAccess

        Friend Sub New(operationFactory As VisualBasicOperationFactory, boundProperty As BoundPropertyAccess, [property] As IPropertySymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazySwitchCaseOperation
        Inherits LazySwitchCaseOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _caseBlock As BoundCaseBlock

        Friend Sub New(operationFactory As VisualBasicOperationFactory, caseBlock As BoundCaseBlock, locals As ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyWhileLoopOperation
        Inherits LazyWhileLoopOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _conditionalLoop As IBoundConditionalLoop

        Friend Sub New(operationFactory As VisualBasicOperationFactory, conditionalLoop As IBoundConditionalLoop, locals As ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, conditionIsTop As Boolean, conditionIsUntil As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyWithStatementOperation
        Inherits LazyWithStatementOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _withStatement As BoundWithStatement

        Friend Sub New(operationFactory As VisualBasicOperationFactory, withStatement As BoundWithStatement, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

    Friend NotInheritable Class VisualBasicLazyAggregateQueryOperation
        Inherits LazyAggregateQueryOperation

        Private ReadOnly _operationFactory As VisualBasicOperationFactory
        Private ReadOnly _aggregateClause As BoundAggregateClause

        Friend Sub New(operationFactory As VisualBasicOperationFactory, aggregateClause As BoundAggregateClause, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, initializer As BoundNode, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, redimStatement As BoundRedimStatement, preserve As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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

        Friend Sub New(operationFactory As VisualBasicOperationFactory, redimClause As BoundRedimClause, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As ConstantValue, isImplicit As Boolean)
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
