// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method.
    /// </summary>
    public abstract class OperationVisitor
    {
        public virtual void Visit(IOperation operation)
        {
            operation?.Accept(this);
        }

        public virtual void DefaultVisit(IOperation operation)
        {
            // no-op
        }

        internal virtual void VisitNoneOperation(IOperation operation)
        {
            // no-op
        }

        public virtual void VisitBlock(IBlockOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableDeclarator(IVariableDeclaratorOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableDeclaration(IVariableDeclarationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSwitch(ISwitchOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSwitchCase(ISwitchCaseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitRangeCaseClause(IRangeCaseClauseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitWhileLoop(IWhileLoopOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForLoop(IForLoopOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForToLoop(IForToLoopOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForEachLoop(IForEachLoopOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLabeled(ILabeledOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitBranch(IBranchOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEmpty(IEmptyOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitReturn(IReturnOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLock(ILockOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTry(ITryOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCatchClause(ICatchClauseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUsing(IUsingOperation operation)
        {
            DefaultVisit(operation);
        }

        // Make public after review: https://github.com/dotnet/roslyn/issues/21281
        internal virtual void VisitFixed(IFixedOperation operation)
        {
            // https://github.com/dotnet/roslyn/issues/21281
            //DefaultVisit(operation);
            VisitNoneOperation(operation);
        }

        internal virtual void VisitAggregateQuery(IAggregateQueryOperation operation)
        {
            VisitNoneOperation(operation);
        }

        public virtual void VisitExpressionStatement(IExpressionStatementOperation operation)
        {
            DefaultVisit(operation);
        }

        // https://github.com/dotnet/roslyn/issues/22005
        internal virtual void VisitWith(IWithOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitStop(IStopOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEnd(IEndOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInvocation(IInvocationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArgument(IArgumentOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitOmittedArgument(IOmittedArgumentOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArrayElementReference(IArrayElementReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        // API moved internal for V1
        // https://github.com/dotnet/roslyn/issues/21295
        internal virtual void VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLocalReference(ILocalReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParameterReference(IParameterReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInstanceReference(IInstanceReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFieldReference(IFieldReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitMethodReference(IMethodReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEventReference(IEventReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEventAssignment(IEventAssignmentOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConditionalAccess(IConditionalAccessOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation)
        {
            DefaultVisit(operation);
        }

        // https://github.com/dotnet/roslyn/issues/21294
        internal virtual void VisitPlaceholder(IPlaceholderOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUnaryOperator(IUnaryOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitBinaryOperator(IBinaryOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTupleBinaryOperator(ITupleBinaryOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConversion(IConversionOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConditional(IConditionalOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCoalesce(ICoalesceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIsType(IIsTypeOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSizeOf(ISizeOfOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTypeOf(ITypeOfOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAnonymousFunction(IAnonymousFunctionOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDelegateCreation(IDelegateCreationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLiteral(ILiteralOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAwait(IAwaitOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitNameOf(INameOfOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitThrow(IThrowOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAddressOf(IAddressOfOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitObjectCreation(IObjectCreationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicInvocation(IDynamicInvocationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitMemberInitializer(IMemberInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
        public virtual void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFieldInitializer(IFieldInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableInitializer(IVariableInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitPropertyInitializer(IPropertyInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParameterInitializer(IParameterInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArrayCreation(IArrayCreationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArrayInitializer(IArrayInitializerOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDeclarationExpression(IDeclarationExpressionOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParenthesized(IParenthesizedOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDefaultValue(IDefaultValueOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation)
        {
            DefaultVisit(operation);
        }

        internal virtual void VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation)
        {
            VisitNoneOperation(operation);
        }

        public virtual void VisitInvalid(IInvalidOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLocalFunction(ILocalFunctionOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInterpolatedString(IInterpolatedStringOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInterpolation(IInterpolationOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIsPattern(IIsPatternOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConstantPattern(IConstantPatternOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDeclarationPattern(IDeclarationPatternOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitPatternCaseClause(IPatternCaseClauseOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTuple(ITupleOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTranslatedQuery(ITranslatedQueryOperation operation)
        {
            DefaultVisit(operation);
        }
        
        public virtual void VisitRaiseEvent(IRaiseEventOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitMethodBodyOperation(IMethodBodyOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConstructorBodyOperation(IConstructorBodyOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDiscardOperation(IDiscardOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFlowCapture(IFlowCaptureOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIsNull(IIsNullOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCaughtException(ICaughtExceptionOperation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation)
        {
            DefaultVisit(operation);
        }
    }

    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method with an additional argument of the type specified by the
    /// <typeparamref name="TArgument"/> parameter and produces a value of the type specified by
    /// the <typeparamref name="TResult"/> parameter.
    /// </summary>
    /// <typeparam name="TArgument">
    /// The type of the additional argument passed to this visitor's Visit method.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of the return value of this visitor's Visit method.
    /// </typeparam>
    public abstract class OperationVisitor<TArgument, TResult>
    {
        public virtual TResult Visit(IOperation operation, TArgument argument)
        {
            return operation == null ? default(TResult) : operation.Accept(this, argument);
        }

        public virtual TResult DefaultVisit(IOperation operation, TArgument argument)
        {
            return default(TResult);
        }

        internal virtual TResult VisitNoneOperation(IOperation operation, TArgument argument)
        {
            return default(TResult);
        }

        public virtual TResult VisitBlock(IBlockOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableDeclarator(IVariableDeclaratorOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableDeclaration(IVariableDeclarationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSwitch(ISwitchOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSwitchCase(ISwitchCaseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitRelationalCaseClause(IRelationalCaseClauseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitRangeCaseClause(IRangeCaseClauseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDefaultCaseClause(IDefaultCaseClauseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitWhileLoop(IWhileLoopOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForLoop(IForLoopOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForToLoop(IForToLoopOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForEachLoop(IForEachLoopOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLabeled(ILabeledOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitBranch(IBranchOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEmpty(IEmptyOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitReturn(IReturnOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLock(ILockOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTry(ITryOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCatchClause(ICatchClauseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitUsing(IUsingOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // Make public after review: https://github.com/dotnet/roslyn/issues/21281
        internal virtual TResult VisitFixed(IFixedOperation operation, TArgument argument)
        {
            // https://github.com/dotnet/roslyn/issues/21281
            //return DefaultVisit(operation, argument);
            return VisitNoneOperation(operation, argument);
        }

        internal virtual TResult VisitAggregateQuery(IAggregateQueryOperation operation, TArgument argument)
        {
            return VisitNoneOperation(operation, argument);
        }

        public virtual TResult VisitExpressionStatement(IExpressionStatementOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // https://github.com/dotnet/roslyn/issues/22005
        internal virtual TResult VisitWith(IWithOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitStop(IStopOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEnd(IEndOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInvocation(IInvocationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArgument(IArgumentOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitOmittedArgument(IOmittedArgumentOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArrayElementReference(IArrayElementReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // API moved internal for V1
        // https://github.com/dotnet/roslyn/issues/21295
        internal virtual TResult VisitPointerIndirectionReference(IPointerIndirectionReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLocalReference(ILocalReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParameterReference(IParameterReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInstanceReference(IInstanceReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFieldReference(IFieldReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitMethodReference(IMethodReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitPropertyReference(IPropertyReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEventReference(IEventReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEventAssignment(IEventAssignmentOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConditionalAccess(IConditionalAccessOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // https://github.com/dotnet/roslyn/issues/21294
        internal virtual TResult VisitPlaceholder(IPlaceholderOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitUnaryOperator(IUnaryOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitBinaryOperator(IBinaryOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTupleBinaryOperator(ITupleBinaryOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConversion(IConversionOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConditional(IConditionalOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCoalesce(ICoalesceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIsType(IIsTypeOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSizeOf(ISizeOfOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTypeOf(ITypeOfOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAnonymousFunction(IAnonymousFunctionOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDelegateCreation(IDelegateCreationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLiteral(ILiteralOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAwait(IAwaitOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitNameOf(INameOfOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitThrow(IThrowOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAddressOf(IAddressOfOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitObjectCreation(IObjectCreationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicInvocation(IDynamicInvocationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitMemberInitializer(IMemberInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        [Obsolete("ICollectionElementInitializerOperation has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
        public virtual TResult VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFieldInitializer(IFieldInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableInitializer(IVariableInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitPropertyInitializer(IPropertyInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParameterInitializer(IParameterInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArrayCreation(IArrayCreationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArrayInitializer(IArrayInitializerOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSimpleAssignment(ISimpleAssignmentOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDeclarationExpression(IDeclarationExpressionOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCompoundAssignment(ICompoundAssignmentOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParenthesized(IParenthesizedOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDefaultValue(IDefaultValueOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        internal virtual TResult VisitNoPiaObjectCreation(INoPiaObjectCreationOperation operation, TArgument argument)
        {
            return VisitNoneOperation(operation, argument);
        }

        public virtual TResult VisitInvalid(IInvalidOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLocalFunction(ILocalFunctionOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInterpolatedString(IInterpolatedStringOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInterpolatedStringText(IInterpolatedStringTextOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInterpolation(IInterpolationOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIsPattern(IIsPatternOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConstantPattern(IConstantPatternOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDeclarationPattern(IDeclarationPatternOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitPatternCaseClause(IPatternCaseClauseOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTuple(ITupleOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTranslatedQuery(ITranslatedQueryOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }
        
        public virtual TResult VisitRaiseEvent(IRaiseEventOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitMethodBodyOperation(IMethodBodyOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConstructorBodyOperation(IConstructorBodyOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDiscardOperation(IDiscardOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFlowCapture(IFlowCaptureOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIsNull(IIsNullOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCaughtException(ICaughtExceptionOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }
    }
}
