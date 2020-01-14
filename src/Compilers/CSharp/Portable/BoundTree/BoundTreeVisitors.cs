// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using Roslyn.Utilities;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeVisitor<A, R>
    {
        protected BoundTreeVisitor()
        {
        }

        public virtual R Visit(BoundNode node, A arg)
        {
            if (node == null)
            {
                return default(R);
            }

            // this switch contains fewer than 50 of the most common node kinds
            switch (node.Kind)
            {
                case BoundKind.TypeExpression:
                    return VisitTypeExpression(node as BoundTypeExpression, arg);
                case BoundKind.NamespaceExpression:
                    return VisitNamespaceExpression(node as BoundNamespaceExpression, arg);
                case BoundKind.UnaryOperator:
                    return VisitUnaryOperator(node as BoundUnaryOperator, arg);
                case BoundKind.IncrementOperator:
                    return VisitIncrementOperator(node as BoundIncrementOperator, arg);
                case BoundKind.BinaryOperator:
                    return VisitBinaryOperator(node as BoundBinaryOperator, arg);
                case BoundKind.CompoundAssignmentOperator:
                    return VisitCompoundAssignmentOperator(node as BoundCompoundAssignmentOperator, arg);
                case BoundKind.AssignmentOperator:
                    return VisitAssignmentOperator(node as BoundAssignmentOperator, arg);
                case BoundKind.NullCoalescingOperator:
                    return VisitNullCoalescingOperator(node as BoundNullCoalescingOperator, arg);
                case BoundKind.ConditionalOperator:
                    return VisitConditionalOperator(node as BoundConditionalOperator, arg);
                case BoundKind.ArrayAccess:
                    return VisitArrayAccess(node as BoundArrayAccess, arg);
                case BoundKind.TypeOfOperator:
                    return VisitTypeOfOperator(node as BoundTypeOfOperator, arg);
                case BoundKind.DefaultLiteral:
                    return VisitDefaultLiteral(node as BoundDefaultLiteral, arg);
                case BoundKind.DefaultExpression:
                    return VisitDefaultExpression(node as BoundDefaultExpression, arg);
                case BoundKind.IsOperator:
                    return VisitIsOperator(node as BoundIsOperator, arg);
                case BoundKind.AsOperator:
                    return VisitAsOperator(node as BoundAsOperator, arg);
                case BoundKind.Conversion:
                    return VisitConversion(node as BoundConversion, arg);
                case BoundKind.SequencePointExpression:
                    return VisitSequencePointExpression(node as BoundSequencePointExpression, arg);
                case BoundKind.SequencePoint:
                    return VisitSequencePoint(node as BoundSequencePoint, arg);
                case BoundKind.SequencePointWithSpan:
                    return VisitSequencePointWithSpan(node as BoundSequencePointWithSpan, arg);
                case BoundKind.Block:
                    return VisitBlock(node as BoundBlock, arg);
                case BoundKind.LocalDeclaration:
                    return VisitLocalDeclaration(node as BoundLocalDeclaration, arg);
                case BoundKind.MultipleLocalDeclarations:
                    return VisitMultipleLocalDeclarations(node as BoundMultipleLocalDeclarations, arg);
                case BoundKind.Sequence:
                    return VisitSequence(node as BoundSequence, arg);
                case BoundKind.NoOpStatement:
                    return VisitNoOpStatement(node as BoundNoOpStatement, arg);
                case BoundKind.ReturnStatement:
                    return VisitReturnStatement(node as BoundReturnStatement, arg);
                case BoundKind.ThrowStatement:
                    return VisitThrowStatement(node as BoundThrowStatement, arg);
                case BoundKind.ExpressionStatement:
                    return VisitExpressionStatement(node as BoundExpressionStatement, arg);
                case BoundKind.BreakStatement:
                    return VisitBreakStatement(node as BoundBreakStatement, arg);
                case BoundKind.ContinueStatement:
                    return VisitContinueStatement(node as BoundContinueStatement, arg);
                case BoundKind.IfStatement:
                    return VisitIfStatement(node as BoundIfStatement, arg);
                case BoundKind.ForEachStatement:
                    return VisitForEachStatement(node as BoundForEachStatement, arg);
                case BoundKind.TryStatement:
                    return VisitTryStatement(node as BoundTryStatement, arg);
                case BoundKind.Literal:
                    return VisitLiteral(node as BoundLiteral, arg);
                case BoundKind.ThisReference:
                    return VisitThisReference(node as BoundThisReference, arg);
                case BoundKind.Local:
                    return VisitLocal(node as BoundLocal, arg);
                case BoundKind.Parameter:
                    return VisitParameter(node as BoundParameter, arg);
                case BoundKind.LabelStatement:
                    return VisitLabelStatement(node as BoundLabelStatement, arg);
                case BoundKind.GotoStatement:
                    return VisitGotoStatement(node as BoundGotoStatement, arg);
                case BoundKind.LabeledStatement:
                    return VisitLabeledStatement(node as BoundLabeledStatement, arg);
                case BoundKind.StatementList:
                    return VisitStatementList(node as BoundStatementList, arg);
                case BoundKind.ConditionalGoto:
                    return VisitConditionalGoto(node as BoundConditionalGoto, arg);
                case BoundKind.Call:
                    return VisitCall(node as BoundCall, arg);
                case BoundKind.ObjectCreationExpression:
                    return VisitObjectCreationExpression(node as BoundObjectCreationExpression, arg);
                case BoundKind.DelegateCreationExpression:
                    return VisitDelegateCreationExpression(node as BoundDelegateCreationExpression, arg);
                case BoundKind.FieldAccess:
                    return VisitFieldAccess(node as BoundFieldAccess, arg);
                case BoundKind.PropertyAccess:
                    return VisitPropertyAccess(node as BoundPropertyAccess, arg);
                case BoundKind.Lambda:
                    return VisitLambda(node as BoundLambda, arg);
                case BoundKind.NameOfOperator:
                    return VisitNameOfOperator(node as BoundNameOfOperator, arg);
            }

            return VisitInternal(node, arg);
        }

        public virtual R DefaultVisit(BoundNode node, A arg)
        {
            return default(R);
        }
    }

    internal abstract partial class BoundTreeVisitor
    {
        protected BoundTreeVisitor()
        {
        }

        [DebuggerHidden]
        public virtual BoundNode Visit(BoundNode node)
        {
            if (node != null)
            {
                return node.Accept(this);
            }

            return null;
        }

        [DebuggerHidden]
        public virtual BoundNode DefaultVisit(BoundNode node)
        {
            return null;
        }

        public class CancelledByStackGuardException : Exception
        {
            public readonly BoundNode Node;

            public CancelledByStackGuardException(Exception inner, BoundNode node)
                : base(inner.Message, inner)
            {
                Node = node;
            }

            public void AddAnError(DiagnosticBag diagnostics)
            {
                diagnostics.Add(ErrorCode.ERR_InsufficientStack, GetTooLongOrComplexExpressionErrorLocation(Node));
            }

            public static Location GetTooLongOrComplexExpressionErrorLocation(BoundNode node)
            {
                SyntaxNode syntax = node.Syntax;

                if (!(syntax is ExpressionSyntax))
                {
                    syntax = syntax.DescendantNodes(n => !(n is ExpressionSyntax)).OfType<ExpressionSyntax>().FirstOrDefault() ?? syntax;
                }

                return syntax.GetFirstToken().GetLocation();
            }
        }

        /// <summary>
        /// Consumers must provide implementation for <see cref="VisitExpressionWithoutStackGuard"/>.
        /// </summary>
        [DebuggerStepThrough]
        protected BoundExpression VisitExpressionWithStackGuard(ref int recursionDepth, BoundExpression node)
        {
            BoundExpression result;
            recursionDepth++;
#if DEBUG
            int saveRecursionDepth = recursionDepth;
#endif

            if (recursionDepth > 1 || !ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException())
            {
                StackGuard.EnsureSufficientExecutionStack(recursionDepth);

                result = VisitExpressionWithoutStackGuard(node);
            }
            else
            {
                result = VisitExpressionWithStackGuard(node);
            }

#if DEBUG
            Debug.Assert(saveRecursionDepth == recursionDepth);
#endif
            recursionDepth--;
            return result;
        }

        protected virtual bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return true;
        }

        [DebuggerStepThrough]
        private BoundExpression VisitExpressionWithStackGuard(BoundExpression node)
        {
            try
            {
                return VisitExpressionWithoutStackGuard(node);
            }
            catch (InsufficientExecutionStackException ex)
            {
                throw new CancelledByStackGuardException(ex, node);
            }
        }

        /// <summary>
        /// We should be intentional about behavior of derived classes regarding guarding against stack overflow.
        /// </summary>
        protected abstract BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node);
    }
}
