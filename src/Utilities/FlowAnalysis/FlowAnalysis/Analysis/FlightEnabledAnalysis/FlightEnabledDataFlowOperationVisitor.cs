// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis.FlightEnabledAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using FlightEnabledAnalysisData = DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisResult = DataFlowAnalysisResult<FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>;

    /// <summary>
    /// Operation visitor to flow the FlightEnabled values across a given statement in a basic block.
    /// </summary>
    internal abstract class FlightEnabledDataFlowOperationVisitor
        : AnalysisEntityDataFlowOperationVisitor<FlightEnabledAnalysisData, FlightEnabledAnalysisContext, FlightEnabledAnalysisResult, FlightEnabledAbstractValue>
    {
        // This is the global entity storing CFG wide state, which gets updated for every visited operation in the visitor.
        private readonly AnalysisEntity _globalEntity;
        private readonly bool _hasPredicatedGlobalState;

        protected FlightEnabledDataFlowOperationVisitor(FlightEnabledAnalysisContext analysisContext, bool hasPredicatedGlobalState)
            : base(analysisContext)
        {
            _globalEntity = GetGlobalEntity(analysisContext);
            _hasPredicatedGlobalState = hasPredicatedGlobalState;
        }

        private static AnalysisEntity GetGlobalEntity(FlightEnabledAnalysisContext analysisContext)
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

        public sealed override FlightEnabledAnalysisData Flow(IOperation statement, BasicBlock block, FlightEnabledAnalysisData input)
        {
            if (input.Count == 0)
            {
                input[_globalEntity] = ValueDomain.Bottom;
            }
            else
            {
                Debug.Assert(input.ContainsKey(_globalEntity));
            }

            return base.Flow(statement, block, input);
        }

        protected FlightEnabledAbstractValue GlobalState
        {
            get => GetAbstractValue(_globalEntity);
            set => SetAbstractValue(_globalEntity, value);
        }

        public sealed override (FlightEnabledAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, FlightEnabledAnalysisData input)
        {
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

        protected void MergeAndSetGlobalState(FlightEnabledAbstractValue value, bool negate = false)
        {
            Debug.Assert(_hasPredicatedGlobalState || !negate);

            if (value.EnabledFlights.Count > 0)
            {
                var currentGlobalValue = GetAbstractValue(_globalEntity);
                if (currentGlobalValue.Kind != FlightEnabledAbstractValueKind.Unknown)
                {
                    var newGlobalValue = currentGlobalValue.WithAdditionalEnabledFlights(value, negate);
                    SetAbstractValue(_globalEntity, newGlobalValue);
                }
            }
        }

        protected sealed override void AddTrackedEntities(FlightEnabledAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
            => builder.UnionWith(analysisData.Keys);

        protected sealed override void ResetAbstractValue(AnalysisEntity analysisEntity)
            => SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);

        protected sealed override void StopTrackingEntity(AnalysisEntity analysisEntity, FlightEnabledAnalysisData analysisData)
            => analysisData.Remove(analysisEntity);

        protected sealed override FlightEnabledAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

        protected sealed override FlightEnabledAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
            => FlightEnabledAbstractValue.Unset;

        protected sealed override bool HasAbstractValue(AnalysisEntity analysisEntity)
            => CurrentAnalysisData.ContainsKey(analysisEntity);

        protected sealed override bool HasAnyAbstractValue(FlightEnabledAnalysisData data)
            => data.Count > 0;

        protected sealed override void SetAbstractValue(AnalysisEntity analysisEntity, FlightEnabledAbstractValue value)
        {
            Debug.Assert(_hasPredicatedGlobalState || value.Parents.IsEmpty);
            SetAbstractValue(CurrentAnalysisData, analysisEntity, value);
        }

        private static void SetAbstractValue(FlightEnabledAnalysisData analysisData, AnalysisEntity analysisEntity, FlightEnabledAbstractValue value)
        {
            // PERF: Avoid creating an entry if the value is the default unknown value.
            if (value.Kind != FlightEnabledAbstractValueKind.Known &&
                !analysisData.ContainsKey(analysisEntity))
            {
                return;
            }

            analysisData[analysisEntity] = value;
        }

        protected sealed override void ResetCurrentAnalysisData()
            => ResetAnalysisData(CurrentAnalysisData);

        protected sealed override FlightEnabledAnalysisData MergeAnalysisData(FlightEnabledAnalysisData value1, FlightEnabledAnalysisData value2)
            => FlightEnabledAnalysisDomainInstance.Merge(value1, value2);
        protected sealed override FlightEnabledAnalysisData MergeAnalysisData(FlightEnabledAnalysisData value1, FlightEnabledAnalysisData value2, BasicBlock forBlock)
            => _hasPredicatedGlobalState && forBlock.DominatesPredecessors() ?
            FlightEnabledAnalysisDomainInstance.Intersect(value1, value2, FlightEnabledAbstractValueDomain.Intersect) :
            FlightEnabledAnalysisDomainInstance.Merge(value1, value2);
        protected sealed override void UpdateValuesForAnalysisData(FlightEnabledAnalysisData targetAnalysisData)
            => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
        protected sealed override FlightEnabledAnalysisData GetClonedAnalysisData(FlightEnabledAnalysisData analysisData)
            => new FlightEnabledAnalysisData(analysisData);
        public override FlightEnabledAnalysisData GetEmptyAnalysisData()
            => new FlightEnabledAnalysisData();
        protected sealed override FlightEnabledAnalysisData GetExitBlockOutputData(FlightEnabledAnalysisResult analysisResult)
            => new FlightEnabledAnalysisData(analysisResult.ExitBlockOutput.Data);
        protected sealed override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(FlightEnabledAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);
        protected sealed override bool Equals(FlightEnabledAnalysisData value1, FlightEnabledAnalysisData value2)
            => FlightEnabledAnalysisDomainInstance.Equals(value1, value2);
        protected sealed override void ApplyInterproceduralAnalysisResultCore(FlightEnabledAnalysisData resultData)
            => ApplyInterproceduralAnalysisResultHelper(resultData);
        protected sealed override FlightEnabledAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);

        protected FlightEnabledAbstractValue GetValueOrDefault(FlightEnabledAbstractValue value)
            => value.Kind == FlightEnabledAbstractValueKind.Known ? value : GlobalState;

        #region Visitor methods

        public override FlightEnabledAbstractValue Visit(IOperation operation, object? argument)
        {
            var value = base.Visit(operation, argument);
            return GetValueOrDefault(value);
        }

        public override FlightEnabledAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            IMethodSymbol method,
            IOperation? visitedInstance,
            ImmutableArray<IArgumentOperation> visitedArguments,
            bool invokedAsDelegate,
            IOperation originalOperation,
            FlightEnabledAbstractValue defaultValue)
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

        public override FlightEnabledAbstractValue VisitUnaryOperatorCore(IUnaryOperation operation, object? argument)
        {
            var value = base.VisitUnaryOperatorCore(operation, argument);
            if (_hasPredicatedGlobalState &&
                operation.OperatorKind == UnaryOperatorKind.Not &&
                value.Kind == FlightEnabledAbstractValueKind.Known)
            {
                return value.GetNegatedValue();
            }

            return value;
        }

        #endregion
    }
}
