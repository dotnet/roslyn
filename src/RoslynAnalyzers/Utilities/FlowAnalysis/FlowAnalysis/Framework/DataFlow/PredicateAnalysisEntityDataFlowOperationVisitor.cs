// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Predicate analysis based Operation visitor to flow the abstract dataflow analysis values for <see cref="AnalysisEntity"/> instances across a given statement in a basic block.
    /// </summary>
    public abstract class PredicateAnalysisEntityDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : AnalysisEntityDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TAbstractAnalysisValue : IEquatable<TAbstractAnalysisValue>
    {
        protected PredicateAnalysisEntityDataFlowOperationVisitor(TAnalysisContext analysisContext)
            : base(analysisContext)
        {
        }

        #region Predicate analysis
        protected sealed override bool SupportsPredicateAnalysis => true;

        protected override void UpdateReachability(BasicBlock basicBlock, TAnalysisData analysisData, bool isReachable)
        {
            Debug.Assert(PredicateAnalysis);
            if (analysisData is AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue> predicatedData)
            {
                Debug.Assert(!isReachable || predicatedData.IsReachableBlockData);
                predicatedData.IsReachableBlockData = isReachable;
            }
        }

        protected override bool IsReachableBlockData(TAnalysisData analysisData)
            => analysisData?.IsReachableBlockData ?? true;

        protected sealed override void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, TAnalysisData? truePredicateData, TAnalysisData? falsePredicateData)
                => CurrentAnalysisData?.StartTrackingPredicatedData(
                        predicatedEntity,
                        truePredicateData,
                        falsePredicateData);
        protected sealed override void StopTrackingPredicatedData(AnalysisEntity predicatedEntity)
            => CurrentAnalysisData?.StopTrackingPredicatedData(predicatedEntity);
        protected sealed override bool HasPredicatedDataForEntity(TAnalysisData analysisData, AnalysisEntity predicatedEntity)
            => analysisData?.HasPredicatedDataForEntity(predicatedEntity) == true;
        protected sealed override void TransferPredicatedData(AnalysisEntity fromEntity, AnalysisEntity toEntity)
            => CurrentAnalysisData?.TransferPredicatedData(fromEntity, toEntity);
        protected sealed override PredicateValueKind ApplyPredicatedDataForEntity(TAnalysisData analysisData, AnalysisEntity predicatedEntity, bool trueData)
            => analysisData?.ApplyPredicatedDataForEntity(predicatedEntity, trueData) ?? PredicateValueKind.Unknown;
        protected override void SetPredicateValueKind(IOperation operation, TAnalysisData analysisData, PredicateValueKind predicateValueKind)
        {
            base.SetPredicateValueKind(operation, analysisData, predicateValueKind);
            if (predicateValueKind == PredicateValueKind.AlwaysFalse)
            {
                analysisData.IsReachableBlockData = false;
            }
        }
        #endregion
    }
}
