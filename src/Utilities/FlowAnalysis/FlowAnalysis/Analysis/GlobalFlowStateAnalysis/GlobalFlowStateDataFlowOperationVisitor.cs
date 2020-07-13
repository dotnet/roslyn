// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis.GlobalFlowStateAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;

    /// <summary>
    /// Operation visitor to flow the GlobalFlowState values across a given statement in a basic block.
    /// </summary>
    internal abstract class GlobalFlowStateDataFlowOperationVisitor
        : AnalysisEntityDataFlowOperationVisitor<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        // This is the global entity storing CFG wide state, which gets updated for every visited operation in the visitor.
        private readonly AnalysisEntity _globalEntity;
        private readonly bool _hasPredicatedGlobalState;

        protected GlobalFlowStateDataFlowOperationVisitor(GlobalFlowStateAnalysisContext analysisContext, bool hasPredicatedGlobalState)
            : base(analysisContext)
        {
            _globalEntity = GetGlobalEntity(analysisContext);
            _hasPredicatedGlobalState = hasPredicatedGlobalState;
        }

        private static AnalysisEntity GetGlobalEntity(GlobalFlowStateAnalysisContext analysisContext)
        {
            ISymbol owningSymbol;
            if (analysisContext.InterproceduralAnalysisDataOpt == null)
            {
                owningSymbol = analysisContext.OwningSymbol;
            }
            else
            {
                owningSymbol = analysisContext.InterproceduralAnalysisDataOpt.MethodsBeingAnalyzed
                    .Single(m => m.InterproceduralAnalysisDataOpt == null)
                    .OwningSymbol;
            }

            return AnalysisEntity.Create(
                owningSymbol,
                ImmutableArray<AbstractIndex>.Empty,
                owningSymbol.GetMemberOrLocalOrParameterType()!,
                instanceLocation: PointsToAbstractValue.Unknown,
                parentOpt: null);
        }

        public sealed override GlobalFlowStateAnalysisData Flow(IOperation statement, BasicBlock block, GlobalFlowStateAnalysisData input)
        {
            EnsureInitialized(input);
            return base.Flow(statement, block, input);
        }

        private void EnsureInitialized(GlobalFlowStateAnalysisData input)
        {
            if (input.Count == 0)
            {
                input[_globalEntity] = ValueDomain.Bottom;
            }
            else
            {
                Debug.Assert(input.ContainsKey(_globalEntity));
            }
        }

        protected GlobalFlowStateAnalysisValueSet GlobalState
        {
            get => GetAbstractValue(_globalEntity);
            set => SetAbstractValue(_globalEntity, value);
        }

        public sealed override (GlobalFlowStateAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, GlobalFlowStateAnalysisData input)
        {
            EnsureInitialized(input);
            var result = base.FlowBranch(fromBlock, branch, input);

            if (_hasPredicatedGlobalState &&
                branch.ControlFlowConditionKind != ControlFlowConditionKind.None &&
                branch.BranchValueOpt != null &&
                result.isFeasibleBranch)
            {
                var branchValue = GetCachedAbstractValue(branch.BranchValueOpt);
                var negate = branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse;
                MergeAndSetGlobalState(branchValue, negate);
            }

            return result;
        }

        protected void MergeAndSetGlobalState(GlobalFlowStateAnalysisValueSet value, bool negate = false)
        {
            Debug.Assert(_hasPredicatedGlobalState || !negate);

            if (!value.AnalysisValues.IsEmpty)
            {
                var currentGlobalValue = GetAbstractValue(_globalEntity);
                if (currentGlobalValue.Kind != GlobalFlowStateAnalysisValueSetKind.Unknown)
                {
                    var newGlobalValue = currentGlobalValue.WithAdditionalAnalysisValues(value, negate);
                    SetAbstractValue(_globalEntity, newGlobalValue);
                }
            }
        }

        protected sealed override void AddTrackedEntities(GlobalFlowStateAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
            => builder.UnionWith(analysisData.Keys);

        protected sealed override void ResetAbstractValue(AnalysisEntity analysisEntity)
            => SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);

        protected sealed override void StopTrackingEntity(AnalysisEntity analysisEntity, GlobalFlowStateAnalysisData analysisData)
            => analysisData.Remove(analysisEntity);

        protected sealed override GlobalFlowStateAnalysisValueSet GetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

        protected sealed override GlobalFlowStateAnalysisValueSet GetAbstractDefaultValue(ITypeSymbol type)
            => GlobalFlowStateAnalysisValueSet.Unset;

        protected sealed override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.ContainsKey(analysisEntity);

        protected sealed override bool HasAnyAbstractValue(GlobalFlowStateAnalysisData data)
            => data.Count > 0;

        protected sealed override void SetAbstractValue(AnalysisEntity analysisEntity, GlobalFlowStateAnalysisValueSet value)
        {
            Debug.Assert(_hasPredicatedGlobalState || value.Parents.IsEmpty);
            SetAbstractValue(CurrentAnalysisData, analysisEntity, value);
        }

        private static void SetAbstractValue(GlobalFlowStateAnalysisData analysisData, AnalysisEntity analysisEntity, GlobalFlowStateAnalysisValueSet value)
        {
            // PERF: Avoid creating an entry if the value is the default unknown value.
            if (value.Kind != GlobalFlowStateAnalysisValueSetKind.Known &&
                !analysisData.ContainsKey(analysisEntity))
            {
                return;
            }

            analysisData[analysisEntity] = value;
        }

        protected sealed override void ResetCurrentAnalysisData()
            => ResetAnalysisData(CurrentAnalysisData);

        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisData(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2)
            => GlobalFlowStateAnalysisDomainInstance.Merge(value1, value2);
        protected sealed override GlobalFlowStateAnalysisData MergeAnalysisData(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2, BasicBlock forBlock)
            => _hasPredicatedGlobalState && forBlock.DominatesPredecessors() ?
            GlobalFlowStateAnalysisDomainInstance.Intersect(value1, value2, GlobalFlowStateAnalysisValueSetDomain.Intersect) :
            GlobalFlowStateAnalysisDomainInstance.Merge(value1, value2);
        protected sealed override void UpdateValuesForAnalysisData(GlobalFlowStateAnalysisData targetAnalysisData)
            => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
        protected sealed override GlobalFlowStateAnalysisData GetClonedAnalysisData(GlobalFlowStateAnalysisData analysisData)
            => new GlobalFlowStateAnalysisData(analysisData);
        public override GlobalFlowStateAnalysisData GetEmptyAnalysisData()
            => new GlobalFlowStateAnalysisData();
        protected sealed override GlobalFlowStateAnalysisData GetExitBlockOutputData(GlobalFlowStateAnalysisResult analysisResult)
            => new GlobalFlowStateAnalysisData(analysisResult.ExitBlockOutput.Data);
        protected sealed override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(GlobalFlowStateAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);
        protected sealed override bool Equals(GlobalFlowStateAnalysisData value1, GlobalFlowStateAnalysisData value2)
            => GlobalFlowStateAnalysisDomainInstance.Equals(value1, value2);
        protected sealed override void ApplyInterproceduralAnalysisResultCore(GlobalFlowStateAnalysisData resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData);
        protected sealed override GlobalFlowStateAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);

        protected GlobalFlowStateAnalysisValueSet GetValueOrDefault(GlobalFlowStateAnalysisValueSet value)
            => value.Kind == GlobalFlowStateAnalysisValueSetKind.Known ? value : GlobalState;

        #region Visitor methods

        public override GlobalFlowStateAnalysisValueSet Visit(IOperation operation, object? argument)
        {
            var value = base.Visit(operation, argument);
            return GetValueOrDefault(value);
        }

        public override GlobalFlowStateAnalysisValueSet VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            GlobalFlowStateAnalysisValueSet defaultValue)
        {
            var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

            if (_hasPredicatedGlobalState &&
                IsAnyAssertMethod(method))
            {
                var argumentValue = GetCachedAbstractValue(visitedArguments[0]);
                MergeAndSetGlobalState(argumentValue);
            }

            return GetValueOrDefault(value);
        }

        public override GlobalFlowStateAnalysisValueSet VisitUnaryOperatorCore(IUnaryOperation operation, object? argument)
        {
            var value = base.VisitUnaryOperatorCore(operation, argument);
            if (_hasPredicatedGlobalState &&
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
