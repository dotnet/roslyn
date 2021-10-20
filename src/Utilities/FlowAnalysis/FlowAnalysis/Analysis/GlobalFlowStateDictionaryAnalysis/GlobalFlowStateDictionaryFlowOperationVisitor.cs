// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.GlobalFlowStateDictionaryAnalysis
{
    using GlobalFlowStateDictionaryAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateDictionaryAnalysisValue>;
    using GlobalFlowStateDictionaryAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateDictionaryBlockAnalysisResult, GlobalFlowStateDictionaryAnalysisValue>;

    internal abstract class GlobalFlowStateDictionaryFlowOperationVisitor : GlobalFlowStateDataFlowOperationVisitor<
        GlobalFlowStateDictionaryAnalysisData,
        GlobalFlowStateDictionaryAnalysisContext,
        GlobalFlowStateDictionaryAnalysisResult,
        GlobalFlowStateDictionaryAnalysisValue>
    {
        protected GlobalFlowStateDictionaryFlowOperationVisitor(
            GlobalFlowStateDictionaryAnalysisContext analysisContext) : base(analysisContext, true)
        {
        }

        public override GlobalFlowStateDictionaryAnalysisData GetEmptyAnalysisData()
            => new();

        private void EnsureInitialized(GlobalFlowStateDictionaryAnalysisData input)
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

        public sealed override (GlobalFlowStateDictionaryAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, GlobalFlowStateDictionaryAnalysisData input)
        {
            EnsureInitialized(input);
            return base.FlowBranch(fromBlock, branch, input);
        }

        protected override void AddTrackedEntities(GlobalFlowStateDictionaryAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis = false)
            => builder.UnionWith(analysisData.Keys);

        protected override void ApplyInterproceduralAnalysisResultCore(GlobalFlowStateDictionaryAnalysisData resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData);

        protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(GlobalFlowStateDictionaryAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);

        protected override bool Equals(GlobalFlowStateDictionaryAnalysisData value1, GlobalFlowStateDictionaryAnalysisData value2)
            => GlobalFlowStateDictionaryAnalysis.Domain.Equals(value1, value2);

        protected override GlobalFlowStateDictionaryAnalysisValue GetAbstractDefaultValue(ITypeSymbol type)
            => GlobalFlowStateDictionaryAnalysisValue.Empty;

        protected override GlobalFlowStateDictionaryAnalysisValue GetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

        protected override GlobalFlowStateDictionaryAnalysisData GetClonedAnalysisData(GlobalFlowStateDictionaryAnalysisData analysisData)
            => new(analysisData);

        protected override GlobalFlowStateDictionaryAnalysisData GetExitBlockOutputData(GlobalFlowStateDictionaryAnalysisResult analysisResult)
            => new(analysisResult.EntryBlockOutput.Data);

        protected override GlobalFlowStateDictionaryAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);

        protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.ContainsKey(analysisEntity);

        protected override bool HasAnyAbstractValue(GlobalFlowStateDictionaryAnalysisData data)
            => data.Count > 0;

        protected override GlobalFlowStateDictionaryAnalysisData MergeAnalysisData(GlobalFlowStateDictionaryAnalysisData value1, GlobalFlowStateDictionaryAnalysisData value2)
            => GlobalFlowStateDictionaryAnalysis.Domain.Merge(value1, value2);

        protected sealed override GlobalFlowStateDictionaryAnalysisData MergeAnalysisData(GlobalFlowStateDictionaryAnalysisData value1, GlobalFlowStateDictionaryAnalysisData value2, BasicBlock forBlock)
            => HasPredicatedGlobalState && forBlock.DominatesPredecessors(DataFlowAnalysisContext.ControlFlowGraph) ?
            GlobalFlowStateDictionaryAnalysis.Domain.Intersect(value1, value2, GlobalFlowStateDictionaryAnalysisValueDomain.Intersect) :
            GlobalFlowStateDictionaryAnalysis.Domain.Merge(value1, value2);

        protected override GlobalFlowStateDictionaryAnalysisData MergeAnalysisDataForBackEdge(GlobalFlowStateDictionaryAnalysisData value1, GlobalFlowStateDictionaryAnalysisData value2, BasicBlock forBlock)
            => GlobalFlowStateDictionaryAnalysis.Domain.Merge(value1, value2);

        protected override void ResetCurrentAnalysisData()
            => ResetAnalysisData(CurrentAnalysisData);

        protected override void SetAbstractValue(AnalysisEntity analysisEntity, GlobalFlowStateDictionaryAnalysisValue value)
            => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

        protected override void StopTrackingEntity(AnalysisEntity analysisEntity, GlobalFlowStateDictionaryAnalysisData analysisData)
            => analysisData.Remove(analysisEntity);

        protected override void UpdateValuesForAnalysisData(GlobalFlowStateDictionaryAnalysisData targetAnalysisData)
            => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);

        protected static void SetAbstractValue(GlobalFlowStateDictionaryAnalysisData analysisData, AnalysisEntity analysisEntity, GlobalFlowStateDictionaryAnalysisValue value)
        {
            if (value.Kind == GlobalFlowStateDictionaryAnalysisValueKind.Known)
            {
                analysisData[analysisEntity] = value;
            }
        }
    }
}
