// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundTreeRewriter : BoundTreeVisitor
    {
        public virtual TypeSymbol VisitType(TypeSymbol type)
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
            ArrayBuilder<T> newList = null;
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                System.Diagnostics.Debug.Assert(item != null);

                var visited = this.Visit(item);
                if (item != visited)
                {
                    if (newList == null)
                    {
                        newList = ArrayBuilder<T>.GetInstance();
                        if (i > 0)
                        {
                            newList.AddRange(list, i);
                        }
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

        public override BoundNode Visit(BoundNode node)
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

        public sealed override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
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

            var left = (BoundExpression)this.Visit(child);

            do
            {
                binary = stack.Pop();
                var right = (BoundExpression)this.Visit(binary.Right);
                var type = this.VisitType(binary.Type);
                left = binary.Update(binary.OperatorKind, binary.ConstantValueOpt, binary.MethodOpt, binary.ResultKind, binary.OriginalUserDefinedOperatorsOpt, left, right, type);
            }
            while (stack.Count > 0);

            Debug.Assert((object)binary == node);
            stack.Free();

            return left;
        }
    }
}
