// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeWalker : BoundTreeVisitor
    {
        protected BoundTreeWalker()
        {
        }

        public void VisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            if (!list.IsDefault)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    this.Visit(list[i]);
                }
            }
        }

        protected void VisitUnoptimizedForm(BoundQueryClause queryClause)
        {
            BoundExpression? unoptimizedForm = queryClause.UnoptimizedForm;

            // The unoptimized form of a query has an additional argument in the call,
            // which is typically the "trivial" expression x where x is the query
            // variable.  So that we can make sense of x in this
            // context, we store the unoptimized form and visit this extra argument.
            var qc = unoptimizedForm as BoundQueryClause;
            if (qc != null) unoptimizedForm = qc.Value;
            var call = unoptimizedForm as BoundCall;
            if (call != null && (object)call.Method != null)
            {
                var arguments = call.Arguments;
                if (call.Method.Name == "Select")
                {
                    this.Visit(arguments[arguments.Length - 1]);
                }
                else if (call.Method.Name == "GroupBy")
                {
                    this.Visit(arguments[arguments.Length - 2]);
                }
            }
        }
    }

    /// <summary>
    /// Note: do not use a static/singleton instance of this type, as it holds state.
    /// </summary>
    internal abstract class BoundTreeWalkerWithStackGuard : BoundTreeWalker
    {
        private int _recursionDepth;

        protected BoundTreeWalkerWithStackGuard()
        { }

        protected BoundTreeWalkerWithStackGuard(int recursionDepth)
        {
            _recursionDepth = recursionDepth;
        }

        protected int RecursionDepth => _recursionDepth;

        public override BoundNode? Visit(BoundNode? node)
        {
            var expression = node as BoundExpression;
            if (expression != null)
            {
                return VisitExpressionWithStackGuard(ref _recursionDepth, expression);
            }

            return base.Visit(node);
        }

        protected BoundExpression VisitExpressionWithStackGuard(BoundExpression node)
        {
            return VisitExpressionWithStackGuard(ref _recursionDepth, node);
        }

        protected sealed override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            return (BoundExpression)base.Visit(node);
        }
    }

    /// <summary>
    /// Note: do not use a static/singleton instance of this type, as it holds state.
    /// </summary>
    internal abstract class BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator : BoundTreeWalkerWithStackGuard
    {
        protected BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator()
        { }

        protected BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator(int recursionDepth)
            : base(recursionDepth)
        { }

        public sealed override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
        {
            if (node.Left.Kind != BoundKind.BinaryOperator)
            {
                return base.VisitBinaryOperator(node);
            }

            var rightOperands = ArrayBuilder<BoundExpression>.GetInstance();

            rightOperands.Push(node.Right);

            var binary = (BoundBinaryOperator)node.Left;

            rightOperands.Push(binary.Right);

            BoundExpression current = binary.Left;

            while (current.Kind == BoundKind.BinaryOperator)
            {
                binary = (BoundBinaryOperator)current;
                rightOperands.Push(binary.Right);
                current = binary.Left;
            }

            this.Visit(current);

            while (rightOperands.Count > 0)
            {
                this.Visit(rightOperands.Pop());
            }

            rightOperands.Free();
            return null;
        }

        public sealed override BoundNode? VisitCall(BoundCall node)
        {
            if (node.ReceiverOpt is BoundCall receiver1)
            {
                var calls = ArrayBuilder<BoundCall>.GetInstance();

                calls.Push(node);

                node = receiver1;
                while (node.ReceiverOpt is BoundCall receiver2)
                {
                    calls.Push(node);
                    node = receiver2;
                }

                VisitReceiver(node);

                do
                {
                    VisitArguments(node);
                }
                while (calls.TryPop(out node!));

                calls.Free();
            }
            else
            {
                VisitReceiver(node);
                VisitArguments(node);
            }

            return null;
        }

        /// <summary>
        /// Called only for the first (in evaluation order) <see cref="BoundCall"/> in the chain.
        /// </summary>
        protected virtual void VisitReceiver(BoundCall node)
        {
            this.Visit(node.ReceiverOpt);
        }

        protected virtual void VisitArguments(BoundCall node)
        {
            this.VisitList(node.Arguments);
        }
    }
}
