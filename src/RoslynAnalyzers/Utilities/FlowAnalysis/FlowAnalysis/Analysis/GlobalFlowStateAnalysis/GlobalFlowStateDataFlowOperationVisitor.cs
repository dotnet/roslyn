// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    /// <summary>
    /// Operation visitor to flow the GlobalFlowState values across a given statement in a basic block.
    /// </summary>
    internal abstract class GlobalFlowStateDataFlowOperationVisitor<TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : AnalysisEntityDataFlowOperationVisitor<DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue>, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisContext : AbstractDataFlowAnalysisContext<DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue>, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TAbstractAnalysisValue : IEquatable<TAbstractAnalysisValue>
    {
        // This is the global entity storing CFG wide state, which gets updated for every visited operation in the visitor.
        protected AnalysisEntity GlobalEntity { get; }
        protected bool HasPredicatedGlobalState { get; }

        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue>.Builder _globalValuesMapBuilder;

        protected GlobalFlowStateDataFlowOperationVisitor(TAnalysisContext analysisContext, bool hasPredicatedGlobalState)
            : base(analysisContext)
        {
            GlobalEntity = GetGlobalEntity(analysisContext);
            HasPredicatedGlobalState = hasPredicatedGlobalState;
            _globalValuesMapBuilder = ImmutableDictionary.CreateBuilder<IOperation, TAbstractAnalysisValue>();
        }

        internal override bool SkipExceptionPathsAnalysisPostPass => true;

        protected abstract void SetAbstractValue(
            DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> analysisData, AnalysisEntity analysisEntity, TAbstractAnalysisValue value);

        internal ImmutableDictionary<IOperation, TAbstractAnalysisValue> GetGlobalValuesMap()
            => _globalValuesMapBuilder.ToImmutable();

        private static AnalysisEntity GetGlobalEntity(TAnalysisContext analysisContext)
        {
            ISymbol owningSymbol;
            if (analysisContext.InterproceduralAnalysisData == null)
            {
                owningSymbol = analysisContext.OwningSymbol;
            }
            else
            {
                owningSymbol = analysisContext.InterproceduralAnalysisData.MethodsBeingAnalyzed
                    .Single(m => m.InterproceduralAnalysisData == null)
                    .OwningSymbol;
            }

            return AnalysisEntity.Create(
                owningSymbol,
                ImmutableArray<AbstractIndex>.Empty,
                owningSymbol.GetMemberOrLocalOrParameterType()!,
                instanceLocation: PointsToAbstractValue.Unknown,
                parent: null,
                entityForInstanceLocation: null);
        }

        public sealed override DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> Flow(
            IOperation statement, BasicBlock block, DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> input)
        {
            EnsureInitialized(input);
            return base.Flow(statement, block, input);
        }

        private void EnsureInitialized(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> input)
        {
            if (input.Count == 0)
            {
                input[GlobalEntity] = ValueDomain.Bottom;
            }
            else
            {
                Debug.Assert(input.ContainsKey(GlobalEntity));
            }
        }

        protected TAbstractAnalysisValue GlobalState
        {
            get => GetAbstractValue(GlobalEntity);
            set => SetAbstractValue(GlobalEntity, value);
        }

        public override (DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> output, bool isFeasibleBranch) FlowBranch(
            BasicBlock fromBlock, BranchWithInfo branch, DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> input)
        {
            EnsureInitialized(input);
            return base.FlowBranch(fromBlock, branch, input);
        }

        public override DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> GetEmptyAnalysisData()
            => [];

        protected sealed override DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> GetClonedAnalysisData(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> analysisData)
            => [.. analysisData];

        protected sealed override void AddTrackedEntities(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
            => builder.UnionWith(analysisData.Keys);

        protected sealed override void StopTrackingEntity(AnalysisEntity analysisEntity, DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> analysisData)
            => analysisData.Remove(analysisEntity);

        protected sealed override TAbstractAnalysisValue GetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

        protected override void SetAbstractValue(AnalysisEntity analysisEntity, TAbstractAnalysisValue value)
            => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

        protected sealed override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.ContainsKey(analysisEntity);

        protected sealed override bool HasAnyAbstractValue(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> data)
            => data.Count > 0;

        protected sealed override DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);

        protected sealed override void ResetAbstractValue(AnalysisEntity analysisEntity)
            => SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);

        protected sealed override void ResetCurrentAnalysisData()
            => ResetAnalysisData(CurrentAnalysisData);

        protected sealed override void UpdateValuesForAnalysisData(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> targetAnalysisData)
            => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);

        protected sealed override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);

        protected sealed override void ApplyInterproceduralAnalysisResultCore(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData);

        protected override DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> GetInitialInterproceduralAnalysisData(
            IMethodSymbol invokedMethod,
            (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? invocationInstance,
            (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCaller,
            ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> argumentValuesMap,
            IDictionary<AnalysisEntity, PointsToAbstractValue>? pointsToValues,
            IDictionary<AnalysisEntity, CopyAbstractValue>? copyValues,
            IDictionary<AnalysisEntity, ValueContentAbstractValue>? valueContentValues,
            bool isLambdaOrLocalFunction,
            bool hasParameterWithDelegateType)
            => GetClonedCurrentAnalysisData();

        #region Visitor methods

        public override TAbstractAnalysisValue Visit(IOperation? operation, object? argument)
        {
            var value = base.Visit(operation, argument);

            if (operation != null)
            {
                // Store the current global value in a separate global values builder.
                // These values need to be saved into the base operation value builder in the final analysis result.
                // This will be done as a post-step after the analysis is complete.
                _globalValuesMapBuilder[operation] = GlobalState;
            }

            return value;
        }

        #endregion
    }
}
