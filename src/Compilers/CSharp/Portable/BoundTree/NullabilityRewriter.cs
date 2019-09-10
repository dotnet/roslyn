// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullabilityRewriter : BoundTreeRewriter
    {
        protected override BoundExpression? VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            return (BoundExpression)Visit(node);
        }

        public override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
        {
            return VisitBinaryOperatorBase(node);
        }

        public override BoundNode? VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            return VisitBinaryOperatorBase(node);
        }

        private BoundNode VisitBinaryOperatorBase(BoundBinaryOperatorBase binaryOperator)
        {
            // Use an explicit stack to avoid blowing the managed stack when visiting deeply-recursive
            // binary nodes
            var stack = ArrayBuilder<BoundBinaryOperatorBase>.GetInstance();
            BoundBinaryOperatorBase? currentBinary = binaryOperator;

            do
            {
                stack.Push(currentBinary);
                currentBinary = currentBinary.Left as BoundBinaryOperatorBase;
            }
            while (currentBinary is object);

            Debug.Assert(stack.Count > 0);
            var leftChild = (BoundExpression)Visit(stack.Peek().Left);

            do
            {
                currentBinary = stack.Pop();

                bool foundInfo = _updatedNullabilities.TryGetValue(currentBinary, out (NullabilityInfo Info, TypeSymbol Type) infoAndType);
                var right = (BoundExpression)Visit(currentBinary.Right);
                var type = foundInfo ? infoAndType.Type : currentBinary.Type;

                // https://github.com/dotnet/roslyn/issues/35031: We'll need to update the symbols for the internal methods/operators used in the binary operators
                currentBinary = currentBinary switch
                {
                    BoundBinaryOperator binary => binary.Update(binary.OperatorKind, binary.ConstantValueOpt, binary.MethodOpt, binary.ResultKind, binary.OriginalUserDefinedOperatorsOpt, leftChild, right, type),
                    BoundUserDefinedConditionalLogicalOperator logical => logical.Update(logical.OperatorKind, logical.LogicalOperator, logical.TrueOperator, logical.FalseOperator, logical.ResultKind, logical.OriginalUserDefinedOperatorsOpt, leftChild, right, type),
                    _ => throw ExceptionUtilities.UnexpectedValue(currentBinary.Kind),
                };

                if (foundInfo)
                {
                    currentBinary.TopLevelNullability = infoAndType.Info;
                }

                leftChild = currentBinary;
            }
            while (stack.Count > 0);

            Debug.Assert(currentBinary != null);
            return currentBinary!;
        }

        private T GetUpdatedSymbol<T>(BoundNode expr, T sym) where T : Symbol?
        {
            if (sym is null) return sym;

            if (_updatedSymbols.TryGetValue((expr, sym), out var updatedSymbol))
            {
                Debug.Assert(updatedSymbol is object);
                return (T)updatedSymbol;
            }

            return sym;
        }
    }
}
