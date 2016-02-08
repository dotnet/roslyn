﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a <see cref="OperationVisitor"/> that descends an entire <see cref="IOperation"/> tree
    /// visiting each IOperation and its child IOperation nodes in depth-first order.
    /// </summary>
    public abstract class OperationWalker : OperationVisitor
    {
        private int _recursionDepth;

        private void VisitArray<T>(ImmutableArray<T> list) where T : IOperation
        {
            if (!list.IsDefault)
            {
                foreach (var operation in list)
                {
                    Visit(operation);
                }
            }
        }

        public override void Visit(IOperation operation)
        {
            if (operation != null)
            {
                _recursionDepth++;
                try
                {
                    StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                    operation.Accept(this);
                }
                finally
                {
                    _recursionDepth--;
                }
                
            }
        }

        public override void VisitBlockStatement(IBlockStatement operation)
        {
            VisitArray(operation.Statements);
        }

        public override void VisitVariableDeclarationStatement(IVariableDeclarationStatement operation)
        {
            VisitArray(operation.Variables);
        }

        public override void VisitVariable(IVariable operation)
        {
            Visit(operation.InitialValue);
        }

        public override void VisitSwitchStatement(ISwitchStatement operation)
        {
            Visit(operation.Value);
            VisitArray(operation.Cases);
        }

        public override void VisitCase(ICase operation)
        {
            VisitArray(operation.Clauses);
            VisitArray(operation.Body);
        }

        public override void VisitSingleValueCaseClause(ISingleValueCaseClause operation)
        {
            Visit(operation.Value);
        }

        public override void VisitRelationalCaseClause(IRelationalCaseClause operation)
        {
            Visit(operation.Value);
        }

        public override void VisitRangeCaseClause(IRangeCaseClause operation)
        {
            Visit(operation.MinimumValue);
            Visit(operation.MaximumValue);
        }

        public override void VisitIfStatement(IIfStatement operation)
        {
            Visit(operation.Condition);
            Visit(operation.IfTrue);
            Visit(operation.IfFalse);
        }

        public override void VisitWhileUntilLoopStatement(IWhileUntilLoopStatement operation)
        {
            if (operation.IsTopTest)
            {
                Visit(operation.Condition);
                Visit(operation.Body);
            }
            else
            {
                Visit(operation.Body);
                Visit(operation.Condition);
            }
        }

        public override void VisitForLoopStatement(IForLoopStatement operation)
        {
            VisitArray(operation.Before);
            Visit(operation.Condition);
            Visit(operation.Body);
            VisitArray(operation.AtLoopBottom);
        }

        public override void VisitForEachLoopStatement(IForEachLoopStatement operation)
        {
            Visit(operation.Collection);
            Visit(operation.Body);
        }

        public override void VisitLabelStatement(ILabelStatement operation)
        { }

        public override void VisitLabeledStatement(ILabeledStatement operation)
        {
            Visit(operation.Labeled);
        }

        public override void VisitBranchStatement(IBranchStatement operation)
        { }

        public override void VisitYieldBreakStatement(IStatement operation)
        { }

        public override void VisitEmptyStatement(IStatement operation)
        { }

        public override void VisitThrowStatement(IThrowStatement operation)
        {
            Visit(operation.Thrown);
        }

        public override void VisitReturnStatement(IReturnStatement operation)
        {
            Visit(operation.Returned);
        }

        public override void VisitLockStatement(ILockStatement operation)
        {
            Visit(operation.Locked);
            Visit(operation.Body);
        }

        public override void VisitTryStatement(ITryStatement operation)
        {
            Visit(operation.Body);
            VisitArray(operation.Catches);
            Visit(operation.FinallyHandler);
        }

        public override void VisitCatch(ICatch operation)
        {
            Visit(operation.Filter);
            Visit(operation.Handler);
        }

        public override void VisitUsingWithDeclarationStatement(IUsingWithDeclarationStatement operation)
        {
            Visit(operation.Variables);
        }

        public override void VisitUsingWithExpressionStatement(IUsingWithExpressionStatement operation)
        {
            Visit(operation.Value);
        }

        public override void VisitFixedStatement(IFixedStatement operation)
        {
            Visit(operation.Variables);
            Visit(operation.Body);
        }

        public override void VisitExpressionStatement(IExpressionStatement operation)
        {
            Visit(operation.Expression);
        }

        public override void VisitWithStatement(IWithStatement operation)
        {
            Visit(operation.Value);
            Visit(operation.Body);
        }

        public override void VisitStopStatement(IStatement operation)
        { }

        public override void VisitEndStatement(IStatement operation)
        { }

        public override void VisitInvocationExpression(IInvocationExpression operation)
        {
            Visit(operation.Instance);
            VisitArray(operation.ArgumentsInSourceOrder);
        }

        public override void VisitArgument(IArgument operation)
        {
            Visit(operation.Value);
            Visit(operation.InConversion);
            Visit(operation.OutConversion);
        }

        public override void VisitOmittedArgumentExpression(IExpression operation)
        { }

        public override void VisitArrayElementReferenceExpression(IArrayElementReferenceExpression operation)
        {
            Visit(operation.ArrayReference);
            VisitArray(operation.Indices);
        }

        public override void VisitPointerIndirectionReferenceExpression(IPointerIndirectionReferenceExpression operation)
        {
            Visit(operation.Pointer);
        }

        public override void VisitLocalReferenceExpression(ILocalReferenceExpression operation)
        { }

        public override void VisitParameterReferenceExpression(IParameterReferenceExpression operation)
        { }

        public override void VisitSyntheticLocalReferenceExpression(ISyntheticLocalReferenceExpression operation)
        { }

        public override void VisitInstanceReferenceExpression(IInstanceReferenceExpression operation)
        { }

        public override void VisitFieldReferenceExpression(IFieldReferenceExpression operation)
        {
            Visit(operation.Instance);
        }

        public override void VisitMethodBindingExpression(IMethodBindingExpression operation)
        {
            Visit(operation.Instance);
        }

        public override void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
        {
            Visit(operation.Instance);
        }

        public override void VisitEventReferenceExpression(IEventReferenceExpression operation)
        {
            Visit(operation.Instance);
        }

        public override void VisitEventAssignmentExpression(IEventAssignmentExpression operation)
        {
            Visit(operation.EventInstance);
            Visit(operation.HandlerValue);
        }

        public override void VisitConditionalAccessExpression(IConditionalAccessExpression operation)
        {
            Visit(operation.Access);
        }

        public override void VisitUnaryOperatorExpression(IUnaryOperatorExpression operation)
        {
            Visit(operation.Operand);
        }

        public override void VisitBinaryOperatorExpression(IBinaryOperatorExpression operation)
        {
            Visit(operation.Left);
            Visit(operation.Right);
        }

        public override void VisitConversionExpression(IConversionExpression operation)
        {
            Visit(operation.Operand);
        }

        public override void VisitConditionalChoiceExpression(IConditionalChoiceExpression operation)
        {
            Visit(operation.Condition);
            Visit(operation.IfTrue);
            Visit(operation.IfFalse);
        }

        public override void VisitNullCoalescingExpression(INullCoalescingExpression operation)
        {
            Visit(operation.Primary);
            Visit(operation.Secondary);
        }

        public override void VisitIsExpression(IIsExpression operation)
        {
            Visit(operation.Operand);
        }

        public override void VisitTypeOperationExpression(ITypeOperationExpression operation)
        { }

        public override void VisitLambdaExpression(ILambdaExpression operation)
        {
            Visit(operation.Body);
        }

        public override void VisitLiteralExpression(ILiteralExpression operation)
        { }

        public override void VisitAwaitExpression(IAwaitExpression operation)
        {
            Visit(operation.Upon);
        }

        public override void VisitAddressOfExpression(IAddressOfExpression operation)
        {
            Visit(operation.Addressed);
        }

        public override void VisitObjectCreationExpression(IObjectCreationExpression operation)
        {
            VisitArray(operation.ConstructorArguments);
            VisitArray(operation.MemberInitializers);
        }

        public override void VisitFieldInitializer(IFieldInitializer operation)
        {
            Visit(operation.Value);
        }

        public override void VisitPropertyInitializer(IPropertyInitializer operation)
        {
            Visit(operation.Value);
        }

        public override void VisitParameterInitializer(IParameterInitializer operation)
        {
            Visit(operation.Value);
        }

        public override void VisitArrayCreationExpression(IArrayCreationExpression operation)
        {
            VisitArray(operation.DimensionSizes);
            Visit(operation.Initializer);
        }

        public override void VisitArrayInitializer(IArrayInitializer operation)
        {
            VisitArray(operation.ElementValues);
        }

        public override void VisitAssignmentExpression(IAssignmentExpression operation)
        {
            Visit(operation.Target);
            Visit(operation.Value);
        }

        public override void VisitCompoundAssignmentExpression(ICompoundAssignmentExpression operation)
        {
            Visit(operation.Target);
            Visit(operation.Value);
        }

        public override void VisitIncrementExpression(IIncrementExpression operation)
        {
            Visit(operation.Target);
            Visit(operation.Value);
        }

        public override void VisitParenthesizedExpression(IParenthesizedExpression operation)
        {
            Visit(operation.Operand);
        }

        public override void VisitLateBoundMemberReferenceExpression(ILateBoundMemberReferenceExpression operation)
        {
            Visit(operation.Instance);
        }

        public override void VisitUnboundLambdaExpression(IExpression operation)
        { }

        public override void VisitDefaultValueExpression(IExpression operation)
        { }

        public override void VisitTypeParameterObjectCreationExpression(IExpression operation)
        { }

        public override void VisitInvalidStatement(IStatement operation)
        { }

        public override void VisitInvalidExpression(IExpression operation)
        { }
    }
}
