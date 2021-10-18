// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisData = DictionaryAnalysisData<AnalysisEntity, InvocationCountAnalysisValue>;
    using InvocationCountAnalysisResult = DataFlowAnalysisResult<InvocationCountBlockAnalysisResult, InvocationCountAnalysisValue>;

    internal class InvocationCountDataFlowOperationVisitor : GlobalFlowStateDataFlowOperationVisitor<
        InvocationCountAnalysisData,
        InvocationCountAnalysisContext,
        InvocationCountAnalysisResult,
        InvocationCountAnalysisValue>
    {
        public InvocationCountDataFlowOperationVisitor(
            InvocationCountAnalysisContext analysisContext,
            bool hasPredicatedGlobalState) : base(analysisContext, hasPredicatedGlobalState)
        {
        }

        public override InvocationCountAnalysisData GetEmptyAnalysisData()
        {
            throw new NotImplementedException();
        }

        protected override void AddTrackedEntities(InvocationCountAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis = false)
        {
            throw new NotImplementedException();
        }

        protected override void ApplyInterproceduralAnalysisResultCore(InvocationCountAnalysisData resultData)
        {
            throw new NotImplementedException();
        }

        protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(InvocationCountAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
        {
            throw new NotImplementedException();
        }

        protected override bool Equals(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2)
        {
            throw new NotImplementedException();
        }

        protected override InvocationCountAnalysisValue GetAbstractDefaultValue(ITypeSymbol type)
        {
            throw new NotImplementedException();
        }

        protected override InvocationCountAnalysisValue GetAbstractValue(AnalysisEntity analysisEntity)
        {
            throw new NotImplementedException();
        }

        protected override InvocationCountAnalysisData GetClonedAnalysisData(InvocationCountAnalysisData analysisData)
        {
            throw new NotImplementedException();
        }

        protected override InvocationCountAnalysisData GetExitBlockOutputData(InvocationCountAnalysisResult analysisResult)
        {
            throw new NotImplementedException();
        }

        protected override InvocationCountAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
        {
            throw new NotImplementedException();
        }

        protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
        {
            throw new NotImplementedException();
        }

        protected override bool HasAnyAbstractValue(InvocationCountAnalysisData data)
        {
            throw new NotImplementedException();
        }

        protected override InvocationCountAnalysisData MergeAnalysisData(InvocationCountAnalysisData value1, InvocationCountAnalysisData value2)
        {
            throw new NotImplementedException();
        }

        protected override void ResetCurrentAnalysisData()
        {
            throw new NotImplementedException();
        }

        protected override void SetAbstractValue(AnalysisEntity analysisEntity, InvocationCountAnalysisValue value)
        {
            throw new NotImplementedException();
        }

        protected override void StopTrackingEntity(AnalysisEntity analysisEntity, InvocationCountAnalysisData analysisData)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateValuesForAnalysisData(InvocationCountAnalysisData targetAnalysisData)
        {
            throw new NotImplementedException();
        }
    }
}
