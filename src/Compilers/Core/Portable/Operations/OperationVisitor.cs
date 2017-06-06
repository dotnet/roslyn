﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public virtual void VisitIfStatement(IIfStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForLoopStatement(IForLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLabelStatement(ILabelStatement operation)
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

        public virtual void VisitThrowStatement(IThrowStatement operation)
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

        public virtual void VisitFixedStatement(IFixedStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitExpressionStatement(IExpressionStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitWithStatement(IWithStatement operation)
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

        public virtual void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
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

        public virtual void VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation)
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

        public virtual void VisitMethodBindingExpression(IMethodBindingExpression operation)
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

        public virtual void VisitPlaceholderExpression(IPlaceholderExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIndexedPropertyReferenceExpression(IIndexedPropertyReferenceExpression operation)
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

        public virtual void VisitConditionalChoiceExpression(IConditionalChoiceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitNullCoalescingExpression(INullCoalescingExpression operation)
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

        public virtual void VisitLambdaExpression(ILambdaExpression operation)
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

        public virtual void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitFieldInitializer(IFieldInitializer operation)
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

        public virtual void VisitAssignmentExpression(IAssignmentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitIncrementExpression(IIncrementExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUnboundLambdaExpression(IUnboundLambdaExpression operation)
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

        public virtual TResult VisitIfStatement(IIfStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForLoopStatement(IForLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitForEachLoopStatement(IForEachLoopStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLabelStatement(ILabelStatement operation, TArgument argument)
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

        public virtual TResult VisitThrowStatement(IThrowStatement operation, TArgument argument)
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

        public virtual TResult VisitFixedStatement(IFixedStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitExpressionStatement(IExpressionStatement operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitWithStatement(IWithStatement operation, TArgument argument)
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

        public virtual TResult VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation, TArgument argument)
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

        public virtual TResult VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation, TArgument argument)
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

        public virtual TResult VisitMethodBindingExpression(IMethodBindingExpression operation, TArgument argument)
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

        public virtual TResult VisitPlaceholderExpression(IPlaceholderExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIndexedPropertyReferenceExpression(IIndexedPropertyReferenceExpression operation, TArgument argument)
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

        public virtual TResult VisitConditionalChoiceExpression(IConditionalChoiceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitNullCoalescingExpression(INullCoalescingExpression operation, TArgument argument)
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

        public virtual TResult VisitLambdaExpression(ILambdaExpression operation, TArgument argument)
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

        public virtual TResult VisitAddressOfExpression(IAddressOfExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitObjectCreationExpression(IObjectCreationExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitFieldInitializer(IFieldInitializer operation, TArgument argument)
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

        public virtual TResult VisitAssignmentExpression(IAssignmentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitIncrementExpression(IIncrementExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitParenthesizedExpression(IParenthesizedExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation, TArgument argument)
        {
            return DefaultVisit(operation, argument);
        }

        public virtual TResult VisitUnboundLambdaExpression(IUnboundLambdaExpression operation, TArgument argument)
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
    }
}
