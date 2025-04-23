// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis.GlobalFlowStateAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;

    /// <summary>
    /// Operation visitor to flow the GlobalFlowState values across a given statement in a basic block.
    /// </summary>
    internal abstract class GlobalFlowStateValueSetFlowOperationVisitor
        : GlobalFlowStateDataFlowOperationVisitor<GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        protected GlobalFlowStateValueSetFlowOperationVisitor(GlobalFlowStateAnalysisContext analysisContext, bool hasPredicatedGlobalState)
            : base(analysisContext, hasPredicatedGlobalState)
        {
        }

        public sealed override (GlobalFlowStateAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, GlobalFlowStateAnalysisData input)
        {
            var result = base.FlowBranch(fromBlock, branch, input);

            if (HasPredicatedGlobalState &&
                branch.ControlFlowConditionKind != ControlFlowConditionKind.None &&
                branch.BranchValue != null &&
                result.isFeasibleBranch)
            {
                var branchValue = GetCachedAbstractValue(branch.BranchValue);
                var negate = branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse;
                MergeAndSetGlobalState(branchValue, negate);
            }

            return result;
        }

        protected void MergeAndSetGlobalState(GlobalFlowStateAnalysisValueSet value, bool negate = false)
        {
            Debug.Assert(HasPredicatedGlobalState || !negate);

            if (value.Kind == GlobalFlowStateAnalysisValueSetKind.Known)
            {
                var currentGlobalValue = GetAbstractValue(GlobalEntity);
                if (currentGlobalValue.Kind != GlobalFlowStateAnalysisValueSetKind.Unknown)
                {
                    var newGlobalValue = currentGlobalValue.WithAdditionalAnalysisValues(value, negate);
                    SetAbstractValue(GlobalEntity, newGlobalValue);
                }
            }
        }

        protected sealed override GlobalFlowStateAnalysisValueSet GetAbstractDefaultValue(ITypeSymbol? type)
            => GlobalFlowStateAnalysisValueSet.Unset;

        protected sealed override void SetAbstractValue(AnalysisEntity analysisEntity, GlobalFlowStateAnalysisValueSet value)
        {
            Debug.Assert(HasPredicatedGlobalState || value.Parents.IsEmpty);
            base.SetAbstractValue(analysisEntity, value);
        }

        protected override void SetAbstractValue(GlobalFlowStateAnalysisData analysisData, AnalysisEntity analysisEntity, GlobalFlowStateAnalysisValueSet value)
        {
            // PERF: Avoid creating an entry if the value is the default unknown value.
            if (value.Kind != GlobalFlowStateAnalysisValueSetKind.Known &&
                !analysisData.ContainsKey(analysisEntity))
            {
                return;
            }

            analysisData[analysisEntity] = value;
        }

        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisData(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2)
            => GlobalFlowStateAnalysisDomainInstance.Merge(value1, value2);

        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisData(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2, BasicBlock forBlock)
            => HasPredicatedGlobalState && forBlock.DominatesPredecessors(DataFlowAnalysisContext.ControlFlowGraph) ?
            GlobalFlowStateAnalysisDomainInstance.Intersect(value1, value2, GlobalFlowStateAnalysisValueSetDomain.Intersect) :
            GlobalFlowStateAnalysisDomainInstance.Merge(value1, value2);

        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisDataForBackEdge(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2, BasicBlock forBlock)
        {
            // If we are merging analysis data for back edge, we have done at least one analysis pass for the block
            // and should replace 'Unset' value with 'Empty' value for the next pass.
            if (value1.TryGetValue(GlobalEntity, out var value) && value == GlobalFlowStateAnalysisValueSet.Unset)
                value1[GlobalEntity] = GlobalFlowStateAnalysisValueSet.Empty;

            if (value2.TryGetValue(GlobalEntity, out value) && value == GlobalFlowStateAnalysisValueSet.Unset)
                value2[GlobalEntity] = GlobalFlowStateAnalysisValueSet.Empty;

            return base.MergeAnalysisDataForBackEdge(value1, value2, forBlock);
        }

        protected sealed override GlobalFlowStateAnalysisData GetExitBlockOutputData(GlobalFlowStateAnalysisResult analysisResult)
            => [.. analysisResult.ExitBlockOutput.Data];

        protected sealed override bool Equals(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2)
            => GlobalFlowStateAnalysisDomainInstance.Equals(value1, value2);

        #region Visitor methods

        public override GlobalFlowStateAnalysisValueSet VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            GlobalFlowStateAnalysisValueSet defaultValue)
        {
            var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

            if (HasPredicatedGlobalState &&
                IsAnyAssertMethod(method))
            {
                var argumentValue = GetCachedAbstractValue(visitedArguments[0]);
                MergeAndSetGlobalState(argumentValue);
            }

            return value;
        }

        public override GlobalFlowStateAnalysisValueSet VisitUnaryOperatorCore(IUnaryOperation operation, object? argument)
        {
            var value = base.VisitUnaryOperatorCore(operation, argument);
            if (HasPredicatedGlobalState &&
                operation.OperatorKind == UnaryOperatorKind.Not &&
                value.Kind == GlobalFlowStateAnalysisValueSetKind.Known)
            {
                return value.GetNegatedValue();
            }

            return value;
        }

        #endregion
    }
}
