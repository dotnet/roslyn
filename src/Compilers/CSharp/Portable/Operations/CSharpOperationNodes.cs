// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Operations
{
    internal sealed class CSharpLazyAddressOfOperation : LazyAddressOfOperation
    {
        internal CSharpLazyAddressOfOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateReference()
        {
        }
    }

    internal sealed class CSharpLazyNameOfOperation : LazyNameOfOperation
    {
        internal CSharpLazyNameOfOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateArgument()
        {
        }
    }

    internal sealed class CSharpLazyThrowOperation : LazyThrowOperation
    {
        internal CSharpLazyThrowOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateException()
        {
        }
    }

    internal sealed class CSharpLazyArgumentOperation : LazyArgumentOperation
    {
        internal CSharpLazyArgumentOperation(ArgumentKind argumentKind, IConvertibleConversion inConversionOpt, IConvertibleConversion outConversionOpt, IParameterSymbol parameter, SemanticModel semanticModel, SyntaxNode syntax, bool isImplicit) : base(argumentKind, inConversionOpt, outConversionOpt, parameter, semanticModel, syntax, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyArrayCreationOperation : LazyArrayCreationOperation
    {
        internal CSharpLazyArrayCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateDimensionSizes()
        {
        }

        protected override IArrayInitializerOperation CreateInitializer()
        {
        }
    }

    internal sealed class CSharpLazyArrayElementReferenceOperation : LazyArrayElementReferenceOperation
    {
        internal CSharpLazyArrayElementReferenceOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateArrayReference()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateIndices()
        {
        }
    }

    internal sealed class CSharpLazyArrayInitializerOperation : LazyArrayInitializerOperation
    {
        internal CSharpLazyArrayInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateElementValues()
        {
        }
    }

    internal sealed class CSharpLazySimpleAssignmentOperation : LazySimpleAssignmentOperation
    {
        internal CSharpLazySimpleAssignmentOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateTarget()
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyDeconstructionAssignmentOperation : LazyDeconstructionAssignmentOperation
    {
        internal CSharpLazyDeconstructionAssignmentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateTarget()
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyDeclarationExpressionOperation : LazyDeclarationExpressionOperation
    {
        internal CSharpLazyDeclarationExpressionOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateExpression()
        {
        }
    }

    internal sealed class CSharpLazyAwaitOperation : LazyAwaitOperation
    {
        internal CSharpLazyAwaitOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }
    }

    internal sealed class CSharpLazyBinaryOperation : LazyBinaryOperation
    {
        internal CSharpLazyBinaryOperation(BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, bool isCompareText, IMethodSymbol operatorMethod, IMethodSymbol unaryOperatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, isLifted, isChecked, isCompareText, operatorMethod, unaryOperatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateLeftOperand()
        {
        }

        protected override IOperation CreateRightOperand()
        {
        }
    }

    internal sealed class CSharpLazyTupleBinaryOperation : LazyTupleBinaryOperation
    {
        internal CSharpLazyTupleBinaryOperation(BinaryOperatorKind operatorKind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(operatorKind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateLeftOperand()
        {
        }

        protected override IOperation CreateRightOperand()
        {
        }
    }

    internal sealed class CSharpLazyBlockOperation : LazyBlockOperation
    {
        internal CSharpLazyBlockOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateOperations()
        {
        }
    }

    internal sealed class CSharpLazyCatchClauseOperation : LazyCatchClauseOperation
    {
        internal CSharpLazyCatchClauseOperation(ITypeSymbol exceptionType, ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(exceptionType, locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateExceptionDeclarationOrExpression()
        {
        }

        protected override IOperation CreateFilter()
        {
        }

        protected override IBlockOperation CreateHandler()
        {
        }
    }

    internal sealed class CSharpLazyCompoundAssignmentOperation : LazyCompoundAssignmentOperation
    {
        internal CSharpLazyCompoundAssignmentOperation(IConvertibleConversion inConversionConvertible, IConvertibleConversion outConversionConvertible, BinaryOperatorKind operatorKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(inConversionConvertible, outConversionConvertible, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateTarget()
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyConditionalAccessOperation : LazyConditionalAccessOperation
    {
        internal CSharpLazyConditionalAccessOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }

        protected override IOperation CreateWhenNotNull()
        {
        }
    }

    internal sealed class CSharpLazyConditionalOperation : LazyConditionalOperation
    {
        internal CSharpLazyConditionalOperation(bool isRef, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isRef, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateCondition()
        {
        }

        protected override IOperation CreateWhenTrue()
        {
        }

        protected override IOperation CreateWhenFalse()
        {
        }
    }

    internal sealed class CSharpLazyConversionOperation : LazyConversionOperation
    {
        internal CSharpLazyConversionOperation(IConvertibleConversion convertibleConversion, bool isTryCast, bool isChecked, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(convertibleConversion, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperand()
        {
        }
    }

    internal sealed class CSharpLazyEventAssignmentOperation : LazyEventAssignmentOperation
    {
        internal CSharpLazyEventAssignmentOperation(bool adds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(adds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateEventReference()
        {
        }

        protected override IOperation CreateHandlerValue()
        {
        }
    }

    internal sealed class CSharpLazyEventReferenceOperation : LazyEventReferenceOperation
    {
        internal CSharpLazyEventReferenceOperation(IEventSymbol @event, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(@event, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInstance()
        {
        }
    }

    internal sealed class CSharpLazyExpressionStatementOperation : LazyExpressionStatementOperation
    {
        internal CSharpLazyExpressionStatementOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }
    }

    internal sealed class CSharpLazyVariableInitializerOperation : LazyVariableInitializerOperation
    {
        internal CSharpLazyVariableInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyFieldInitializerOperation : LazyFieldInitializerOperation
    {
        internal CSharpLazyFieldInitializerOperation(ImmutableArray<ILocalSymbol> locals, ImmutableArray<IFieldSymbol> initializedFields, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, initializedFields, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyFieldReferenceOperation : LazyFieldReferenceOperation
    {
        internal CSharpLazyFieldReferenceOperation(IFieldSymbol field, bool isDeclaration, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(field, isDeclaration, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInstance()
        {
        }
    }

    internal sealed class CSharpLazyFixedOperation : LazyFixedOperation
    {
        internal CSharpLazyFixedOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IVariableDeclarationGroupOperation CreateVariables()
        {
        }

        protected override IOperation CreateBody()
        {
        }
    }

    internal sealed class CSharpLazyForEachLoopOperation : LazyForEachLoopOperation
    {
        internal CSharpLazyForEachLoopOperation(ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, ForEachLoopOperationInfo info, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, continueLabel, exitLabel, info, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateLoopControlVariable()
        {
        }

        protected override IOperation CreateCollection()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateNextVariables()
        {
        }

        protected override IOperation CreateBody()
        {
        }
    }

    internal sealed class CSharpLazyForLoopOperation : LazyForLoopOperation
    {
        internal CSharpLazyForLoopOperation(ImmutableArray<ILocalSymbol> locals, ImmutableArray<ILocalSymbol> conditionLocals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, conditionLocals, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateBefore()
        {
        }

        protected override IOperation CreateCondition()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateAtLoopBottom()
        {
        }

        protected override IOperation CreateBody()
        {
        }
    }

    internal sealed class CSharpLazyForToLoopOperation : LazyForToLoopOperation
    {
        internal CSharpLazyForToLoopOperation(ImmutableArray<ILocalSymbol> locals, bool isChecked, (ILocalSymbol LoopObject, ForToLoopOperationUserDefinedInfo UserDefinedInfo) info, ILabelSymbol continueLabel, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, isChecked, info, continueLabel, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateLoopControlVariable()
        {
        }

        protected override IOperation CreateInitialValue()
        {
        }

        protected override IOperation CreateLimitValue()
        {
        }

        protected override IOperation CreateStepValue()
        {
        }

        protected override IOperation CreateBody()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateNextVariables()
        {
        }
    }

    internal sealed class CSharpLazyIncrementOrDecrementOperation : LazyIncrementOrDecrementOperation
    {
        internal CSharpLazyIncrementOrDecrementOperation(bool isDecrement, bool isPostfix, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isDecrement, isPostfix, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateTarget()
        {
        }
    }

    internal sealed class CSharpLazyInterpolatedStringOperation : LazyInterpolatedStringOperation
    {
        internal CSharpLazyInterpolatedStringOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IInterpolatedStringContentOperation> CreateParts()
        {
        }
    }

    internal sealed class CSharpLazyInterpolatedStringTextOperation : LazyInterpolatedStringTextOperation
    {
        internal CSharpLazyInterpolatedStringTextOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateText()
        {
        }
    }

    internal sealed class CSharpLazyInterpolationOperation : LazyInterpolationOperation
    {
        internal CSharpLazyInterpolationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateExpression()
        {
        }

        protected override IOperation CreateAlignment()
        {
        }

        protected override IOperation CreateFormatString()
        {
        }
    }

    internal sealed class CSharpLazyInvalidOperation : LazyInvalidOperation
    {
        internal CSharpLazyInvalidOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateChildren()
        {
        }
    }

    internal sealed class CSharpLazyInvocationOperation : LazyInvocationOperation
    {
        internal CSharpLazyInvocationOperation(IMethodSymbol targetMethod, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(targetMethod, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInstance()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IArgumentOperation> CreateArguments()
        {
        }
    }

    internal sealed class CSharpLazyRaiseEventOperation : LazyRaiseEventOperation
    {
        internal CSharpLazyRaiseEventOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IEventReferenceOperation CreateEventReference()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IArgumentOperation> CreateArguments()
        {
        }
    }

    internal sealed class CSharpLazyIsTypeOperation : LazyIsTypeOperation
    {
        internal CSharpLazyIsTypeOperation(ITypeSymbol isType, bool isNotTypeExpression, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(isType, isNotTypeExpression, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValueOperand()
        {
        }
    }

    internal sealed class CSharpLazyLabeledOperation : LazyLabeledOperation
    {
        internal CSharpLazyLabeledOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }
    }

    internal sealed class CSharpLazyAnonymousFunctionOperation : LazyAnonymousFunctionOperation
    {
        internal CSharpLazyAnonymousFunctionOperation(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IBlockOperation CreateBody()
        {
        }
    }

    internal sealed class CSharpLazyDelegateCreationOperation : LazyDelegateCreationOperation
    {
        internal CSharpLazyDelegateCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateTarget()
        {
        }
    }

    internal sealed class CSharpLazyDynamicMemberReferenceOperation : LazyDynamicMemberReferenceOperation
    {
        internal CSharpLazyDynamicMemberReferenceOperation(string memberName, ImmutableArray<ITypeSymbol> typeArguments, ITypeSymbol containingType, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(memberName, typeArguments, containingType, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInstance()
        {
        }
    }

    internal sealed class CSharpLazyLockOperation : LazyLockOperation
    {
        internal CSharpLazyLockOperation(ILocalSymbol lockTakenSymbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(lockTakenSymbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateLockedValue()
        {
        }

        protected override IOperation CreateBody()
        {
        }
    }

    internal sealed class CSharpLazyMethodReferenceOperation : LazyMethodReferenceOperation
    {
        internal CSharpLazyMethodReferenceOperation(IMethodSymbol method, bool isVirtual, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(method, isVirtual, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInstance()
        {
        }
    }

    internal sealed class CSharpLazyCoalesceOperation : LazyCoalesceOperation
    {
        internal CSharpLazyCoalesceOperation(IConvertibleConversion convertibleValueConversion, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(convertibleValueConversion, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }

        protected override IOperation CreateWhenNull()
        {
        }
    }

    internal sealed class CSharpLazyCoalesceAssignmentOperation : LazyCoalesceAssignmentOperation
    {
        internal CSharpLazyCoalesceAssignmentOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateTarget()
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyObjectCreationOperation : LazyObjectCreationOperation
    {
        internal CSharpLazyObjectCreationOperation(IMethodSymbol constructor, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(constructor, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IArgumentOperation> CreateArguments()
        {
        }
    }

    internal sealed class CSharpLazyAnonymousObjectCreationOperation : LazyAnonymousObjectCreationOperation
    {
        internal CSharpLazyAnonymousObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateInitializers()
        {
        }
    }

    internal sealed class CSharpLazyParameterInitializerOperation : LazyParameterInitializerOperation
    {
        internal CSharpLazyParameterInitializerOperation(ImmutableArray<ILocalSymbol> locals, IParameterSymbol parameter, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, parameter, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyParenthesizedOperation : LazyParenthesizedOperation
    {
        internal CSharpLazyParenthesizedOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperand()
        {
        }
    }

    internal sealed class CSharpLazyPropertyInitializerOperation : LazyPropertyInitializerOperation
    {
        internal CSharpLazyPropertyInitializerOperation(ImmutableArray<ILocalSymbol> locals, ImmutableArray<IPropertySymbol> initializedProperties, OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, initializedProperties, kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyPropertyReferenceOperation : LazyPropertyReferenceOperation
    {
        internal CSharpLazyPropertyReferenceOperation(IPropertySymbol property, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(property, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInstance()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IArgumentOperation> CreateArguments()
        {
        }
    }

    internal sealed class CSharpLazyRangeCaseClauseOperation : LazyRangeCaseClauseOperation
    {
        internal CSharpLazyRangeCaseClauseOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateMinimumValue()
        {
        }

        protected override IOperation CreateMaximumValue()
        {
        }
    }

    internal sealed class CSharpLazyRelationalCaseClauseOperation : LazyRelationalCaseClauseOperation
    {
        internal CSharpLazyRelationalCaseClauseOperation(BinaryOperatorKind relation, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(relation, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyReturnOperation : LazyReturnOperation
    {
        internal CSharpLazyReturnOperation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(kind, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateReturnedValue()
        {
        }
    }

    internal sealed class CSharpLazySingleValueCaseClauseOperation : LazySingleValueCaseClauseOperation
    {
        internal CSharpLazySingleValueCaseClauseOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazySwitchCaseOperation : LazySwitchCaseOperation
    {
        internal CSharpLazySwitchCaseOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<ICaseClauseOperation> CreateClauses()
        {
        }

        protected override IOperation CreateCondition()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateBody()
        {
        }
    }

    internal sealed class CSharpLazySwitchOperation : LazySwitchOperation
    {
        internal CSharpLazySwitchOperation(ImmutableArray<ILocalSymbol> locals, ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<ISwitchCaseOperation> CreateCases()
        {
        }
    }

    internal sealed class CSharpLazyTryOperation : LazyTryOperation
    {
        internal CSharpLazyTryOperation(ILabelSymbol exitLabel, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(exitLabel, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IBlockOperation CreateBody()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<ICatchClauseOperation> CreateCatches()
        {
        }

        protected override IBlockOperation CreateFinally()
        {
        }
    }

    internal sealed class CSharpLazyTupleOperation : LazyTupleOperation
    {
        internal CSharpLazyTupleOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, ITypeSymbol naturalType, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, naturalType, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateElements()
        {
        }
    }

    internal sealed class CSharpLazyTypeParameterObjectCreationOperation : LazyTypeParameterObjectCreationOperation
    {
        internal CSharpLazyTypeParameterObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
        }
    }

    internal sealed class CSharpLazyDynamicObjectCreationOperation : LazyDynamicObjectCreationOperation
    {
        internal CSharpLazyDynamicObjectCreationOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateArguments()
        {
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
        }
    }

    internal sealed class CSharpLazyDynamicInvocationOperation : LazyDynamicInvocationOperation
    {
        internal CSharpLazyDynamicInvocationOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateArguments()
        {
        }
    }

    internal sealed class CSharpLazyDynamicIndexerAccessOperation : LazyDynamicIndexerAccessOperation
    {
        internal CSharpLazyDynamicIndexerAccessOperation(ImmutableArray<string> argumentNames, ImmutableArray<RefKind> argumentRefKinds, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(argumentNames, argumentRefKinds, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateArguments()
        {
        }
    }

    internal sealed class CSharpLazyUnaryOperation : LazyUnaryOperation
    {
        internal CSharpLazyUnaryOperation(UnaryOperatorKind unaryOperationKind, bool isLifted, bool isChecked, IMethodSymbol operatorMethod, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(unaryOperationKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperand()
        {
        }
    }

    internal sealed class CSharpLazyUsingOperation : LazyUsingOperation
    {
        internal CSharpLazyUsingOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateResources()
        {
        }

        protected override IOperation CreateBody()
        {
        }
    }

    internal sealed class CSharpLazyVariableDeclaratorOperation : LazyVariableDeclaratorOperation
    {
        internal CSharpLazyVariableDeclaratorOperation(ILocalSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateIgnoredArguments()
        {
        }
    }

    internal sealed class CSharpLazyVariableDeclarationOperation : LazyVariableDeclarationOperation
    {
        internal CSharpLazyVariableDeclarationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IVariableDeclaratorOperation> CreateDeclarators()
        {
        }

        protected override IVariableInitializerOperation CreateInitializer()
        {
        }
    }

    internal sealed class CSharpLazyVariableDeclarationGroupOperation : LazyVariableDeclarationGroupOperation
    {
        internal CSharpLazyVariableDeclarationGroupOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IVariableDeclarationOperation> CreateDeclarations()
        {
        }
    }

    internal sealed class CSharpLazyWhileLoopOperation : LazyWhileLoopOperation
    {
        internal CSharpLazyWhileLoopOperation(ImmutableArray<ILocalSymbol> locals, ILabelSymbol continueLabel, ILabelSymbol exitLabel, bool conditionIsTop, bool conditionIsUntil, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(locals, continueLabel, exitLabel, conditionIsTop, conditionIsUntil, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateCondition()
        {
        }

        protected override IOperation CreateBody()
        {
        }

        protected override IOperation CreateIgnoredCondition()
        {
        }
    }

    internal sealed class CSharpLazyWithOperation : LazyWithOperation
    {
        internal CSharpLazyWithOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateBody()
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyLocalFunctionOperation : LazyLocalFunctionOperation
    {
        internal CSharpLazyLocalFunctionOperation(IMethodSymbol symbol, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(symbol, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IBlockOperation CreateBody()
        {
        }

        protected override IBlockOperation CreateIgnoredBody()
        {
        }
    }

    internal sealed class CSharpLazyConstantPatternOperation : LazyConstantPatternOperation
    {
        internal CSharpLazyConstantPatternOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }
    }

    internal sealed class CSharpLazyPatternCaseClauseOperation : LazyPatternCaseClauseOperation
    {
        internal CSharpLazyPatternCaseClauseOperation(ILabelSymbol label, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(label, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IPatternOperation CreatePattern()
        {
        }

        protected override IOperation CreateGuard()
        {
        }
    }

    internal sealed class CSharpLazyIsPatternOperation : LazyIsPatternOperation
    {
        internal CSharpLazyIsPatternOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateValue()
        {
        }

        protected override IPatternOperation CreatePattern()
        {
        }
    }

    internal sealed class CSharpLazyObjectOrCollectionInitializerOperation : LazyObjectOrCollectionInitializerOperation
    {
        internal CSharpLazyObjectOrCollectionInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateInitializers()
        {
        }
    }

    internal sealed class CSharpLazyMemberInitializerOperation : LazyMemberInitializerOperation
    {
        internal CSharpLazyMemberInitializerOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateInitializedMember()
        {
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
        }
    }

    internal sealed class CSharpLazyTranslatedQueryOperation : LazyTranslatedQueryOperation
    {
        internal CSharpLazyTranslatedQueryOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperation()
        {
        }
    }

    internal sealed class CSharpLazyMethodBodyOperation : LazyMethodBodyOperation
    {
        internal CSharpLazyMethodBodyOperation(SemanticModel semanticModel, SyntaxNode syntax) : base(semanticModel, syntax)
        {
        }

        protected override IBlockOperation CreateBlockBody()
        {
        }

        protected override IBlockOperation CreateExpressionBody()
        {
        }
    }

    internal sealed class CSharpLazyConstructorBodyOperation : LazyConstructorBodyOperation
    {
        internal CSharpLazyConstructorBodyOperation(ImmutableArray<ILocalSymbol> locals, SemanticModel semanticModel, SyntaxNode syntax) : base(locals, semanticModel, syntax)
        {
        }

        protected override IOperation CreateInitializer()
        {
        }

        protected override IBlockOperation CreateBlockBody()
        {
        }

        protected override IBlockOperation CreateExpressionBody()
        {
        }
    }

    internal sealed class CSharpLazyAggregateQueryOperation : LazyAggregateQueryOperation
    {
        internal CSharpLazyAggregateQueryOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateGroup()
        {
        }

        protected override IOperation CreateAggregation()
        {
        }
    }

    internal sealed class CSharpLazyNoPiaObjectCreationOperation : LazyNoPiaObjectCreationOperation
    {
        internal CSharpLazyNoPiaObjectCreationOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IObjectOrCollectionInitializerOperation CreateInitializer()
        {
        }
    }

    internal sealed class CSharpLazyFromEndIndexOperation : LazyFromEndIndexOperation
    {
        internal CSharpLazyFromEndIndexOperation(bool isLifted, bool isImplicit, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol) : base(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        {
        }

        protected override IOperation CreateOperand()
        {
        }
    }

    internal sealed class CSharpLazyRangeOperation : LazyRangeOperation
    {
        internal CSharpLazyRangeOperation(bool isLifted, bool isImplicit, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, IMethodSymbol symbol) : base(isLifted, isImplicit, semanticModel, syntax, type, symbol)
        {
        }

        protected override IOperation CreateLeftOperand()
        {
        }

        protected override IOperation CreateRightOperand()
        {
        }
    }

    internal sealed class CSharpLazyReDimOperation : LazyReDimOperation
    {
        internal CSharpLazyReDimOperation(bool preserve, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(preserve, semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IReDimClauseOperation> CreateClauses()
        {
        }
    }

    internal sealed class CSharpLazyReDimClauseOperation : LazyReDimClauseOperation
    {
        internal CSharpLazyReDimClauseOperation(SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit) : base(semanticModel, syntax, type, constantValue, isImplicit)
        {
        }

        protected override IOperation CreateOperand()
        {
        }

        protected override System.Collections.Immutable.ImmutableArray<IOperation> CreateDimensionSizes()
        {
        }
    }
}
