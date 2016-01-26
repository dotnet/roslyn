// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method.
    /// </summary>
    public abstract class IOperationVisitor
    {
        public virtual void Visit(IOperation operation)
        {
            operation?.Accept(this);
        }

        public virtual void DefaultVisit(IOperation operation)
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

        public virtual void VisitVariable(IVariable operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitSwitchStatement(ISwitchStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitCase(ICase operation)
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

        public virtual void VisitLabeledStatement(ILabeledStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitBranchStatement(IBranchStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitYieldBreakStatement(IStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEmptyStatement(IStatement operation)
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

        public virtual void VisitCatch(ICatch operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUsingWithDeclarationStatement(IUsingWithDeclarationStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitUsingWithExpressionStatement(IUsingWithExpressionStatement operation)
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

        public virtual void VisitStopStatement(IStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitEndStatement(IStatement operation)
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

        public virtual void VisitOmittedArgumentExpression(IExpression operation)
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

        public virtual void VisitIsExpression(IIsExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTypeOperationExpression(ITypeOperationExpression operation)
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

        public virtual void VisitUnboundLambdaExpression(IExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitDefaultValueExpression(IExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitTypeParameterObjectCreationExpression(IExpression operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInvalidStatement(IStatement operation)
        {
            DefaultVisit(operation);
        }

        public virtual void VisitInvalidExpression(IExpression operation)
        {
            DefaultVisit(operation);
        }
    }

    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method with an additional argument of the type specified by the 
    /// <typeparamref name="TArg"/> parameter and produces a value of the type specified by 
    /// the <typeparamref name="TResult"/> parameter.
    /// </summary>
    /// <typeparam name="TArg">
    /// The type of the additional argument passed to this visitor's Visit method.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of the return value of this visitor's Visit method.
    /// </typeparam>
    public abstract class IOperationVisitor<TArg, TResult>
    {
        public virtual TResult Visit(IOperation operation, TArg arg)
        {
            return operation == null ? default(TResult) : operation.Accept(this, arg);
        }

        public virtual TResult DefaultVisit(IOperation operation, TArg arg)
        {
            return default(TResult);
        }

        public virtual TResult VisitBlockStatement(IBlockStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitVariableDeclarationStatement(IVariableDeclarationStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitVariable(IVariable operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitSwitchStatement(ISwitchStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitCase(ICase operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitSingleValueCaseClause(ISingleValueCaseClause operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitRelationalCaseClause(IRelationalCaseClause operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitRangeCaseClause(IRangeCaseClause operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitIfStatement(IIfStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitForLoopStatement(IForLoopStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitForEachLoopStatement(IForEachLoopStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLabelStatement(ILabelStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLabeledStatement(ILabeledStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitBranchStatement(IBranchStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitYieldBreakStatement(IStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitEmptyStatement(IStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitThrowStatement(IThrowStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitReturnStatement(IReturnStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLockStatement(ILockStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitTryStatement(ITryStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitCatch(ICatch operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitUsingWithDeclarationStatement(IUsingWithDeclarationStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitUsingWithExpressionStatement(IUsingWithExpressionStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitFixedStatement(IFixedStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitExpressionStatement(IExpressionStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitWithStatement(IWithStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitStopStatement(IStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitEndStatement(IStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitInvocationExpression(IInvocationExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitArgument(IArgument operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitOmittedArgumentExpression(IExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLocalReferenceExpression(ILocalReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitParameterReferenceExpression(IParameterReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitInstanceReferenceExpression(IInstanceReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitFieldReferenceExpression(IFieldReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitMethodBindingExpression(IMethodBindingExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitPropertyReferenceExpression(IPropertyReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitEventReferenceExpression(IEventReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitEventAssignmentExpression(IEventAssignmentExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitConditionalAccessExpression(IConditionalAccessExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitUnaryOperatorExpression(IUnaryOperatorExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitBinaryOperatorExpression(IBinaryOperatorExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitConversionExpression(IConversionExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitConditionalChoiceExpression(IConditionalChoiceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitNullCoalescingExpression(INullCoalescingExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitIsExpression(IIsExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitTypeOperationExpression(ITypeOperationExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLambdaExpression(ILambdaExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLiteralExpression(ILiteralExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitAwaitExpression(IAwaitExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitAddressOfExpression(IAddressOfExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitObjectCreationExpression(IObjectCreationExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitFieldInitializer(IFieldInitializer operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitPropertyInitializer(IPropertyInitializer operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitArrayCreationExpression(IArrayCreationExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitArrayInitializer(IArrayInitializer operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitAssignmentExpression(IAssignmentExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitIncrementExpression(IIncrementExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitParenthesizedExpression(IParenthesizedExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitUnboundLambdaExpression(IExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitDefaultValueExpression(IExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitTypeParameterObjectCreationExpression(IExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitInvalidStatement(IStatement operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }

        public virtual TResult VisitInvalidExpression(IExpression operation, TArg arg)
        {
            return DefaultVisit(operation, arg);
        }
    }
}
