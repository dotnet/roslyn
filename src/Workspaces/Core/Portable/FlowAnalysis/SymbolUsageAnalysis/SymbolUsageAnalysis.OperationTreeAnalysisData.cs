// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis
{
    internal static partial class SymbolUsageAnalysis
    {
        private sealed class OperationTreeAnalysisData : AnalysisData
        {
            private readonly Func<IMethodSymbol, BasicBlockAnalysisData> _analyzeLocalFunction;

            private OperationTreeAnalysisData(
                PooledDictionary<(ISymbol symbol, IOperation operation), bool> symbolsWriteMap,
                PooledHashSet<ISymbol> symbolsRead,
                PooledHashSet<IMethodSymbol> lambdaOrLocalFunctionsBeingAnalyzed,
                Func<IMethodSymbol, BasicBlockAnalysisData> analyzeLocalFunction)
                : base(symbolsWriteMap, symbolsRead, lambdaOrLocalFunctionsBeingAnalyzed)
            {
                _analyzeLocalFunction = analyzeLocalFunction;
            }

            public static OperationTreeAnalysisData Create(
                ISymbol owningSymbol,
                Func<IMethodSymbol, BasicBlockAnalysisData> analyzeLocalFunction)
            {
                return new OperationTreeAnalysisData(
                    symbolsWriteMap: CreateSymbolsWriteMap(owningSymbol.GetParameters()),
                    symbolsRead: PooledHashSet<ISymbol>.GetInstance(),
                    lambdaOrLocalFunctionsBeingAnalyzed: PooledHashSet<IMethodSymbol>.GetInstance(),
                    analyzeLocalFunction);
            }

            protected override BasicBlockAnalysisData AnalyzeLocalFunctionInvocationCore(IMethodSymbol localFunction, CancellationToken cancellationToken)
            {
                _ = UpdateSymbolsWriteMap(SymbolsWriteBuilder, localFunction.Parameters);
                return _analyzeLocalFunction(localFunction);
            }

            // Lambda target needs flow analysis, not supported/reachable in operation tree based analysis.
            protected override BasicBlockAnalysisData AnalyzeLambdaInvocationCore(IFlowAnonymousFunctionOperation lambda, CancellationToken cancellationToken)
                => throw ExceptionUtilities.Unreachable;

            public override bool IsLValueFlowCapture(CaptureId captureId)
                => throw ExceptionUtilities.Unreachable;
            public override bool IsRValueFlowCapture(CaptureId captureId)
                => throw ExceptionUtilities.Unreachable;
            public override void OnLValueCaptureFound(ISymbol symbol, IOperation operation, CaptureId captureId)
                => throw ExceptionUtilities.Unreachable;
            public override void OnLValueDereferenceFound(CaptureId captureId)
                => throw ExceptionUtilities.Unreachable;

            public override bool IsTrackingDelegateCreationTargets => false;
            public override void SetLambdaTargetForDelegate(IOperation write, IFlowAnonymousFunctionOperation lambdaTarget)
                => throw ExceptionUtilities.Unreachable;
            public override void SetLocalFunctionTargetForDelegate(IOperation write, IMethodReferenceOperation localFunctionTarget)
                => throw ExceptionUtilities.Unreachable;
            public override void SetEmptyInvocationTargetsForDelegate(IOperation write)
                => throw ExceptionUtilities.Unreachable;
            public override void SetTargetsFromSymbolForDelegate(IOperation write, ISymbol symbol)
                => throw ExceptionUtilities.Unreachable;
            public override bool TryGetDelegateInvocationTargets(IOperation write, out ImmutableHashSet<IOperation> targets)
                => throw ExceptionUtilities.Unreachable;
        }
    }
}
