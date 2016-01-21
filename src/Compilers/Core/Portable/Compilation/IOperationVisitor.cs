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
            this.DefaultVisit(operation);
        }

        public virtual void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitVariable(IVariable operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitSwitchStatement(ISwitchStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitCase(ICase operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitSingleValueCaseClause(ISingleValueCaseClause operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitRangeCaseClause(IRangeCaseClause operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitIfStatement(IIfStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitForLoopStatement(IForLoopStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLabelStatement(ILabelStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLabeledStatement(ILabeledStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitBranchStatement(IBranchStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitYieldBreakStatement(IStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitEmptyStatement(IStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitThrowStatement(IThrowStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitReturnStatement(IReturnStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLockStatement(ILockStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitTryStatement(ITryStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitCatch(ICatch operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitUsingWithDeclarationStatement(IUsingWithDeclarationStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitUsingWithExpressionStatement(IUsingWithExpressionStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitFixedStatement(IFixedStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitExpressionStatement(IExpressionStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitWithStatement(IWithStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitStopStatement(IStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitEndStatement(IStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitInvocationExpression(IInvocationExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitArgument(IArgument operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitOmittedArgumentExpression(IExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitInstanceReferenceExpression(IInstanceReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitMethodBindingExpression(IMethodBindingExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitEventReferenceExpression(IEventReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitEventAssignmentExpression(IEventAssignmentExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitConditionalAccessExpression(IConditionalAccessExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitConversionExpression(IConversionExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitConditionalChoiceExpression(IConditionalChoiceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitNullCoalescingExpression(INullCoalescingExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitIsExpression(IIsExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitTypeOperationExpression(ITypeOperationExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLambdaExpression(ILambdaExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLiteralExpression(ILiteralExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitAwaitExpression(IAwaitExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitFieldInitializer(IFieldInitializer operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitPropertyInitializer(IPropertyInitializer operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitArrayCreationExpression(IArrayCreationExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitArrayInitializer(IArrayInitializer operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitAssignmentExpression(IAssignmentExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitIncrementExpression(IIncrementExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitUnboundLambdaExpression(IExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitDefaultValueExpression(IExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitTypeParameterObjectCreationExpression(IExpression operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitInvalidStatement(IStatement operation)
        {
            this.DefaultVisit(operation);
        }

        public virtual void VisitInvalidExpression(IExpression operation)
        {
            this.DefaultVisit(operation);
        }
    }

    /// <summary>
    /// Represents a <see cref="IOperation"/> visitor that visits only the single IOperation
    /// passed into its Visit method and produces 
    /// a value of the type specified by the <typeparamref name="TResult"/> parameter.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the return value this visitor's Visit method.
    /// </typeparam>
    public abstract class IOperationVisitor<TResult>
    {
        public virtual TResult Visit(IOperation operation)
        {
            return operation == null ? default(TResult) : operation.Accept(this);
        }

        public virtual TResult DefaultVisit(IOperation operation)
        {
            return default(TResult);
        }

        public virtual TResult VisitBlockStatement(IBlockStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitVariable(IVariable operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitSwitchStatement(ISwitchStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitCase(ICase operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitSingleValueCaseClause(ISingleValueCaseClause operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitRangeCaseClause(IRangeCaseClause operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitIfStatement(IIfStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitForLoopStatement(IForLoopStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLabelStatement(ILabelStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLabeledStatement(ILabeledStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitBranchStatement(IBranchStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitYieldBreakStatement(IStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitEmptyStatement(IStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitThrowStatement(IThrowStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitReturnStatement(IReturnStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLockStatement(ILockStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitTryStatement(ITryStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitCatch(ICatch operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitUsingWithDeclarationStatement(IUsingWithDeclarationStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitUsingWithExpressionStatement(IUsingWithExpressionStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitFixedStatement(IFixedStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitExpressionStatement(IExpressionStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitWithStatement(IWithStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitStopStatement(IStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitEndStatement(IStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitInvocationExpression(IInvocationExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitArgument(IArgument operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitOmittedArgumentExpression(IExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitInstanceReferenceExpression(IInstanceReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitMethodBindingExpression(IMethodBindingExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitEventReferenceExpression(IEventReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitEventAssignmentExpression(IEventAssignmentExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitConditionalAccessExpression(IConditionalAccessExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitConversionExpression(IConversionExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitConditionalChoiceExpression(IConditionalChoiceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitNullCoalescingExpression(INullCoalescingExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitIsExpression(IIsExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitTypeOperationExpression(ITypeOperationExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLambdaExpression(ILambdaExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLiteralExpression(ILiteralExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitAwaitExpression(IAwaitExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitAddressOfExpression(IAddressOfExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitFieldInitializer(IFieldInitializer operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitPropertyInitializer(IPropertyInitializer operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitArrayCreationExpression(IArrayCreationExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitArrayInitializer(IArrayInitializer operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitAssignmentExpression(IAssignmentExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitIncrementExpression(IIncrementExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitUnboundLambdaExpression(IExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitDefaultValueExpression(IExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitTypeParameterObjectCreationExpression(IExpression operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitInvalidStatement(IStatement operation)
        {
            return this.DefaultVisit(operation);
        }

        public virtual TResult VisitInvalidExpression(IExpression operation)
        {
            return this.DefaultVisit(operation);
        }
    }
}
