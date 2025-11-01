// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullabilityRewriter : BoundTreeRewriter
    {
        protected override BoundNode? VisitExpressionOrPatternWithoutStackGuard(BoundNode node)
        {
            return Visit(node);
        }

        public override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
        {
            return VisitBinaryOperatorBase(node);
        }

        public override BoundNode? VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            return VisitBinaryOperatorBase(node);
        }

        public override BoundNode? VisitIfStatement(BoundIfStatement node)
        {
            var stack = ArrayBuilder<(BoundIfStatement, BoundExpression, BoundStatement)>.GetInstance();

            BoundStatement? rewrittenAlternative;
            while (true)
            {
                var rewrittenCondition = (BoundExpression)Visit(node.Condition);
                var rewrittenConsequence = (BoundStatement)Visit(node.Consequence);
                Debug.Assert(rewrittenConsequence is { });
                stack.Push((node, rewrittenCondition, rewrittenConsequence));

                var alternative = node.AlternativeOpt;
                if (alternative is null)
                {
                    rewrittenAlternative = null;
                    break;
                }

                if (alternative is BoundIfStatement elseIfStatement)
                {
                    node = elseIfStatement;
                }
                else
                {
                    rewrittenAlternative = (BoundStatement)Visit(alternative);
                    break;
                }
            }

            BoundStatement result;
            do
            {
                var (ifStatement, rewrittenCondition, rewrittenConsequence) = stack.Pop();
                result = ifStatement.Update(rewrittenCondition, rewrittenConsequence, rewrittenAlternative);
                rewrittenAlternative = result;
            }
            while (stack.Any());

            stack.Free();
            return result;
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
            while (currentBinary is not null);

            Debug.Assert(stack.Count > 0);
            var leftChild = (BoundExpression)Visit(stack.Peek().Left);

            do
            {
                currentBinary = stack.Pop();

                bool foundInfo = _updatedNullabilities.TryGetValue(currentBinary, out (NullabilityInfo Info, TypeSymbol? Type) infoAndType);
                var right = (BoundExpression)Visit(currentBinary.Right);
                var type = foundInfo ? infoAndType.Type : currentBinary.Type;

                currentBinary = currentBinary switch
                {
                    BoundBinaryOperator binary => binary.Update(
                        binary.OperatorKind,
                        binary.BinaryOperatorMethod is { } binaryOperatorMethod ? binary.Data?.WithUpdatedMethod(GetUpdatedSymbol(binary, binaryOperatorMethod)) : binary.Data,
                        binary.ResultKind,
                        leftChild,
                        right,
                        type!),

                    BoundUserDefinedConditionalLogicalOperator logical => logical.Update(
                        logical.OperatorKind,
                        GetUpdatedSymbol(logical, logical.LogicalOperator),
                        logical.TrueOperator,
                        logical.FalseOperator,
                        logical.TrueFalseOperandPlaceholder,
                        logical.TrueFalseOperandConversion,
                        logical.ConstrainedToTypeOpt,
                        logical.ResultKind,
                        logical.OriginalUserDefinedOperatorsOpt,
                        leftChild,
                        right,
                        type!),
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

        public override BoundNode? VisitBinaryPattern(BoundBinaryPattern node)
        {
            // Use an explicit stack to avoid blowing the managed stack when visiting deeply-recursive
            // binary nodes
            var stack = ArrayBuilder<BoundBinaryPattern>.GetInstance();
            BoundBinaryPattern? currentBinary = node;

            do
            {
                stack.Push(currentBinary);
                currentBinary = currentBinary.Left as BoundBinaryPattern;
            }
            while (currentBinary is not null);

            Debug.Assert(stack.Count > 0);
            var leftChild = (BoundPattern)Visit(stack.Peek().Left);

            do
            {
                currentBinary = stack.Pop();

                TypeSymbol inputType = GetUpdatedSymbol(currentBinary, currentBinary.InputType);
                TypeSymbol narrowedType = GetUpdatedSymbol(currentBinary, currentBinary.NarrowedType);

                var right = (BoundPattern)Visit(currentBinary.Right);

                currentBinary = currentBinary.Update(currentBinary.Disjunction, leftChild, right, inputType, narrowedType);

                leftChild = currentBinary;
            }
            while (stack.Count > 0);

            return currentBinary;
        }

        public override BoundNode? VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt = GetUpdatedArray(node, node.OriginalUserDefinedOperatorsOpt);
            BoundExpression left = (BoundExpression)this.Visit(node.Left);
            BoundExpression right = (BoundExpression)this.Visit(node.Right);
            BoundValuePlaceholder? leftPlaceholder = node.LeftPlaceholder;
            BoundExpression? leftConversion = node.LeftConversion;
            BoundValuePlaceholder? finalPlaceholder = node.FinalPlaceholder;
            BoundExpression? finalConversion = node.FinalConversion;
            BoundCompoundAssignmentOperator updatedNode;

            var op = node.Operator;

            if (op.Method is not null)
            {
                op = new BinaryOperatorSignature(op.Kind, op.LeftType, op.RightType, op.ReturnType, GetUpdatedSymbol(node, op.Method), op.ConstrainedToTypeOpt);
            }

            if (_updatedNullabilities.TryGetValue(node, out (NullabilityInfo Info, TypeSymbol? Type) infoAndType))
            {
                updatedNode = node.Update(op, left, right, leftPlaceholder, leftConversion, finalPlaceholder, finalConversion, node.ResultKind, originalUserDefinedOperatorsOpt, infoAndType.Type!);
                updatedNode.TopLevelNullability = infoAndType.Info;
            }
            else
            {
                updatedNode = node.Update(op, left, right, leftPlaceholder, leftConversion, finalPlaceholder, finalConversion, node.ResultKind, originalUserDefinedOperatorsOpt, node.Type);
            }
            return updatedNode;
        }

        private T GetUpdatedSymbol<T>(BoundNode expr, T sym) where T : Symbol?
        {
            if (sym is null) return sym;

            Symbol? updatedSymbol = null;
            if (_snapshotManager?.TryGetUpdatedSymbol(expr, sym, out updatedSymbol) != true)
            {
                updatedSymbol = sym;
            }
            RoslynDebug.Assert(updatedSymbol is object);

            switch (updatedSymbol)
            {
                case LambdaSymbol lambda:
                    return (T)remapLambda((BoundLambda)expr, lambda);

                case SourceLocalSymbol local:
                    return (T)remapLocal(local);

                case ParameterSymbol param:
                    if (_remappedSymbols.TryGetValue(param, out var updatedParam))
                    {
                        return (T)updatedParam;
                    }
                    break;
            }

            return (T)updatedSymbol;

            Symbol remapLambda(BoundLambda boundLambda, LambdaSymbol lambda)
            {
                var updatedDelegateType = _snapshotManager?.GetUpdatedDelegateTypeForLambda(lambda);

                if (!_remappedSymbols.TryGetValue(lambda.ContainingSymbol, out Symbol? updatedContaining) && updatedDelegateType is null)
                {
                    return lambda;
                }

                LambdaSymbol updatedLambda;
                if (updatedDelegateType is null)
                {
                    Debug.Assert(updatedContaining is object);
                    updatedLambda = boundLambda.CreateLambdaSymbol(updatedContaining, lambda.ReturnTypeWithAnnotations, lambda.ParameterTypesWithAnnotations, lambda.ParameterRefKinds, lambda.RefKind, lambda.RefCustomModifiers);
                }
                else
                {
                    Debug.Assert(updatedDelegateType is object);
                    updatedLambda = boundLambda.CreateLambdaSymbol(updatedDelegateType, updatedContaining ?? lambda.ContainingSymbol);
                }

                _remappedSymbols.Add(lambda, updatedLambda);

                Debug.Assert(lambda.ParameterCount == updatedLambda.ParameterCount);
                for (int i = 0; i < lambda.ParameterCount; i++)
                {
                    _remappedSymbols.Add(lambda.Parameters[i], updatedLambda.Parameters[i]);
                }

                return updatedLambda;
            }

            Symbol remapLocal(SourceLocalSymbol local)
            {
                if (_remappedSymbols.TryGetValue(local, out var updatedLocal))
                {
                    return updatedLocal;
                }

                var updatedType = _snapshotManager?.GetUpdatedTypeForLocalSymbol(local);

                if (!_remappedSymbols.TryGetValue(local.ContainingSymbol, out Symbol? updatedContaining) && !updatedType.HasValue)
                {
                    // Map the local to itself so we don't have to search again in the future
                    _remappedSymbols.Add(local, local);
                    return local;
                }

                updatedLocal = new UpdatedContainingSymbolAndNullableAnnotationLocal(local, updatedContaining ?? local.ContainingSymbol, updatedType ?? local.TypeWithAnnotations);
                _remappedSymbols.Add(local, updatedLocal);
                return updatedLocal;
            }
        }

        public override BoundNode? VisitImplicitIndexerAccess(BoundImplicitIndexerAccess node)
        {
            BoundExpression receiver = (BoundExpression)this.Visit(node.Receiver);
            BoundExpression argument = (BoundExpression)this.Visit(node.Argument);
            BoundExpression lengthOrCountAccess = node.LengthOrCountAccess;
            BoundExpression indexerAccess = (BoundExpression)this.Visit(node.IndexerOrSliceAccess);
            BoundImplicitIndexerAccess updatedNode;

            if (_updatedNullabilities.TryGetValue(node, out (NullabilityInfo Info, TypeSymbol? Type) infoAndType))
            {
                updatedNode = node.Update(receiver, argument, lengthOrCountAccess, node.ReceiverPlaceholder, indexerAccess, node.ArgumentPlaceholders, infoAndType.Type!);
                updatedNode.TopLevelNullability = infoAndType.Info;
            }
            else
            {
                updatedNode = node.Update(receiver, argument, lengthOrCountAccess, node.ReceiverPlaceholder, indexerAccess, node.ArgumentPlaceholders, node.Type);
            }
            return updatedNode;
        }

        private ImmutableArray<T> GetUpdatedArray<T>(BoundNode expr, ImmutableArray<T> symbols) where T : Symbol?
        {
            if (symbols.IsDefaultOrEmpty)
            {
                return symbols;
            }

            var builder = ArrayBuilder<T>.GetInstance(symbols.Length);
            bool foundUpdate = false;
            foreach (var originalSymbol in symbols)
            {
                T updatedSymbol = null!;
                if (originalSymbol is object)
                {
                    updatedSymbol = GetUpdatedSymbol(expr, originalSymbol);
                    Debug.Assert(updatedSymbol is object);
                    if ((object)originalSymbol != updatedSymbol)
                    {
                        foundUpdate = true;
                    }
                }

                builder.Add(updatedSymbol);
            }

            if (foundUpdate)
            {
                return builder.ToImmutableAndFree();
            }
            else
            {
                builder.Free();
                return symbols;
            }
        }
    }
}
