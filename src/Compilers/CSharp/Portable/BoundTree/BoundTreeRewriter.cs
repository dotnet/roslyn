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

        public sealed override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
        {
            BoundPattern child = node.Left;

            if (child.Kind != BoundKind.BinaryPattern)
            {
                return base.VisitBinaryPattern(node);
            }

            var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
            stack.Push(node);

            BoundBinaryPattern binary = (BoundBinaryPattern)child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;

                if (child.Kind != BoundKind.BinaryPattern)
                {
                    break;
                }

                binary = (BoundBinaryPattern)child;
            }

            var left = (BoundPattern?)this.Visit(child);
            Debug.Assert(left is { });

            do
            {
                binary = stack.Pop();
                var right = (BoundPattern?)this.Visit(binary.Right);
                Debug.Assert(right is { });
                left = binary.Update(binary.Disjunction, left, right, VisitType(binary.InputType), VisitType(binary.NarrowedType));
            }
            while (stack.Count > 0);

            Debug.Assert((object)binary == node);
            stack.Free();

            return left;
        }
    }
}
