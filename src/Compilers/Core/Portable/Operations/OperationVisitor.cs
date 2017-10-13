// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Semantics
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

        public virtual void VisitBlockStatement(IBlockStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableDeclaration(IVariableDeclaration operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSwitchStatement(ISwitchStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSwitchCase(ISwitchCase operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSingleValueCaseClause(ISingleValueCaseClause operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitRangeCaseClause(IRangeCaseClause operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDefaultCaseClause(IDefaultCaseClause operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIfStatement(IIfStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDoLoopStatement(IDoLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitWhileLoopStatement(IWhileLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForLoopStatement(IForLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForToLoopStatement(IForToLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLabeledStatement(ILabeledStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitBranchStatement(IBranchStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitYieldBreakStatement(IReturnStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEmptyStatement(IEmptyStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitReturnStatement(IReturnStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLockStatement(ILockStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTryStatement(ITryStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCatchClause(ICatchClause operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUsingStatement(IUsingStatement operation)
        {
            DefaultVisit(operation);
        }

        // Make public after review: https://github.com/dotnet/roslyn/issues/21281
        internal virtual void VisitFixedStatement(IFixedStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitExpressionStatement(IExpressionStatement operation)
        {
            DefaultVisit(operation);
        }

        // https://github.com/dotnet/roslyn/issues/22005
        internal virtual void VisitWithStatement(IWithStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitStopStatement(IStopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEndStatement(IEndStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInvocationExpression(IInvocationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArgument(IArgument operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitOmittedArgumentExpression(IOmittedArgumentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        // API moved internal for V1
        // https://github.com/dotnet/roslyn/issues/21295
        internal virtual void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInstanceReferenceExpression(IInstanceReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitMethodReferenceExpression(IMethodReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEventReferenceExpression(IEventReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEventAssignmentExpression(IEventAssignmentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConditionalAccessExpression(IConditionalAccessExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConditionalAccessInstanceExpression(IConditionalAccessInstanceExpression operation)
        {
            DefaultVisit(operation);
        }

        // https://github.com/dotnet/roslyn/issues/21294
        internal virtual void VisitPlaceholderExpression(IPlaceholderExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConversionExpression(IConversionExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConditionalExpression(IConditionalExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCoalesceExpression(ICoalesceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIsTypeExpression(IIsTypeExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSizeOfExpression(ISizeOfExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTypeOfExpression(ITypeOfExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAnonymousFunctionExpression(IAnonymousFunctionExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDelegateCreationExpression(IDelegateCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLiteralExpression(ILiteralExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAwaitExpression(IAwaitExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitNameOfExpression(INameOfExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitThrowExpression(IThrowExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitAnonymousObjectCreationExpression(IAnonymousObjectCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicObjectCreationExpression(IDynamicObjectCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicInvocationExpression(IDynamicInvocationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicIndexerAccessExpression(IDynamicIndexerAccessExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitObjectOrCollectionInitializerExpression(IObjectOrCollectionInitializerExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitMemberInitializerExpression(IMemberInitializerExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCollectionElementInitializerExpression(ICollectionElementInitializerExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFieldInitializer(IFieldInitializer operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitVariableInitializer(IVariableInitializer operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitPropertyInitializer(IPropertyInitializer operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParameterInitializer(IParameterInitializer operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArrayCreationExpression(IArrayCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitArrayInitializer(IArrayInitializer operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSimpleAssignmentExpression(ISimpleAssignmentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDeconstructionAssignmentExpression(IDeconstructionAssignmentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDeclarationExpression(IDeclarationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIncrementOrDecrementExpression(IIncrementOrDecrementExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDynamicMemberReferenceExpression(IDynamicMemberReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDefaultValueExpression(IDefaultValueExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTypeParameterObjectCreationExpression(ITypeParameterObjectCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInvalidStatement(IInvalidStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInvalidExpression(IInvalidExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLocalFunctionStatement(ILocalFunctionStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInterpolatedStringExpression(IInterpolatedStringExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInterpolatedStringText(IInterpolatedStringText operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInterpolation(IInterpolation operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIsPatternExpression(IIsPatternExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitConstantPattern(IConstantPattern operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDeclarationPattern(IDeclarationPattern operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDiscardPattern(IDiscardPattern operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitRecursivePattern(IRecursivePattern operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitPatternCaseClause(IPatternCaseClause operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTupleExpression(ITupleExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTranslatedQueryExpression(ITranslatedQueryExpression operation)
        {
            DefaultVisit(operation);
        }
        
        public virtual void VisitRaiseEventStatement(IRaiseEventStatement operation)
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

        public virtual TResult VisitBlockStatement(IBlockStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableDeclarationStatement(IVariableDeclarationStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableDeclaration(IVariableDeclaration operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSwitchStatement(ISwitchStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSwitchCase(ISwitchCase operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSingleValueCaseClause(ISingleValueCaseClause operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitRelationalCaseClause(IRelationalCaseClause operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitRangeCaseClause(IRangeCaseClause operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDefaultCaseClause(IDefaultCaseClause operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIfStatement(IIfStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDoLoopStatement(IDoLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitWhileLoopStatement(IWhileLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForLoopStatement(IForLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForToLoopStatement(IForToLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForEachLoopStatement(IForEachLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLabeledStatement(ILabeledStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitBranchStatement(IBranchStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitYieldBreakStatement(IReturnStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEmptyStatement(IEmptyStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitReturnStatement(IReturnStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLockStatement(ILockStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTryStatement(ITryStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCatchClause(ICatchClause operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitUsingStatement(IUsingStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // Make public after review: https://github.com/dotnet/roslyn/issues/21281
        internal virtual TResult VisitFixedStatement(IFixedStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitExpressionStatement(IExpressionStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // https://github.com/dotnet/roslyn/issues/22005
        internal virtual TResult VisitWithStatement(IWithStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitStopStatement(IStopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEndStatement(IEndStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInvocationExpression(IInvocationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArgument(IArgument operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitOmittedArgumentExpression(IOmittedArgumentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // API moved internal for V1
        // https://github.com/dotnet/roslyn/issues/21295
        internal virtual TResult VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLocalReferenceExpression(ILocalReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParameterReferenceExpression(IParameterReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInstanceReferenceExpression(IInstanceReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFieldReferenceExpression(IFieldReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitMethodReferenceExpression(IMethodReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitPropertyReferenceExpression(IPropertyReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEventReferenceExpression(IEventReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitEventAssignmentExpression(IEventAssignmentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConditionalAccessExpression(IConditionalAccessExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConditionalAccessInstanceExpression(IConditionalAccessInstanceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        // https://github.com/dotnet/roslyn/issues/21294
        internal virtual TResult VisitPlaceholderExpression(IPlaceholderExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitUnaryOperatorExpression(IUnaryOperatorExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitBinaryOperatorExpression(IBinaryOperatorExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConversionExpression(IConversionExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConditionalExpression(IConditionalExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCoalesceExpression(ICoalesceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIsTypeExpression(IIsTypeExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSizeOfExpression(ISizeOfExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTypeOfExpression(ITypeOfExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAnonymousFunctionExpression(IAnonymousFunctionExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDelegateCreationExpression(IDelegateCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLiteralExpression(ILiteralExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAwaitExpression(IAwaitExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitNameOfExpression(INameOfExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitThrowExpression(IThrowExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAddressOfExpression(IAddressOfExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitObjectCreationExpression(IObjectCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitAnonymousObjectCreationExpression(IAnonymousObjectCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicObjectCreationExpression(IDynamicObjectCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicInvocationExpression(IDynamicInvocationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicIndexerAccessExpression(IDynamicIndexerAccessExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitObjectOrCollectionInitializerExpression(IObjectOrCollectionInitializerExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitMemberInitializerExpression(IMemberInitializerExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCollectionElementInitializerExpression(ICollectionElementInitializerExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFieldInitializer(IFieldInitializer operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitVariableInitializer(IVariableInitializer operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitPropertyInitializer(IPropertyInitializer operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParameterInitializer(IParameterInitializer operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArrayCreationExpression(IArrayCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitArrayInitializer(IArrayInitializer operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitSimpleAssignmentExpression(ISimpleAssignmentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDeconstructionAssignmentExpression(IDeconstructionAssignmentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDeclarationExpression(IDeclarationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIncrementOrDecrementExpression(IIncrementOrDecrementExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParenthesizedExpression(IParenthesizedExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDynamicMemberReferenceExpression(IDynamicMemberReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDefaultValueExpression(IDefaultValueExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTypeParameterObjectCreationExpression(ITypeParameterObjectCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInvalidStatement(IInvalidStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInvalidExpression(IInvalidExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLocalFunctionStatement(ILocalFunctionStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInterpolatedStringExpression(IInterpolatedStringExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInterpolatedStringText(IInterpolatedStringText operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitInterpolation(IInterpolation operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIsPatternExpression(IIsPatternExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitConstantPattern(IConstantPattern operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDeclarationPattern(IDeclarationPattern operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitDiscardPattern(IDiscardPattern operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitRecursivePattern(IRecursivePattern operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitPatternCaseClause(IPatternCaseClause operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTupleExpression(ITupleExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitTranslatedQueryExpression(ITranslatedQueryExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }
        
        public virtual TResult VisitRaiseEventStatement(IRaiseEventStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument); 
        }
    }
}
