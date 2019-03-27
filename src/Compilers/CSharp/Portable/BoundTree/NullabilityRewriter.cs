// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullabilityRewriter : BoundTreeRewriter
    {
        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            return (BoundExpression)Visit(node);
        }

        public override BoundNode VisitBinaryOperator(BoundBinaryOperator node)
        {
            return VisitBinaryOperatorBase(node, (binary, left, right, type) => node.Update(node.OperatorKind, node.ConstantValueOpt, node.MethodOpt, node.ResultKind, left, right, type));
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            return VisitBinaryOperatorBase(node, (binary, left, right, type) => node.Update(node.OperatorKind, node.LogicalOperator, node.TrueOperator, node.FalseOperator, node.ResultKind, left, right, type));
        }

        private BoundNode VisitBinaryOperatorBase(BoundBinaryOperatorBase binaryOperator, Func<BoundBinaryOperatorBase, BoundExpression, BoundExpression, TypeSymbol, BoundBinaryOperatorBase> nodeUpdater)
        {
            // Use an explicit stack to avoid blowing the managed stack when visiting deeply-recursive
            // binary nodes
            var stack = ArrayBuilder<BoundBinaryOperatorBase>.GetInstance();
            BoundBinaryOperatorBase currentBinary;
            BoundExpression child = binaryOperator;

            while (true)
            {
                if (child is BoundBinaryOperatorBase childBinary)
                {
                    currentBinary = childBinary;
                    child = childBinary.Left;
                    stack.Push(currentBinary);
                }
                else
                {
                    break;
                }
            }

            Debug.Assert(stack.Count > 0);
            currentBinary = null;

            while (stack.Count > 0)
            {
                var left = currentBinary;
                currentBinary = stack.Pop();
                currentBinary = updateNode(currentBinary, left, this, nodeUpdater);
            }

            Debug.Assert(currentBinary != null);
            return currentBinary;

            static BoundBinaryOperatorBase updateNode(BoundBinaryOperatorBase node, BoundExpression leftOpt, NullabilityRewriter rewriter, Func<BoundBinaryOperatorBase, BoundExpression, BoundExpression, TypeSymbol, BoundBinaryOperatorBase> nodeUpdater)
            {
                var foundInfo = rewriter._updatedNullabilities.TryGetValue(node, out (NullabilityInfo Info, TypeSymbol Type) infoAndType);

                leftOpt ??= (BoundExpression)rewriter.Visit(node.Left);
                Debug.Assert(leftOpt != null);
                var right = (BoundExpression)rewriter.Visit(node.Right);
                node = nodeUpdater(node, leftOpt, right, foundInfo ? infoAndType.Type : node.Type);

                if (foundInfo)
                {
                    node.TopLevelNullability = infoAndType.Info;
                }

                return node;
            }
        }
    }
}
