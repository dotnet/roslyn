// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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
            return VisitBinaryOperatorBase(node);
        }

        public override BoundNode VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            return VisitBinaryOperatorBase(node);
        }

        private BoundNode VisitBinaryOperatorBase(BoundBinaryOperatorBase binaryOperator)
        {
            // Use an explicit stack to avoid blowing the managed stack when visiting deeply-recursive
            // binary nodes
            var stack = ArrayBuilder<BoundBinaryOperatorBase>.GetInstance();
            BoundBinaryOperatorBase currentBinary = binaryOperator;

            do
            {
                stack.Push(currentBinary);
                currentBinary = currentBinary.Left as BoundBinaryOperatorBase;
            }
            while (currentBinary != null);

            Debug.Assert(stack.Count > 0);
            var leftChild = (BoundExpression)Visit(stack.Peek().Left);

            do
            {
                currentBinary = stack.Pop();

                bool foundInfo = _updatedNullabilities.TryGetValue(currentBinary, out (NullabilityInfo Info, TypeSymbol Type) infoAndType);
                var right = (BoundExpression)Visit(currentBinary.Right);
                var type = foundInfo ? infoAndType.Type : currentBinary.Type;

#pragma warning disable IDE0055 // Fix formatting
                // https://github.com/dotnet/roslyn/issues/35031: We'll need to update the symbols for the internal methods/operators used in the binary operators
                currentBinary = currentBinary switch
                    {
                        BoundBinaryOperator binary => (BoundBinaryOperatorBase)binary.Update(binary.OperatorKind, binary.ConstantValueOpt, binary.MethodOpt, binary.ResultKind, leftChild, right, type),
                        BoundUserDefinedConditionalLogicalOperator logical => logical.Update(logical.OperatorKind, logical.LogicalOperator, logical.TrueOperator, logical.FalseOperator, logical.ResultKind, leftChild, right, type),
                        _ => throw ExceptionUtilities.UnexpectedValue(currentBinary.Kind),
                    };
#pragma warning restore IDE0055 // Fix formatting

                if (foundInfo)
                {
                    currentBinary.TopLevelNullability = infoAndType.Info;
                }

                leftChild = currentBinary;
            }
            while (stack.Count > 0);

            Debug.Assert(currentBinary != null);
            return currentBinary;
        }
    }
}
