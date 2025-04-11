// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    public partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// An abstract analysis domain implementation for <see cref="PointsToAnalysisData"/> tracked by <see cref="PointsToAnalysis"/>.
        /// </summary>
        private sealed class PointsToAnalysisDomain : PredicatedAnalysisDataDomain<PointsToAnalysisData, PointsToAbstractValue>
        {
            public PointsToAnalysisDomain(DefaultPointsToValueGenerator defaultPointsToValueGenerator, Func<ITypeSymbol?, bool> isDisposable)
                : base(new CorePointsToAnalysisDataDomain(defaultPointsToValueGenerator, ValueDomainInstance, isDisposable))
            {
            }

            public PointsToAnalysisData MergeAnalysisDataForBackEdge(
                PointsToAnalysisData forwardEdgeAnalysisData,
                PointsToAnalysisData backEdgeAnalysisData,
                Func<PointsToAbstractValue, ImmutableHashSet<AnalysisEntity>> getChildAnalysisEntities,
                Action<AnalysisEntity, PointsToAnalysisData> resetAbstractValue,
                Func<ITypeSymbol?, bool> isDisposable)
            {
                if (!forwardEdgeAnalysisData.IsReachableBlockData && backEdgeAnalysisData.IsReachableBlockData)
                {
                    return (PointsToAnalysisData)backEdgeAnalysisData.Clone();
                }
                else if (!backEdgeAnalysisData.IsReachableBlockData && forwardEdgeAnalysisData.IsReachableBlockData)
                {
                    return (PointsToAnalysisData)forwardEdgeAnalysisData.Clone();
                }

                Debug.Assert(forwardEdgeAnalysisData.IsReachableBlockData == backEdgeAnalysisData.IsReachableBlockData);

                var mergedCoreAnalysisData = ((CorePointsToAnalysisDataDomain)CoreDataAnalysisDomain).MergeCoreAnalysisDataForBackEdge(
                    forwardEdgeAnalysisData,
                    backEdgeAnalysisData,
                    getChildAnalysisEntities,
                    resetAbstractValue);
                return new PointsToAnalysisData(mergedCoreAnalysisData, forwardEdgeAnalysisData,
                    backEdgeAnalysisData, forwardEdgeAnalysisData.IsReachableBlockData, CoreDataAnalysisDomain, isDisposable);
            }
        }
    }
}
