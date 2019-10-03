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

                currentBinary = currentBinary switch
                {
                    BoundBinaryOperator binary => binary.Update(binary.OperatorKind, binary.ConstantValueOpt, GetUpdatedSymbol(binary, binary.MethodOpt), binary.ResultKind, binary.OriginalUserDefinedOperatorsOpt, leftChild, right, type),
                    // https://github.com/dotnet/roslyn/issues/35031: We'll need to update logical.LogicalOperator
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

            Symbol? updatedSymbol = null;
            if (_snapshotManager?.TryGetUpdatedSymbol(expr, sym, out updatedSymbol) != true)
            {
                updatedSymbol = sym;
            }
            // On merge, replace with RoslynDebug.Assert and remove suppression
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
                    updatedLambda = boundLambda.CreateLambdaSymbol(updatedContaining, lambda.ReturnTypeWithAnnotations, lambda.ParameterTypesWithAnnotations, lambda.ParameterRefKinds, lambda.RefKind);
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
