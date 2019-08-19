﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

#pragma warning disable IDE0055 // Fix formatting
                // https://github.com/dotnet/roslyn/issues/35031: We'll need to update the symbols for the internal methods/operators used in the binary operators
                currentBinary = currentBinary switch
                    {
                        BoundBinaryOperator binary => binary.Update(binary.OperatorKind, binary.ConstantValueOpt, binary.MethodOpt, binary.ResultKind, binary.OriginalUserDefinedOperatorsOpt, leftChild, right, type),
                        BoundUserDefinedConditionalLogicalOperator logical => logical.Update(logical.OperatorKind, logical.LogicalOperator, logical.TrueOperator, logical.FalseOperator, logical.ResultKind, logical.OriginalUserDefinedOperatorsOpt, leftChild, right, type),
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
            return currentBinary!;
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            BoundExpression? receiverOpt = (BoundExpression)this.Visit(node.ReceiverOpt);
            ImmutableArray<BoundExpression> arguments = this.VisitList(node.Arguments);
            BoundCall updatedNode;

            if (_updatedNullabilities.TryGetValue(node, out (NullabilityInfo Info, TypeSymbol Type) infoAndType) &&
                _updatedMethodSymbols.TryGetValue(node, out MethodSymbol updatedMethodSymbol))
            {
                updatedNode = node.Update(receiverOpt, updatedMethodSymbol, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.IsDelegateCall, node.Expanded, node.InvokedAsExtensionMethod, node.ArgsToParamsOpt, node.ResultKind, node.OriginalMethodsOpt, node.BinderOpt, infoAndType.Type);
                updatedNode.TopLevelNullability = infoAndType.Info;
            }
            else
            {
                updatedNode = node.Update(receiverOpt, node.Method, arguments, node.ArgumentNamesOpt, node.ArgumentRefKindsOpt, node.IsDelegateCall, node.Expanded, node.InvokedAsExtensionMethod, node.ArgsToParamsOpt, node.ResultKind, node.OriginalMethodsOpt, node.BinderOpt, node.Type);
            }
            return updatedNode;
        }
    }
}
