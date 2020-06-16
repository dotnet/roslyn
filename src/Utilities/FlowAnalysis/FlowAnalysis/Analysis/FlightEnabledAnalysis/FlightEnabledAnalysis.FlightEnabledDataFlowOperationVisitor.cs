// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using FlightEnabledAnalysisData = DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisDomain = MapAbstractDomain<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisResult = DataFlowAnalysisResult<FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>;

    internal partial class FlightEnabledAnalysis : ForwardDataFlowAnalysis<FlightEnabledAnalysisData, FlightEnabledAnalysisContext, FlightEnabledAnalysisResult, FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the FlightEnabled values across a given statement in a basic block.
        /// </summary>
        private sealed class FlightEnabledDataFlowOperationVisitor
            : AnalysisEntityDataFlowOperationVisitor<FlightEnabledAnalysisData, FlightEnabledAnalysisContext, FlightEnabledAnalysisResult, FlightEnabledAbstractValue>
        {
            // This is the global entity storing CFG wide state, which gets updated for every visited operation in the visitor.
            private readonly AnalysisEntity _globalEntity;

            public FlightEnabledDataFlowOperationVisitor(FlightEnabledAnalysisContext analysisContext)
                : base(analysisContext)
            {
                _globalEntity = GetGlobalEntity(analysisContext);
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

            public override FlightEnabledAnalysisData Flow(IOperation statement, BasicBlock block, FlightEnabledAnalysisData input)
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

            public override (FlightEnabledAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, FlightEnabledAnalysisData input)
            {
                var result = base.FlowBranch(fromBlock, branch, input);

                if (branch.ControlFlowConditionKind != ControlFlowConditionKind.None &&
                    branch.BranchValueOpt != null &&
                    result.isFeasibleBranch)
                {
                    var branchValue = GetCachedAbstractValue(branch.BranchValueOpt);
                    var negate = branch.ControlFlowConditionKind == ControlFlowConditionKind.WhenFalse;
                    EnableFlightsGlobally(branchValue, negate);
                }

                return result;
            }

            private void EnableFlightsGlobally(FlightEnabledAbstractValue value, bool negate)
            {
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

            protected override void AddTrackedEntities(FlightEnabledAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
                => builder.UnionWith(analysisData.Keys);

            protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
                => SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity, FlightEnabledAnalysisData analysisData)
                => analysisData.Remove(analysisEntity);

            protected override FlightEnabledAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
                => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : ValueDomain.UnknownOrMayBeValue;

            protected override FlightEnabledAbstractValue GetAbstractDefaultValue(ITypeSymbol type)
                => FlightEnabledAbstractValue.Unset;

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity)
                => CurrentAnalysisData.ContainsKey(analysisEntity);

            protected override bool HasAnyAbstractValue(FlightEnabledAnalysisData data)
                => data.Count > 0;

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, FlightEnabledAbstractValue value)
                => SetAbstractValue(CurrentAnalysisData, analysisEntity, value);

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

            protected override void ResetCurrentAnalysisData()
                => ResetAnalysisData(CurrentAnalysisData);

            protected override FlightEnabledAnalysisData MergeAnalysisData(FlightEnabledAnalysisData value1, FlightEnabledAnalysisData value2)
                => FlightEnabledAnalysisDomainInstance.Merge(value1, value2);
            protected override FlightEnabledAnalysisData MergeAnalysisData(FlightEnabledAnalysisData value1, FlightEnabledAnalysisData value2, BasicBlock forBlock)
                => forBlock.DominatesPredecessors() ?
                FlightEnabledAnalysisDomain.Intersect(value1, value2, FlightEnabledAbstractValueDomain.Intersect) :
                FlightEnabledAnalysisDomainInstance.Merge(value1, value2);
            protected override void UpdateValuesForAnalysisData(FlightEnabledAnalysisData targetAnalysisData)
                => UpdateValuesForAnalysisData(targetAnalysisData, CurrentAnalysisData);
            protected override FlightEnabledAnalysisData GetClonedAnalysisData(FlightEnabledAnalysisData analysisData)
                => new FlightEnabledAnalysisData(analysisData);
            public override FlightEnabledAnalysisData GetEmptyAnalysisData()
                => new FlightEnabledAnalysisData();
            protected override FlightEnabledAnalysisData GetExitBlockOutputData(FlightEnabledAnalysisResult analysisResult)
                => new FlightEnabledAnalysisData(analysisResult.ExitBlockOutput.Data);
            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(FlightEnabledAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
                => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException, CurrentAnalysisData, throwBranchWithExceptionType);
            protected override bool Equals(FlightEnabledAnalysisData value1, FlightEnabledAnalysisData value2)
                => FlightEnabledAnalysisDomainInstance.Equals(value1, value2);
            protected override void ApplyInterproceduralAnalysisResultCore(FlightEnabledAnalysisData resultData)
                => ApplyInterproceduralAnalysisResultHelper(resultData);
            protected override FlightEnabledAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
                => GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData, SetAbstractValue);

            private FlightEnabledAbstractValue GetValueOrDefault(FlightEnabledAbstractValue value)
                => value.Kind == FlightEnabledAbstractValueKind.Known ? value : CurrentAnalysisData[_globalEntity];

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

                if (DataFlowAnalysisContext.FlightEnablingMethods.Contains(method))
                {
                    var context = new FlightEnabledAnalysisCallbackContext(method, visitedArguments,
                        DataFlowAnalysisContext.PointsToAnalysisResultOpt, DataFlowAnalysisContext.ValueContentAnalysisResultOpt);
                    return DataFlowAnalysisContext.GetValueForFlightEnablingMethodInvocation(context);
                }
                else if (IsAnyAssertMethod(method))
                {
                    var argumentValue = GetCachedAbstractValue(visitedArguments[0]);
                    EnableFlightsGlobally(argumentValue, negate: false);
                }

                return GetValueOrDefault(value);
            }

            public override FlightEnabledAbstractValue VisitUnaryOperatorCore(IUnaryOperation operation, object? argument)
            {
                var value = base.VisitUnaryOperatorCore(operation, argument);
                if (operation.OperatorKind == UnaryOperatorKind.Not &&
                    value.Kind == FlightEnabledAbstractValueKind.Known)
                {
                    return value.GetNegatedValue();
                }

                return value;
            }

            #endregion
        }
    }
}
