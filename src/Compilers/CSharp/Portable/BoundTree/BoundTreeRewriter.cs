// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeRewriter : BoundTreeVisitor
    {
        [return: NotNullIfNotNull(nameof(type))]
        public virtual TypeSymbol? VisitType(TypeSymbol? type)
        {
            return type;
        }

        public ImmutableArray<T> VisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            if (list.IsDefault)
            {
                return list;
            }

            return DoVisitList(list);
        }

        private ImmutableArray<T> DoVisitList<T>(ImmutableArray<T> list) where T : BoundNode
        {
            ArrayBuilder<T>? newList = null;
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                System.Diagnostics.Debug.Assert(item != null);

                var visited = this.Visit(item);
                if (newList == null && item != visited)
                {
                    newList = ArrayBuilder<T>.GetInstance();
                    if (i > 0)
                    {
                        newList.AddRange(list, i);
                    }
                }

                if (newList != null && visited != null)
                {
                    newList.Add((T)visited);
                }
            }

            if (newList != null)
            {
                return newList.ToImmutableAndFree();
            }

            return list;
        }
    }

    internal abstract class BoundTreeRewriterWithStackGuard : BoundTreeRewriter
    {
        private int _recursionDepth;

        protected BoundTreeRewriterWithStackGuard()
        { }

        protected BoundTreeRewriterWithStackGuard(int recursionDepth)
        {
            _recursionDepth = recursionDepth;
        }

        protected int RecursionDepth => _recursionDepth;

        [return: NotNullIfNotNull(nameof(node))]
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

    internal abstract class BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator : BoundTreeRewriterWithStackGuard
    {
        protected BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator()
        { }

        protected BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator(int recursionDepth)
            : base(recursionDepth)
        { }

        public sealed override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
        {
            BoundExpression child = node.Left;

            if (child.Kind != BoundKind.BinaryOperator)
            {
                return base.VisitBinaryOperator(node);
            }

            var stack = ArrayBuilder<BoundBinaryOperator>.GetInstance();
            stack.Push(node);

            BoundBinaryOperator binary = (BoundBinaryOperator)child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;

                if (child.Kind != BoundKind.BinaryOperator)
                {
                    break;
                }

                binary = (BoundBinaryOperator)child;
            }

            var left = (BoundExpression?)this.Visit(child);
            Debug.Assert(left is { });

            do
            {
                binary = stack.Pop();
                var right = (BoundExpression?)this.Visit(binary.Right);
                Debug.Assert(right is { });
                var type = this.VisitType(binary.Type);
                left = binary.Update(binary.OperatorKind, binary.Data, binary.ResultKind, left, right, type);
            }
            while (stack.Count > 0);

            Debug.Assert((object)binary == node);
            stack.Free();

            return left;
        }

        public sealed override BoundNode? VisitIfStatement(BoundIfStatement node)
        {
            if (node.AlternativeOpt is not BoundIfStatement ifStatement)
            {
                return base.VisitIfStatement(node);
            }

            var stack = ArrayBuilder<BoundIfStatement>.GetInstance();
            stack.Push(node);

            BoundStatement? alternative;
            while (true)
            {
                stack.Push(ifStatement);

                alternative = ifStatement.AlternativeOpt;
                if (alternative is not BoundIfStatement nextIfStatement)
                {
                    break;
                }

                ifStatement = nextIfStatement;
            }

            alternative = (BoundStatement?)this.Visit(alternative);

            do
            {
                ifStatement = stack.Pop();

                BoundExpression condition = (BoundExpression)this.Visit(ifStatement.Condition);
                BoundStatement consequence = (BoundStatement)this.Visit(ifStatement.Consequence);

                alternative = ifStatement.Update(condition, consequence, alternative);
            }
            while (stack.Count > 0);

            Debug.Assert((object)ifStatement == node);
            stack.Free();

            return alternative;
        }
    }
}
