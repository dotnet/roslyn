' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Operations
    Friend NotInheritable Class VisualBasicLazyAddressOfOperation
        Inherits LazyAddressOfOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateReference() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyNameOfOperation
        Inherits LazyNameOfOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateArgument() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyThrowOperation
        Inherits LazyThrowOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateException() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArgumentOperation
        Inherits LazyArgumentOperation

        Friend Sub New(argumentKind As ArgumentKind, inConversionOpt As IConvertibleConversion, outConversionOpt As IConvertibleConversion, parameter As IParameterSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, isImplicit As Boolean)
            MyBase.New(argumentKind, inConversionOpt, outConversionOpt, parameter, semanticModel, syntax, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayCreationOperation
        Inherits LazyArrayCreationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateDimensionSizes() As Immutable.ImmutableArray(Of IOperation)
        End Function

        Protected Overrides Function CreateInitializer() As IArrayInitializerOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayElementReferenceOperation
        Inherits LazyArrayElementReferenceOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateArrayReference() As IOperation
        End Function

        Protected Overrides Function CreateIndices() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyArrayInitializerOperation
        Inherits LazyArrayInitializerOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateElementValues() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySimpleAssignmentOperation
        Inherits LazySimpleAssignmentOperation

        Friend Sub New(isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
        End Function

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDeconstructionAssignmentOperation
        Inherits LazyDeconstructionAssignmentOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
        End Function

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDeclarationExpressionOperation
        Inherits LazyDeclarationExpressionOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateExpression() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAwaitOperation
        Inherits LazyAwaitOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyBinaryOperation
        Inherits LazyBinaryOperation

        Friend Sub New(operatorKind As BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, isCompareText As Boolean, operatorMethod As IMethodSymbol, unaryOperatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateLeftOperand() As IOperation
        End Function

        Protected Overrides Function CreateRightOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTupleBinaryOperation
        Inherits LazyTupleBinaryOperation

        Friend Sub New(operatorKind As BinaryOperatorKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(operatorKind, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateLeftOperand() As IOperation
        End Function

        Protected Overrides Function CreateRightOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyBlockOperation
        Inherits LazyBlockOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperations() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCatchClauseOperation
        Inherits LazyCatchClauseOperation

        Friend Sub New(exceptionType As ITypeSymbol, locals As Immutable.ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateExceptionDeclarationOrExpression() As IOperation
        End Function

        Protected Overrides Function CreateFilter() As IOperation
        End Function

        Protected Overrides Function CreateHandler() As IBlockOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCompoundAssignmentOperation
        Inherits LazyCompoundAssignmentOperation

        Friend Sub New(inConversionConvertible As IConvertibleConversion, outConversionConvertible As IConvertibleConversion, operatorKind As BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(inConversionConvertible, outConversionConvertible, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
        End Function

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConditionalAccessOperation
        Inherits LazyConditionalAccessOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function

        Protected Overrides Function CreateWhenNotNull() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConditionalOperation
        Inherits LazyConditionalOperation

        Friend Sub New(isRef As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateCondition() As IOperation
        End Function

        Protected Overrides Function CreateWhenTrue() As IOperation
        End Function

        Protected Overrides Function CreateWhenFalse() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConversionOperation
        Inherits LazyConversionOperation

        Friend Sub New(convertibleConversion As IConvertibleConversion, isTryCast As Boolean, isChecked As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(convertibleConversion, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyEventAssignmentOperation
        Inherits LazyEventAssignmentOperation

        Friend Sub New(adds As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(adds, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateEventReference() As IOperation
        End Function

        Protected Overrides Function CreateHandlerValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyEventReferenceOperation
        Inherits LazyEventReferenceOperation

        Friend Sub New([event] As IEventSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New([event], semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyExpressionStatementOperation
        Inherits LazyExpressionStatementOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableInitializerOperation
        Inherits LazyVariableInitializerOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyFieldInitializerOperation
        Inherits LazyFieldInitializerOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), initializedFields As Immutable.ImmutableArray(Of IFieldSymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyFieldReferenceOperation
        Inherits LazyFieldReferenceOperation

        Friend Sub New(field As IFieldSymbol, isDeclaration As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyFixedOperation
        Inherits LazyFixedOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateVariables() As IVariableDeclarationGroupOperation
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyForEachLoopOperation
        Inherits LazyForEachLoopOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, info As ForEachLoopOperationInfo, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, continueLabel, exitLabel, info, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateLoopControlVariable() As IOperation
        End Function

        Protected Overrides Function CreateCollection() As IOperation
        End Function

        Protected Overrides Function CreateNextVariables() As Immutable.ImmutableArray(Of IOperation)
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyForLoopOperation
        Inherits LazyForLoopOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), conditionLocals As Immutable.ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, conditionLocals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateBefore() As Immutable.ImmutableArray(Of IOperation)
        End Function

        Protected Overrides Function CreateCondition() As IOperation
        End Function

        Protected Overrides Function CreateAtLoopBottom() As Immutable.ImmutableArray(Of IOperation)
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyForToLoopOperation
        Inherits LazyForToLoopOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), isChecked As Boolean, info As (LoopObject As ILocalSymbol, UserDefinedInfo As ForToLoopOperationUserDefinedInfo), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, isChecked, info, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateLoopControlVariable() As IOperation
        End Function

        Protected Overrides Function CreateInitialValue() As IOperation
        End Function

        Protected Overrides Function CreateLimitValue() As IOperation
        End Function

        Protected Overrides Function CreateStepValue() As IOperation
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function

        Protected Overrides Function CreateNextVariables() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyIncrementOrDecrementOperation
        Inherits LazyIncrementOrDecrementOperation

        Friend Sub New(isDecrement As Boolean, isPostfix As Boolean, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isDecrement, isPostfix, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolatedStringOperation
        Inherits LazyInterpolatedStringOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateParts() As Immutable.ImmutableArray(Of IInterpolatedStringContentOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolatedStringTextOperation
        Inherits LazyInterpolatedStringTextOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateText() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInterpolationOperation
        Inherits LazyInterpolationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateExpression() As IOperation
        End Function

        Protected Overrides Function CreateAlignment() As IOperation
        End Function

        Protected Overrides Function CreateFormatString() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInvalidOperation
        Inherits LazyInvalidOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateChildren() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyInvocationOperation
        Inherits LazyInvocationOperation

        Friend Sub New(targetMethod As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
        End Function

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IArgumentOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRaiseEventOperation
        Inherits LazyRaiseEventOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateEventReference() As IEventReferenceOperation
        End Function

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IArgumentOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyIsTypeOperation
        Inherits LazyIsTypeOperation

        Friend Sub New(isType As ITypeSymbol, isNotTypeExpression As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isType, isNotTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValueOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyLabeledOperation
        Inherits LazyLabeledOperation

        Friend Sub New(label As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(label, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAnonymousFunctionOperation
        Inherits LazyAnonymousFunctionOperation

        Friend Sub New(symbol As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDelegateCreationOperation
        Inherits LazyDelegateCreationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicMemberReferenceOperation
        Inherits LazyDynamicMemberReferenceOperation

        Friend Sub New(memberName As String, typeArguments As Immutable.ImmutableArray(Of ITypeSymbol), containingType As ITypeSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyLockOperation
        Inherits LazyLockOperation

        Friend Sub New(lockTakenSymbol As ILocalSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateLockedValue() As IOperation
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyMethodReferenceOperation
        Inherits LazyMethodReferenceOperation

        Friend Sub New(method As IMethodSymbol, isVirtual As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCoalesceOperation
        Inherits LazyCoalesceOperation

        Friend Sub New(convertibleValueConversion As IConvertibleConversion, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function

        Protected Overrides Function CreateWhenNull() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyCoalesceAssignmentOperation
        Inherits LazyCoalesceAssignmentOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateTarget() As IOperation
        End Function

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyObjectCreationOperation
        Inherits LazyObjectCreationOperation

        Friend Sub New(constructor As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
        End Function

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IArgumentOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAnonymousObjectCreationOperation
        Inherits LazyAnonymousObjectCreationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializers() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyParameterInitializerOperation
        Inherits LazyParameterInitializerOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), parameter As IParameterSymbol, kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyParenthesizedOperation
        Inherits LazyParenthesizedOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyPropertyInitializerOperation
        Inherits LazyPropertyInitializerOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), initializedProperties As Immutable.ImmutableArray(Of IPropertySymbol), kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, initializedProperties, kind, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyPropertyReferenceOperation
        Inherits LazyPropertyReferenceOperation

        Friend Sub New([property] As IPropertySymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New([property], semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInstance() As IOperation
        End Function

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IArgumentOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRangeCaseClauseOperation
        Inherits LazyRangeCaseClauseOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateMinimumValue() As IOperation
        End Function

        Protected Overrides Function CreateMaximumValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRelationalCaseClauseOperation
        Inherits LazyRelationalCaseClauseOperation

        Friend Sub New(relation As BinaryOperatorKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(relation, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReturnOperation
        Inherits LazyReturnOperation

        Friend Sub New(kind As OperationKind, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(kind, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateReturnedValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySingleValueCaseClauseOperation
        Inherits LazySingleValueCaseClauseOperation

        Friend Sub New(label As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(label, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySwitchCaseOperation
        Inherits LazySwitchCaseOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateClauses() As Immutable.ImmutableArray(Of ICaseClauseOperation)
        End Function

        Protected Overrides Function CreateCondition() As IOperation
        End Function

        Protected Overrides Function CreateBody() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazySwitchOperation
        Inherits LazySwitchOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function

        Protected Overrides Function CreateCases() As Immutable.ImmutableArray(Of ISwitchCaseOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTryOperation
        Inherits LazyTryOperation

        Friend Sub New(exitLabel As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
        End Function

        Protected Overrides Function CreateCatches() As Immutable.ImmutableArray(Of ICatchClauseOperation)
        End Function

        Protected Overrides Function CreateFinally() As IBlockOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTupleOperation
        Inherits LazyTupleOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, naturalType As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, naturalType, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateElements() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTypeParameterObjectCreationOperation
        Inherits LazyTypeParameterObjectCreationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicObjectCreationOperation
        Inherits LazyDynamicObjectCreationOperation

        Friend Sub New(argumentNames As Immutable.ImmutableArray(Of String), argumentRefKinds As Immutable.ImmutableArray(Of RefKind), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IOperation)
        End Function

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicInvocationOperation
        Inherits LazyDynamicInvocationOperation

        Friend Sub New(argumentNames As Immutable.ImmutableArray(Of String), argumentRefKinds As Immutable.ImmutableArray(Of RefKind), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyDynamicIndexerAccessOperation
        Inherits LazyDynamicIndexerAccessOperation

        Friend Sub New(argumentNames As Immutable.ImmutableArray(Of String), argumentRefKinds As Immutable.ImmutableArray(Of RefKind), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function

        Protected Overrides Function CreateArguments() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyUnaryOperation
        Inherits LazyUnaryOperation

        Friend Sub New(unaryOperationKind As UnaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyUsingOperation
        Inherits LazyUsingOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateResources() As IOperation
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableDeclaratorOperation
        Inherits LazyVariableDeclaratorOperation

        Friend Sub New(symbol As ILocalSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializer() As IVariableInitializerOperation
        End Function

        Protected Overrides Function CreateIgnoredArguments() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableDeclarationOperation
        Inherits LazyVariableDeclarationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateDeclarators() As Immutable.ImmutableArray(Of IVariableDeclaratorOperation)
        End Function

        Protected Overrides Function CreateInitializer() As IVariableInitializerOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyVariableDeclarationGroupOperation
        Inherits LazyVariableDeclarationGroupOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateDeclarations() As Immutable.ImmutableArray(Of IVariableDeclarationOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyWhileLoopOperation
        Inherits LazyWhileLoopOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), continueLabel As ILabelSymbol, exitLabel As ILabelSymbol, conditionIsTop As Boolean, conditionIsUntil As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateCondition() As IOperation
        End Function

        Protected Overrides Function CreateBody() As IOperation
        End Function

        Protected Overrides Function CreateIgnoredCondition() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyWithOperation
        Inherits LazyWithOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateBody() As IOperation
        End Function

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyLocalFunctionOperation
        Inherits LazyLocalFunctionOperation

        Friend Sub New(symbol As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateBody() As IBlockOperation
        End Function

        Protected Overrides Function CreateIgnoredBody() As IBlockOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConstantPatternOperation
        Inherits LazyConstantPatternOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyPatternCaseClauseOperation
        Inherits LazyPatternCaseClauseOperation

        Friend Sub New(label As ILabelSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(label, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreatePattern() As IPatternOperation
        End Function

        Protected Overrides Function CreateGuard() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyIsPatternOperation
        Inherits LazyIsPatternOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateValue() As IOperation
        End Function

        Protected Overrides Function CreatePattern() As IPatternOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyObjectOrCollectionInitializerOperation
        Inherits LazyObjectOrCollectionInitializerOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializers() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyMemberInitializerOperation
        Inherits LazyMemberInitializerOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializedMember() As IOperation
        End Function

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyTranslatedQueryOperation
        Inherits LazyTranslatedQueryOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperation() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyMethodBodyOperation
        Inherits LazyMethodBodyOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode)
            MyBase.New(semanticModel, syntax)
        End Sub

        Protected Overrides Function CreateBlockBody() As IBlockOperation
        End Function

        Protected Overrides Function CreateExpressionBody() As IBlockOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyConstructorBodyOperation
        Inherits LazyConstructorBodyOperation

        Friend Sub New(locals As Immutable.ImmutableArray(Of ILocalSymbol), semanticModel As SemanticModel, syntax As SyntaxNode)
            MyBase.New(locals, semanticModel, syntax)
        End Sub

        Protected Overrides Function CreateInitializer() As IOperation
        End Function

        Protected Overrides Function CreateBlockBody() As IBlockOperation
        End Function

        Protected Overrides Function CreateExpressionBody() As IBlockOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyAggregateQueryOperation
        Inherits LazyAggregateQueryOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateGroup() As IOperation
        End Function

        Protected Overrides Function CreateAggregation() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyNoPiaObjectCreationOperation
        Inherits LazyNoPiaObjectCreationOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateInitializer() As IObjectOrCollectionInitializerOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyFromEndIndexOperation
        Inherits LazyFromEndIndexOperation

        Friend Sub New(isLifted As Boolean, isImplicit As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, symbol As IMethodSymbol)
            MyBase.New(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyRangeOperation
        Inherits LazyRangeOperation

        Friend Sub New(isLifted As Boolean, isImplicit As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, symbol As IMethodSymbol)
            MyBase.New(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        End Sub

        Protected Overrides Function CreateLeftOperand() As IOperation
        End Function

        Protected Overrides Function CreateRightOperand() As IOperation
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReDimOperation
        Inherits LazyReDimOperation

        Friend Sub New(preserve As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(preserve, semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateClauses() As Immutable.ImmutableArray(Of IReDimClauseOperation)
        End Function
    End Class

    Friend NotInheritable Class VisualBasicLazyReDimClauseOperation
        Inherits LazyReDimClauseOperation

        Friend Sub New(semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(semanticModel, syntax, type, constantValue, isImplicit)
        End Sub

        Protected Overrides Function CreateOperand() As IOperation
        End Function

        Protected Overrides Function CreateDimensionSizes() As Immutable.ImmutableArray(Of IOperation)
        End Function
    End Class
End Namespace
