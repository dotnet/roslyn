// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    public partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the PointsTo values across a given statement in a basic block.
        /// </summary>
        private sealed class PointsToDataFlowOperationVisitor :
            AnalysisEntityDataFlowOperationVisitor<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToAbstractValue>
        {
            private readonly TrackedEntitiesBuilder _trackedEntitiesBuilder;
            private readonly DefaultPointsToValueGenerator _defaultPointsToValueGenerator;
            private readonly PointsToAnalysisDomain _pointsToAnalysisDomain;
            private readonly PooledDictionary<IOperation, ImmutableHashSet<AbstractLocation>.Builder> _escapedOperationLocationsBuilder;
            private readonly PooledDictionary<IOperation, ImmutableHashSet<AbstractLocation>.Builder> _escapedReturnValueLocationsBuilder;
            private readonly PooledDictionary<AnalysisEntity, ImmutableHashSet<AbstractLocation>.Builder> _escapedEntityLocationsBuilder;

            public PointsToDataFlowOperationVisitor(
                TrackedEntitiesBuilder trackedEntitiesBuilder,
                DefaultPointsToValueGenerator defaultPointsToValueGenerator,
                PointsToAnalysisDomain pointsToAnalysisDomain,
                PointsToAnalysisContext analysisContext)
                : base(analysisContext)
            {
                _trackedEntitiesBuilder = trackedEntitiesBuilder;
                _defaultPointsToValueGenerator = defaultPointsToValueGenerator;
                _pointsToAnalysisDomain = pointsToAnalysisDomain;
                _escapedOperationLocationsBuilder = PooledDictionary<IOperation, ImmutableHashSet<AbstractLocation>.Builder>.GetInstance();
                _escapedReturnValueLocationsBuilder = PooledDictionary<IOperation, ImmutableHashSet<AbstractLocation>.Builder>.GetInstance();
                _escapedEntityLocationsBuilder = PooledDictionary<AnalysisEntity, ImmutableHashSet<AbstractLocation>.Builder>.GetInstance();

                analysisContext.InterproceduralAnalysisDataOpt?.InitialAnalysisData.AssertValidPointsToAnalysisData();
            }

            public ImmutableDictionary<IOperation, ImmutableHashSet<AbstractLocation>> GetEscapedLocationsThroughOperationsMap()
                => GetEscapedAbstractLocationsMapAndFreeBuilder(_escapedOperationLocationsBuilder);

            public ImmutableDictionary<IOperation, ImmutableHashSet<AbstractLocation>> GetEscapedLocationsThroughReturnValuesMap()
                => GetEscapedAbstractLocationsMapAndFreeBuilder(_escapedReturnValueLocationsBuilder);

            public ImmutableDictionary<AnalysisEntity, ImmutableHashSet<AbstractLocation>> GetEscapedLocationsThroughEntitiesMap()
                => GetEscapedAbstractLocationsMapAndFreeBuilder(_escapedEntityLocationsBuilder);

            private static ImmutableDictionary<T, ImmutableHashSet<AbstractLocation>> GetEscapedAbstractLocationsMapAndFreeBuilder<T>(
                PooledDictionary<T, ImmutableHashSet<AbstractLocation>.Builder> escapedLocationsBuilder)
                where T : class
            {
                try
                {
                    if (escapedLocationsBuilder.Count == 0)
                    {
                        return ImmutableDictionary<T, ImmutableHashSet<AbstractLocation>>.Empty;
                    }

                    var builder = ImmutableDictionary.CreateBuilder<T, ImmutableHashSet<AbstractLocation>>();
                    foreach ((var key, var valueBuilder) in escapedLocationsBuilder)
                    {
                        builder.Add(key, valueBuilder.ToImmutable());
                    }

                    return builder.ToImmutable();
                }
                finally
                {
                    escapedLocationsBuilder.Free();
                }
            }

            public override PointsToAnalysisData Flow(IOperation statement, BasicBlock block, PointsToAnalysisData input)
            {
                AssertValidPointsToAnalysisData(input);
#if DEBUG
                if (input != null &&
                    block.Kind == BasicBlockKind.Exit)
                {
                    // No flow capture entities should be alive at the end of flow graph.
                    input.AssertNoFlowCaptureEntitiesTracked();
                }
#endif

                // Ensure PointsTo value is set for the "this" or "Me" instance.
                if (input != null &&
                    !HasAbstractValue(AnalysisEntityFactory.ThisOrMeInstance) &&
                    ShouldBeTracked(AnalysisEntityFactory.ThisOrMeInstance))
                {
                    input.SetAbstractValue(AnalysisEntityFactory.ThisOrMeInstance, ThisOrMePointsToAbstractValue);
                }

                var output = base.Flow(statement, block, input);
                AssertValidPointsToAnalysisData(output);
                return output;
            }

            public override (PointsToAnalysisData output, bool isFeasibleBranch) FlowBranch(BasicBlock fromBlock, BranchWithInfo branch, PointsToAnalysisData input)
            {
                AssertValidPointsToAnalysisData(input);
                (PointsToAnalysisData output, bool isFeasibleBranch) result = base.FlowBranch(fromBlock, branch, input);
                AssertValidPointsToAnalysisData(result.output);
                return result;
            }

            protected override void AddTrackedEntities(PointsToAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
            {
                if (!analysisData.HasAnyAbstractValue &&
                    (forInterproceduralAnalysis || !_defaultPointsToValueGenerator.HasAnyTrackedEntity))
                {
                    return;
                }

                foreach (var entity in _trackedEntitiesBuilder.AllEntities)
                {
                    if (analysisData.HasAbstractValue(entity) ||
                        !forInterproceduralAnalysis && _defaultPointsToValueGenerator.IsTrackedEntity(entity))
                    {
                        builder.Add(entity);
                    }
                }
            }

            protected override bool IsPointsToAnalysis => true;

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity, PointsToAnalysisData analysisData)
                => analysisData.RemoveEntries(analysisEntity);

            protected override PointsToAbstractValue GetAbstractValue(AnalysisEntity analysisEntity)
            {
                if (!ShouldBeTracked(analysisEntity))
                {
                    Debug.Assert(!CurrentAnalysisData.TryGetValue(analysisEntity, out var existingValue) || existingValue == PointsToAbstractValue.NoLocation);
                    return PointsToAbstractValue.NoLocation;
                }

                if (!CurrentAnalysisData.TryGetValue(analysisEntity, out var value))
                {
                    value = _defaultPointsToValueGenerator.GetOrCreateDefaultValue(analysisEntity);
                }

                return value;
            }

            protected override PointsToAbstractValue GetPointsToAbstractValue(IOperation operation) => base.GetCachedAbstractValue(operation);

            protected override PointsToAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => !ShouldBeTracked(type) ? PointsToAbstractValue.NoLocation : PointsToAbstractValue.NullLocation;

            protected override bool HasAnyAbstractValue(PointsToAnalysisData data) => data.HasAnyAbstractValue;

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, PointsToAbstractValue value)
            {
                Debug.Assert(ShouldBeTracked(analysisEntity) || !HasAbstractValue(analysisEntity));

                if (ShouldBeTracked(analysisEntity))
                {
                    if (value.Kind == PointsToAbstractValueKind.Undefined)
                    {
                        Debug.Assert(value == _defaultPointsToValueGenerator.GetOrCreateDefaultValue(analysisEntity));
                        Debug.Assert(!CurrentAnalysisData.TryGetValue(analysisEntity, out var currentValue) ||
                                     currentValue.Kind == PointsToAbstractValueKind.Unknown &&
                                     (analysisEntity.SymbolOpt as IParameterSymbol)?.RefKind == RefKind.Out);
                        return;
                    }

                    SetAbstractValueCore(CurrentAnalysisData, analysisEntity, value);
                    _trackedEntitiesBuilder.AllEntities.Add(analysisEntity);
                }
            }

            private static void SetAbstractValueCore(PointsToAnalysisData pointsToAnalysisData, AnalysisEntity analysisEntity, PointsToAbstractValue value)
                => pointsToAnalysisData.SetAbstractValue(analysisEntity, value);

            protected override void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, PointsToAbstractValue assignedValue)
            {
                if (assignedValue == PointsToAbstractValue.Undefined)
                {
                    return;
                }

                base.SetAbstractValueForTupleElementAssignment(tupleElementEntity, assignedValueOperation, assignedValue);
            }

            protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
            {
                if (analysisEntity.IsLValueFlowCaptureEntity)
                {
                    // Flow captures can never be re-assigned.
                    return;
                }

                SetAbstractValue(analysisEntity, PointsToAbstractValue.Unknown);
            }

            private static void ResetAbstractValueIfTracked(AnalysisEntity analysisEntity, PointsToAnalysisData pointsToAnalysisData)
            {
                if (pointsToAnalysisData.TryGetValue(analysisEntity, out var currentValue))
                {
                    pointsToAnalysisData.SetAbstractValue(analysisEntity, GetResetValue(analysisEntity, currentValue));
                }
            }

            private static void SetAbstractValueFromPredicate(
                AnalysisEntity analysisEntity,
                NullAbstractValue nullState,
                DefaultPointsToValueGenerator defaultPointsToValueGenerator,
                PointsToAnalysisData sourceAnalysisData,
                PointsToAnalysisData targetAnalysisData)
            {
                AssertValidPointsToAnalysisData(sourceAnalysisData);
                AssertValidPointsToAnalysisData(targetAnalysisData);

                Debug.Assert(IsValidValueForPredicateAnalysis(nullState) || nullState == NullAbstractValue.Invalid);

                if (!sourceAnalysisData.TryGetValue(analysisEntity, out PointsToAbstractValue existingValue))
                {
                    existingValue = defaultPointsToValueGenerator.GetOrCreateDefaultValue(analysisEntity);
                }

                var newPointsToValue = nullState switch
                {
                    NullAbstractValue.Null => existingValue.MakeNull(),

                    NullAbstractValue.NotNull => existingValue.MakeNonNull(),

                    NullAbstractValue.Invalid => PointsToAbstractValue.Invalid,

                    _ => throw new InvalidProgramException(),
                };

                targetAnalysisData.SetAbstractValue(analysisEntity, newPointsToValue);
                AssertValidPointsToAnalysisData(targetAnalysisData);
            }

            // Create a dummy PointsTo value for each reference type parameter.
            protected override PointsToAbstractValue GetDefaultValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity)
                => ShouldBeTracked(parameter.Type) ?
                    PointsToAbstractValue.Create(
                        AbstractLocation.CreateSymbolLocation(parameter, DataFlowAnalysisContext.InterproceduralAnalysisDataOpt?.CallStack),
                        mayBeNull: !parameter.IsParams) :
                    PointsToAbstractValue.NoLocation;

            protected override void EscapeValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                // Mark PointsTo values for ref/out parameters in non-interprocedural context as escaped.
                if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out)
                {
                    Debug.Assert(DataFlowAnalysisContext.InterproceduralAnalysisDataOpt == null);
                    var pointsToValue = GetAbstractValue(analysisEntity);
                    HandleEscapingLocations(analysisEntity, _escapedEntityLocationsBuilder, analysisEntity, pointsToValue);
                }
            }

            private static PointsToAbstractValue GetResetValue(AnalysisEntity analysisEntity, PointsToAbstractValue currentValue)
            {
                if (analysisEntity.IsLValueFlowCaptureEntity)
                {
                    // LValue flow capture PointsToAbstractValue can never change.
                    return currentValue;
                }

                return PointsToAbstractValue.Unknown;
            }

            protected override void ResetCurrentAnalysisData() => CurrentAnalysisData.Reset(GetResetValue);

            protected override PointsToAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, PointsToAbstractValue defaultValue)
            {
                if (ShouldBeTracked(operation.Type) &&
                    AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return GetAbstractValue(analysisEntity);
                }
                else
                {
                    Debug.Assert(operation.Type == null || !operation.Type.IsNonNullableValueType() || defaultValue == PointsToAbstractValue.NoLocation);
                    return defaultValue;
                }
            }

            protected override PointsToAbstractValue ComputeAnalysisValueForEscapedRefOrOutArgument(AnalysisEntity analysisEntity, IArgumentOperation operation, PointsToAbstractValue defaultValue)
            {
                Debug.Assert(operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out);

                if (!ShouldBeTracked(analysisEntity))
                {
                    return PointsToAbstractValue.NoLocation;
                }

                var location = AbstractLocation.CreateAllocationLocation(operation, analysisEntity.Type, DataFlowAnalysisContext);
                return PointsToAbstractValue.Create(location, mayBeNull: true);
            }

            protected override void PostProcessArgument(IArgumentOperation operation, bool isEscaped)
            {
                base.PostProcessArgument(operation, isEscaped);

                if (!isEscaped)
                {
                    // Update abstract value for unescaped ref or out argument (interprocedural analysis case).
                    if ((operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out) &&
                        AnalysisEntityFactory.TryCreate(operation, out var analysisEntity))
                    {
                        CacheAbstractValue(operation, GetAbstractValue(analysisEntity));

                        if (analysisEntity.SymbolOpt?.Kind == SymbolKind.Field)
                        {
                            // Ref/Out field argument is considered escaped.
                            HandleEscapingOperation(operation, operation);
                        }
                    }
                }
                else if (operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out)
                {
                    if (operation.Parameter.RefKind == RefKind.Ref)
                    {
                        // Input by-ref argument passed to invoked method is considered escaped in non-interprocedural analysis case.
                        HandleEscapingOperation(operation, operation.Value);
                    }

                    // Output by-ref or out argument might be escaped if assigned to a field.
                    HandlePossibleEscapingForAssignment(target: operation.Value, value: operation, operation: operation);
                }
            }

            protected override void ProcessReturnValue(IOperation returnValue)
            {
                base.ProcessReturnValue(returnValue);

                // Escape the return value if we are not analyzing an invoked method during interprocedural analysis.
                if (returnValue != null &&
                    DataFlowAnalysisContext.InterproceduralAnalysisDataOpt == null)
                {
                    HandleEscapingOperation(escapingOperation: returnValue, escapedInstance: returnValue, _escapedReturnValueLocationsBuilder);
                }
            }

            private protected override PointsToAbstractValue GetAbstractValueForImplicitWrappingTaskCreation(IOperation returnValueOperation, PointsToAbstractValue returnValue, PointsToAbstractValue implicitTaskPointsToValue)
            {
                return implicitTaskPointsToValue;
            }

            #region Predicate analysis
            private static bool IsValidValueForPredicateAnalysis(NullAbstractValue value)
            {
                switch (value)
                {
                    case NullAbstractValue.Null:
                    case NullAbstractValue.NotNull:
                        return true;

                    default:
                        return false;
                }
            }

            protected override PredicateValueKind SetValueForEqualsOrNotEqualsComparisonOperator(
                IOperation leftOperand,
                IOperation rightOperand,
                bool equals,
                bool isReferenceEquality,
                PointsToAnalysisData targetAnalysisData)
            {
                var predicateValueKind = PredicateValueKind.Unknown;

                // Handle "a == null" and "a != null"
                if (SetValueForNullCompare(leftOperand, rightOperand, equals, ref predicateValueKind, targetAnalysisData))
                {
                    return predicateValueKind;
                }

                // Otherwise, handle "null == a" and "null != a"
                SetValueForNullCompare(rightOperand, leftOperand, equals, ref predicateValueKind, targetAnalysisData);
                return predicateValueKind;
            }

            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, PointsToAnalysisData targetAnalysisData)
            {
                var predicateValueKind = PredicateValueKind.Unknown;
                SetValueForNullCompare(leftOperand, value: NullAbstractValue.Null, equals: equals, predicateValueKind: ref predicateValueKind, targetAnalysisData: targetAnalysisData);
                return predicateValueKind;
            }

            private bool SetValueForNullCompare(
                IOperation target,
                IOperation assignedValueOperation,
                bool equals,
                ref PredicateValueKind predicateValueKind,
                PointsToAnalysisData targetAnalysisData)
            {
                NullAbstractValue value = GetNullAbstractValue(assignedValueOperation);
                return SetValueForNullCompare(target, value, equals, ref predicateValueKind, targetAnalysisData);
            }

            private bool SetValueForNullCompare(
                IOperation target,
                NullAbstractValue value,
                bool equals,
                ref PredicateValueKind predicateValueKind,
                PointsToAnalysisData targetAnalysisData)
            {
                if (IsValidValueForPredicateAnalysis(value) &&
                    AnalysisEntityFactory.TryCreate(target, out AnalysisEntity targetEntity) &&
                    ShouldBeTracked(targetEntity))
                {
                    // Comparison with a non-null value guarantees that we can infer result in only one of the branches.
                    // For example, predicate "a == c", where we know 'c' is non-null, guarantees 'a' is non-null in CurrentAnalysisData,
                    bool inferInTargetAnalysisData = !(value == NullAbstractValue.NotNull && !equals);

                    CopyAbstractValue copyValue = GetCopyAbstractValue(target);
                    if (copyValue.Kind.IsKnown())
                    {
                        foreach (var analysisEntity in copyValue.AnalysisEntities)
                        {
                            SetValueForNullCompareFromPredicate(analysisEntity, value, equals, inferInTargetAnalysisData,
                                ref predicateValueKind, _defaultPointsToValueGenerator,
                                sourceAnalysisData: CurrentAnalysisData, targetAnalysisData: targetAnalysisData);
                        }
                    }
                    else
                    {
                        SetValueForNullCompareFromPredicate(targetEntity, value, equals, inferInTargetAnalysisData,
                            ref predicateValueKind, _defaultPointsToValueGenerator,
                            sourceAnalysisData: CurrentAnalysisData, targetAnalysisData: targetAnalysisData);
                    }

                    return true;
                }

                return false;
            }

            private static void SetValueForNullCompareFromPredicate(
                AnalysisEntity key,
                NullAbstractValue value,
                bool equals,
                bool inferInTargetAnalysisData,
                ref PredicateValueKind predicateValueKind,
                DefaultPointsToValueGenerator defaultPointsToValueGenerator,
                PointsToAnalysisData sourceAnalysisData,
                PointsToAnalysisData targetAnalysisData)
            {
                if (!ShouldBeTracked(key))
                {
                    Debug.Assert(!targetAnalysisData.HasAbstractValue(key));
                    Debug.Assert(!sourceAnalysisData.HasAbstractValue(key));
                    return;
                }

                // Compute the negated value.
                NullAbstractValue negatedValue = NegatePredicateValue(value);

                // Check if the key already has an existing "Null" or "NotNull" NullState that would make the condition always true or false.
                // If so, set the predicateValueKind to always true/false, set the value in branch that can never be taken to NullAbstractValue.Invalid
                // and turn off value inference in one of the branch.
                if (sourceAnalysisData.TryGetValue(key, out PointsToAbstractValue existingPointsToValue))
                {
                    NullAbstractValue existingNullValue = existingPointsToValue.NullState;
                    if (IsValidValueForPredicateAnalysis(existingNullValue) &&
                        (existingNullValue == NullAbstractValue.Null || value == NullAbstractValue.Null))
                    {
                        if (value == existingNullValue && equals ||
                            negatedValue == existingNullValue && !equals)
                        {
                            predicateValueKind = PredicateValueKind.AlwaysTrue;
                            negatedValue = NullAbstractValue.Invalid;
                            inferInTargetAnalysisData = false;
                        }

                        if (negatedValue == existingNullValue && equals ||
                            value == existingNullValue && !equals)
                        {
                            predicateValueKind = PredicateValueKind.AlwaysFalse;
                            value = NullAbstractValue.Invalid;
                        }
                    }
                }

                // Swap value and negatedValue if we are processing not-equals operator.
                if (!equals)
                {
                    if (value != NullAbstractValue.Invalid && negatedValue != NullAbstractValue.Invalid)
                    {
                        value = negatedValue;
                    }
                }

                if (inferInTargetAnalysisData)
                {
                    // Set value for the CurrentAnalysisData.
                    SetAbstractValueFromPredicate(key, value, defaultPointsToValueGenerator, sourceAnalysisData, targetAnalysisData);
                }
            }

            private static NullAbstractValue NegatePredicateValue(NullAbstractValue value)
            {
                Debug.Assert(IsValidValueForPredicateAnalysis(value));

                return value switch
                {
                    NullAbstractValue.Null => NullAbstractValue.NotNull,

                    NullAbstractValue.NotNull => NullAbstractValue.Null,

                    _ => throw new InvalidProgramException(),
                };
            }
            #endregion

            protected override PointsToAnalysisData MergeAnalysisData(PointsToAnalysisData value1, PointsToAnalysisData value2)
                => _pointsToAnalysisDomain.Merge(value1, value2);
            protected override PointsToAnalysisData MergeAnalysisDataForBackEdge(PointsToAnalysisData value1, PointsToAnalysisData value2)
                => _pointsToAnalysisDomain.MergeAnalysisDataForBackEdge(value1, value2, GetChildAnalysisEntities, ResetAbstractValueIfTracked);
            protected override void UpdateValuesForAnalysisData(PointsToAnalysisData targetAnalysisData)
                => UpdateValuesForAnalysisData(targetAnalysisData.CoreAnalysisData, CurrentAnalysisData.CoreAnalysisData);
            protected override PointsToAnalysisData GetClonedAnalysisData(PointsToAnalysisData analysisData)
                => (PointsToAnalysisData)analysisData.Clone();
            public override PointsToAnalysisData GetEmptyAnalysisData()
                => new PointsToAnalysisData();
            protected override PointsToAnalysisData GetExitBlockOutputData(PointsToAnalysisResult analysisResult)
                => new PointsToAnalysisData(analysisResult.ExitBlockOutput.Data);
            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(PointsToAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
                => ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(dataAtException.CoreAnalysisData, CurrentAnalysisData.CoreAnalysisData, throwBranchWithExceptionType);
            protected override void AssertValidAnalysisData(PointsToAnalysisData analysisData)
                => AssertValidPointsToAnalysisData(analysisData);
            protected override bool Equals(PointsToAnalysisData value1, PointsToAnalysisData value2)
                => value1.Equals(value2);

            protected override void ApplyInterproceduralAnalysisResultCore(PointsToAnalysisData resultData)
            {
                ApplyInterproceduralAnalysisResultHelper(resultData.CoreAnalysisData);
                AssertValidPointsToAnalysisData(CurrentAnalysisData);
            }

            protected override PointsToAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            {
                var trimmedData = GetTrimmedCurrentAnalysisDataHelper(withEntities, CurrentAnalysisData.CoreAnalysisData, SetAbstractValueCore);
                AssertValidPointsToAnalysisData(trimmedData);
                return trimmedData;
            }

            protected override PointsToAnalysisData GetInitialInterproceduralAnalysisData(
                IMethodSymbol invokedMethod,
                (AnalysisEntity InstanceOpt, PointsToAbstractValue PointsToValue)? invocationInstanceOpt,
                (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCallerOpt,
                ImmutableDictionary<IParameterSymbol, ArgumentInfo<PointsToAbstractValue>> argumentValuesMap,
                IDictionary<AnalysisEntity, PointsToAbstractValue> pointsToValuesOpt,
                IDictionary<AnalysisEntity, CopyAbstractValue> copyValuesOpt,
                IDictionary<AnalysisEntity, ValueContentAbstractValue> valueContentValuesOpt,
                bool isLambdaOrLocalFunction,
                bool hasParameterWithDelegateType)
            {
                pointsToValuesOpt = CurrentAnalysisData.CoreAnalysisData;
                var initialAnalysisData = base.GetInitialInterproceduralAnalysisData(invokedMethod,
                    invocationInstanceOpt, thisOrMeInstanceForCallerOpt, argumentValuesMap, pointsToValuesOpt,
                    copyValuesOpt, valueContentValuesOpt, isLambdaOrLocalFunction, hasParameterWithDelegateType);
                AssertValidPointsToAnalysisData(initialAnalysisData);
                return initialAnalysisData;
            }

            private void HandleEscapingOperation(IOperation escapingOperation, IOperation escapedInstance)
            {
                HandleEscapingOperation(escapingOperation, escapedInstance, _escapedOperationLocationsBuilder);
            }

            private void HandleEscapingOperation(IOperation escapingOperation, IOperation escapedInstance, PooledDictionary<IOperation, ImmutableHashSet<AbstractLocation>.Builder> builder)
            {
                Debug.Assert(escapingOperation != null);
                Debug.Assert(escapedInstance != null);

                PointsToAbstractValue escapedInstancePointsToValue = GetPointsToAbstractValue(escapedInstance);
                if (escapedInstancePointsToValue.Kind == PointsToAbstractValueKind.KnownLValueCaptures)
                {
                    foreach (var capturedOperation in escapedInstancePointsToValue.LValueCapturedOperations)
                    {
                        HandleEscapingOperation(escapingOperation, capturedOperation, builder);
                    }

                    return;
                }

                AnalysisEntityFactory.TryCreate(escapedInstance, out var escapedEntityOpt);
                HandleEscapingLocations(escapingOperation, builder, escapedEntityOpt, escapedInstancePointsToValue);
            }

            private void HandleEscapingLocations<TKey>(
                TKey key,
                PooledDictionary<TKey, ImmutableHashSet<AbstractLocation>.Builder> escapedLocationsBuilder,
                AnalysisEntity escapedEntityOpt,
                PointsToAbstractValue escapedInstancePointsToValue)
                where TKey : class
            {
                Debug.Assert(key != null);

                // Start by clearing escaped locations from previous flow analysis iterations.
                if (escapedLocationsBuilder.TryGetValue(key, out var builder))
                {
                    builder.Clear();
                }

                HandleEscapingLocations(key, escapedLocationsBuilder, escapedInstancePointsToValue);

                // For value type entities, we also need to handle escaping the locations for child entities.
                if (escapedEntityOpt?.Type.HasValueCopySemantics() == true)
                {
                    HandleEscapingLocations(key, escapedLocationsBuilder, escapedEntityOpt.InstanceLocation);
                }
            }

            private void HandleEscapingLocations<TKey>(
                TKey key,
                PooledDictionary<TKey, ImmutableHashSet<AbstractLocation>.Builder> escapedLocationsBuilder,
                PointsToAbstractValue pointsToValueOfEscapedInstance)
                where TKey : class
            {
                if (pointsToValueOfEscapedInstance.Locations.Count == 0 ||
                    pointsToValueOfEscapedInstance == PointsToAbstractValue.NoLocation ||
                    pointsToValueOfEscapedInstance == PointsToAbstractValue.NullLocation)
                {
                    return;
                }

                if (!escapedLocationsBuilder.TryGetValue(key, out var builder))
                {
                    builder = ImmutableHashSet.CreateBuilder<AbstractLocation>();
                    escapedLocationsBuilder.Add(key, builder);
                }

                HandleEscapingLocations(pointsToValueOfEscapedInstance, builder);
                foreach (var childEntity in GetChildAnalysisEntities(pointsToValueOfEscapedInstance))
                {
                    var pointsToValueOfEscapedChild = GetAbstractValue(childEntity);
                    HandleEscapingLocations(pointsToValueOfEscapedChild, builder);
                }

                if (TryGetTaskWrappedValue(pointsToValueOfEscapedInstance, out var wrappedValue))
                {
                    HandleEscapingLocations(key, escapedLocationsBuilder, wrappedValue);
                }
            }

            private static void HandleEscapingLocations(PointsToAbstractValue pointsToValueOfEscapedInstance, ImmutableHashSet<AbstractLocation>.Builder builder)
            {
                foreach (var escapedLocation in pointsToValueOfEscapedInstance.Locations)
                {
                    // Only escape locations associated with creations.
                    // We can expand this for more cases in future if need arises.
                    if (escapedLocation.CreationOpt != null)
                    {
                        builder.Add(escapedLocation);
                    }
                }
            }

            private void HandlePossibleEscapingForAssignment(IOperation target, IOperation value, IOperation operation)
            {
                // FxCop compat: The object assigned to a field or a property or an array element is considered escaped.
                // TODO: Perform better analysis for array element assignments as we already track element locations.
                // https://github.com/dotnet/roslyn-analyzers/issues/1577
                if (target is IMemberReferenceOperation ||
                    target.Kind == OperationKind.ArrayElementReference)
                {
                    HandleEscapingOperation(operation, value);
                }
            }

            protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, PointsToAbstractValue assignedValue, bool mayBeAssignment = false)
            {
                base.SetAbstractValueForAssignment(target, assignedValueOperation, assignedValue, mayBeAssignment);

                if (assignedValueOperation != null)
                {
                    HandlePossibleEscapingForAssignment(target, assignedValueOperation, assignedValueOperation);
                }
            }

            protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, PointsToAbstractValue value)
            {
                base.SetAbstractValueForArrayElementInitializer(arrayCreation, indices, elementType, initializer, value);

                // We use the array initializer as the escaping operation instead of arrayCreation
                // to ensure we have a unique escaping operation key for each initializer.
                HandleEscapingOperation(initializer, initializer);
            }

            #region Visitor methods

            public override PointsToAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                _ = base.DefaultVisit(operation, argument);

                // Special handling for:
                //  1. Null value: NullLocation
                //  2. Constants and value types do not point to any location.
                if (operation.ConstantValue.HasValue)
                {
                    if (operation.Type == null ||
                        operation.ConstantValue.Value == null)
                    {
                        return PointsToAbstractValue.NullLocation;
                    }
                    else
                    {
                        return PointsToAbstractValue.NoLocation;
                    }
                }
                else if (operation.Type.IsNonNullableValueType())
                {
                    return PointsToAbstractValue.NoLocation;
                }

                return ValueDomain.UnknownOrMayBeValue;
            }

            public override PointsToAbstractValue VisitIsType(IIsTypeOperation operation, object argument)
            {
                _ = base.VisitIsType(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            public override PointsToAbstractValue VisitInstanceReference(IInstanceReferenceOperation operation, object argument)
            {
                _ = base.VisitInstanceReference(operation, argument);
                IOperation currentInstanceOperation = operation.GetInstance(IsInsideAnonymousObjectInitializer);
                var value = currentInstanceOperation != null ?
                    GetCachedAbstractValue(currentInstanceOperation) :
                    ThisOrMePointsToAbstractValue;
                Debug.Assert(value.NullState == NullAbstractValue.NotNull || DataFlowAnalysisContext.InterproceduralAnalysisDataOpt != null);
                return value;
            }

            private PointsToAbstractValue VisitTypeCreationWithArgumentsAndInitializer<TOperation>(
                TOperation operation,
                object argument,
                Func<TOperation, object, PointsToAbstractValue> baseVisit)
                where TOperation : IOperation
            {
                AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type, DataFlowAnalysisContext);
                var pointsToAbstractValue = PointsToAbstractValue.Create(location, mayBeNull: false);
                CacheAbstractValue(operation, pointsToAbstractValue);

                _ = baseVisit(operation, argument);

                return pointsToAbstractValue;
            }

            public override PointsToAbstractValue VisitObjectCreation(IObjectCreationOperation operation, object argument)
            {
                return VisitTypeCreationWithArgumentsAndInitializer(operation, argument, base.VisitObjectCreation);
            }

            public override PointsToAbstractValue VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation, object argument)
            {
                return VisitTypeCreationWithArgumentsAndInitializer(operation, argument, base.VisitDynamicObjectCreation);
            }

            public override PointsToAbstractValue VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation, object argument)
            {
                return VisitTypeCreationWithArgumentsAndInitializer(operation, argument, base.VisitAnonymousObjectCreation);
            }

            public override PointsToAbstractValue VisitTuple(ITupleOperation operation, object argument)
            {
                var type = operation.Type.GetUnderlyingValueTupleTypeOrThis();
                AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, type, DataFlowAnalysisContext);
                var pointsToAbstractValue = PointsToAbstractValue.Create(location, mayBeNull: false);
                CacheAbstractValue(operation, pointsToAbstractValue);

                _ = base.VisitTuple(operation, argument);
                return pointsToAbstractValue;
            }

            public override PointsToAbstractValue VisitDelegateCreation(IDelegateCreationOperation operation, object argument)
            {
                _ = base.VisitDelegateCreation(operation, argument);
                AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type, DataFlowAnalysisContext);
                return PointsToAbstractValue.Create(location, mayBeNull: false);
            }

            public override PointsToAbstractValue VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation, object argument)
            {
                return VisitTypeCreationWithArgumentsAndInitializer(operation, argument, base.VisitTypeParameterObjectCreation);
            }

            public override PointsToAbstractValue VisitArrayInitializer(IArrayInitializerOperation operation, object argument)
            {
                _ = base.VisitArrayInitializer(operation, argument);

                // We should have created a new PointsTo value for the associated array creation operation in non-error code.
                // Bail out otherwise.
                var arrayCreation = operation.GetAncestor<IArrayCreationOperation>(OperationKind.ArrayCreation);
                return arrayCreation != null ? GetCachedAbstractValue(arrayCreation) : ValueDomain.UnknownOrMayBeValue;
            }

            public override PointsToAbstractValue VisitArrayCreation(IArrayCreationOperation operation, object argument)
            {
                var pointsToAbstractValue = PointsToAbstractValue.Create(AbstractLocation.CreateAllocationLocation(operation, operation.Type, DataFlowAnalysisContext), mayBeNull: false);
                CacheAbstractValue(operation, pointsToAbstractValue);

                _ = VisitArray(operation.DimensionSizes, argument);
                var initializerValue = Visit(operation.Initializer, argument);
                Debug.Assert(operation.Initializer == null || initializerValue == pointsToAbstractValue);
                return pointsToAbstractValue;
            }

            public override PointsToAbstractValue VisitInterpolatedString(IInterpolatedStringOperation operation, object argument)
            {
                _ = base.VisitInterpolatedString(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            public override PointsToAbstractValue VisitBinaryOperatorCore(IBinaryOperation operation, object argument)
            {
                _ = base.VisitBinaryOperatorCore(operation, argument);
                return PointsToAbstractValue.Unknown;
            }

            public override PointsToAbstractValue VisitSizeOf(ISizeOfOperation operation, object argument)
            {
                _ = base.VisitSizeOf(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            public override PointsToAbstractValue VisitTypeOf(ITypeOfOperation operation, object argument)
            {
                _ = base.VisitTypeOf(operation, argument);
                return PointsToAbstractValue.NoLocation;
            }

            private PointsToAbstractValue VisitInvocationCommon(IOperation operation, IOperation instance)
            {
                if (ShouldBeTracked(operation.Type))
                {
                    if (TryGetInterproceduralAnalysisResult(operation, out var interproceduralResult))
                    {
                        return interproceduralResult.ReturnValueAndPredicateKindOpt.Value.Value;
                    }

                    AbstractLocation location = AbstractLocation.CreateAllocationLocation(operation, operation.Type, DataFlowAnalysisContext);
                    var pointsToAbstractValue = PointsToAbstractValue.Create(location, mayBeNull: true);
                    return GetValueBasedOnInstanceOrReferenceValue(referenceOrInstance: instance, operation: operation, defaultValue: pointsToAbstractValue);
                }
                else
                {
                    return PointsToAbstractValue.NoLocation;
                }
            }

            public override PointsToAbstractValue VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
                IMethodSymbol method,
                IOperation visitedInstance,
                ImmutableArray<IArgumentOperation> visitedArguments,
                bool invokedAsDelegate,
                IOperation originalOperation,
                PointsToAbstractValue defaultValue)
            {
                _ = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(method, visitedInstance, visitedArguments, invokedAsDelegate, originalOperation, defaultValue);

                if (visitedArguments.Length > 0 &&
                    method.IsCollectionAddMethod(CollectionNamedTypes))
                {
                    // FxCop compat: The object added to a collection is considered escaped.
                    var lastArgument = visitedArguments[visitedArguments.Length - 1];
                    HandleEscapingOperation(originalOperation, lastArgument.Value);
                }

                var value = VisitInvocationCommon(originalOperation, visitedInstance);

                if (IsSpecialEmptyOrFactoryMethod(method) &&
                    !TryGetInterproceduralAnalysisResult(originalOperation, out _))
                {
                    return value.MakeNonNull();
                }

                return value;
            }

            private static bool IsSpecialEmptyOrFactoryMethod(IMethodSymbol method)
                => IsSpecialFactoryMethod(method) || IsSpecialEmptyMember(method);

            /// <summary>
            /// Returns true if this special static factory method whose name starts with "Create", such that
            /// method's containing type is static OR a special type OR derives from or is same as the type of the field/property/method return.
            /// For example: class SomeType { static SomeType CreateXXX(...); }
            /// </summary>
            private static bool IsSpecialFactoryMethod(IMethodSymbol method)
            {
                return method.IsStatic &&
                    method.Name.StartsWith("Create", StringComparison.Ordinal) &&
                    (method.ContainingType.IsStatic ||
                     method.ContainingType.SpecialType != SpecialType.None ||
                     method.ReturnType is INamedTypeSymbol namedType &&
                     method.ContainingType.DerivesFromOrImplementsAnyConstructionOf(namedType.OriginalDefinition));
            }

            /// <summary>
            /// Returns true if this special member symbol named "Empty", such that one of the following is true:
            ///  1. It is a static method with no parameters or
            ///  2. It is a static readonly property or
            ///  3. It is static readonly field
            /// and symbol's containing type is a special type or derives from or is same as the type of the field/property/method return.
            /// For example:
            ///  1. class SomeType { static readonly SomeType Empty; }
            ///  2. class SomeType { static readonly SomeType Empty { get; } }
            ///  3. class SomeType { static SomeType Empty(); }
            /// </summary>
            private static bool IsSpecialEmptyMember(ISymbol symbol)
            {
                return symbol.IsStatic &&
                    symbol.Name.Equals("Empty", StringComparison.Ordinal) &&
                    (symbol.IsReadOnlyFieldOrProperty() || symbol.Kind == SymbolKind.Method) &&
                    (symbol.ContainingType.SpecialType != SpecialType.None ||
                     symbol.GetMemberType() is INamedTypeSymbol namedType &&
                     symbol.ContainingType.DerivesFromOrImplementsAnyConstructionOf(namedType.OriginalDefinition));
            }

            public override PointsToAbstractValue VisitInvocation_LocalFunction(
                IMethodSymbol localFunction,
                ImmutableArray<IArgumentOperation> visitedArguments,
                IOperation originalOperation,
                PointsToAbstractValue defaultValue)
            {
                _ = base.VisitInvocation_LocalFunction(localFunction, visitedArguments, originalOperation, defaultValue);
                return VisitInvocationCommon(originalOperation, instance: null);
            }

            public override PointsToAbstractValue VisitInvocation_Lambda(
                IFlowAnonymousFunctionOperation lambda,
                ImmutableArray<IArgumentOperation> visitedArguments,
                IOperation originalOperation,
                PointsToAbstractValue defaultValue)
            {
                _ = base.VisitInvocation_Lambda(lambda, visitedArguments, originalOperation, defaultValue);
                return VisitInvocationCommon(originalOperation, instance: null);
            }

            public override PointsToAbstractValue VisitDynamicInvocation(IDynamicInvocationOperation operation, object argument)
            {
                _ = base.VisitDynamicInvocation(operation, argument);
                return VisitInvocationCommon(operation, operation.Operation);
            }

            private NullAbstractValue GetNullStateBasedOnInstanceOrReferenceValue(IOperation referenceOrInstance, ITypeSymbol operationType, NullAbstractValue defaultValue)
            {
                if (operationType.IsNonNullableValueType())
                {
                    return NullAbstractValue.NotNull;
                }

                NullAbstractValue referenceOrInstanceValue = referenceOrInstance != null ? GetNullAbstractValue(referenceOrInstance) : NullAbstractValue.NotNull;
                switch (referenceOrInstanceValue)
                {
                    case NullAbstractValue.Invalid:
                    case NullAbstractValue.Null:
                        return referenceOrInstanceValue;

                    default:
                        return defaultValue;
                }
            }

            private PointsToAbstractValue GetValueBasedOnInstanceOrReferenceValue(IOperation referenceOrInstance, IOperation operation, PointsToAbstractValue defaultValue)
            {
                NullAbstractValue nullState = GetNullStateBasedOnInstanceOrReferenceValue(referenceOrInstance, operation.Type, defaultValue.NullState);
                return nullState switch
                {
                    NullAbstractValue.NotNull => defaultValue.MakeNonNull(),

                    NullAbstractValue.Null => defaultValue.MakeNull(),

                    NullAbstractValue.Invalid => PointsToAbstractValue.Invalid,

                    _ => defaultValue,
                };
            }

            public override PointsToAbstractValue VisitFieldReference(IFieldReferenceOperation operation, object argument)
            {
                var value = base.VisitFieldReference(operation, argument);

                // "class SomeType { static readonly SomeType Empty; }"
                if (IsSpecialEmptyMember(operation.Field) &&
                    value.NullState != NullAbstractValue.Null)
                {
                    return value.MakeNonNull();
                }

                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitPropertyReference(IPropertyReferenceOperation operation, object argument)
            {
                var value = base.VisitPropertyReference(operation, argument);

                // "class SomeType { static SomeType Empty { get; } }"
                if (IsSpecialEmptyMember(operation.Property) &&
                    value.NullState != NullAbstractValue.Null)
                {
                    return value.MakeNonNull();
                }

                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation, object argument)
            {
                var value = base.VisitDynamicMemberReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitMethodReference(IMethodReferenceOperation operation, object argument)
            {
                var value = base.VisitMethodReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitEventReference(IEventReferenceOperation operation, object argument)
            {
                var value = base.VisitEventReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Instance, operation, value);
            }

            public override PointsToAbstractValue VisitArrayElementReference(IArrayElementReferenceOperation operation, object argument)
            {
                var value = base.VisitArrayElementReference(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.ArrayReference, operation, value);
            }

            public override PointsToAbstractValue VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation, object argument)
            {
                var value = base.VisitDynamicIndexerAccess(operation, argument);
                return GetValueBasedOnInstanceOrReferenceValue(operation.Operation, operation, value);
            }

            public override PointsToAbstractValue VisitConversion(IConversionOperation operation, object argument)
            {
                var value = base.VisitConversion(operation, argument);

                if (operation.OperatorMethod != null)
                {
                    // Conservatively handle user defined conversions as escaping operations.
                    HandleEscapingOperation(operation, operation.Operand);
                }

                ConversionInference? inferenceOpt = null;
                if (value.NullState == NullAbstractValue.NotNull)
                {
                    if (TryInferConversion(operation, out var conversionInference))
                    {
                        inferenceOpt = conversionInference;
                        value = InferConversionCommon(conversionInference, value);
                    }
                    else
                    {
                        value = value.MakeMayBeNull();
                    }
                }

                return HandleBoxingUnboxing(value, operation, inferenceOpt ?? ConversionInference.Create(operation));
            }

            public override PointsToAbstractValue GetAssignedValueForPattern(IIsPatternOperation operation, PointsToAbstractValue operandValue)
            {
                var value = base.GetAssignedValueForPattern(operation, operandValue);

                ConversionInference? inferenceOpt = null;
                if (operandValue.NullState == NullAbstractValue.NotNull &&
                    ShouldBeTracked(operation.Value.Type))
                {
                    if (TryInferConversion(operation, out var conversionInference))
                    {
                        inferenceOpt = conversionInference;
                        value = InferConversionCommon(conversionInference, operandValue);
                    }
                    else
                    {
                        value = operandValue.MakeMayBeNull();
                    }
                }

                return HandleBoxingUnboxing(value, operation, inferenceOpt ?? ConversionInference.Create(operation));
            }

            private static PointsToAbstractValue InferConversionCommon(ConversionInference inference, PointsToAbstractValue operandValue)
            {
                Debug.Assert(!inference.AlwaysSucceed || !inference.AlwaysFail);
                if (inference.AlwaysFail)
                {
                    return operandValue.MakeNull();
                }
                else if (inference.IsTryCast && !inference.AlwaysSucceed)
                {
                    // TryCast which may or may not succeed.
                    return operandValue.MakeMayBeNull();
                }

                return operandValue;
            }

            private PointsToAbstractValue HandleBoxingUnboxing(
                PointsToAbstractValue value,
                IOperation operation,
                ConversionInference inference)
            {
                if (inference.IsBoxing)
                {
                    Debug.Assert(!inference.IsUnboxing);
                    var location = AbstractLocation.CreateAllocationLocation(operation, operation.Type, DataFlowAnalysisContext);
                    return PointsToAbstractValue.Create(location, mayBeNull: false);
                }
                else if (inference.IsUnboxing)
                {
                    return PointsToAbstractValue.NoLocation;
                }
                else
                {
                    return value;
                }
            }

            public override PointsToAbstractValue VisitFlowCapture(IFlowCaptureOperation operation, object argument)
            {
                var value = base.VisitFlowCapture(operation, argument);
                if (IsLValueFlowCapture(operation) &&
                    AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureEntity))
                {
                    value = PointsToAbstractValue.Create(operation.Value);
                    SetAbstractValue(flowCaptureEntity, value);
                }

                return value;
            }

            public override PointsToAbstractValue VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation, object argument)
            {
                var value = base.VisitFlowCaptureReference(operation, argument);
                if (IsLValueFlowCaptureReference(operation) &&
                    AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity flowCaptureEntity))
                {
                    return GetAbstractValue(flowCaptureEntity);
                }

                return value;
            }

            public override PointsToAbstractValue ComputeValueForCompoundAssignment(ICompoundAssignmentOperation operation, PointsToAbstractValue targetValue, PointsToAbstractValue assignedValue, ITypeSymbol targetType, ITypeSymbol assignedValueType)
            {
                if (targetValue.Kind == PointsToAbstractValueKind.KnownLValueCaptures)
                {
                    // Flow captures can never be re-assigned, so reuse the target value.
                    return targetValue;
                }

                return base.ComputeValueForCompoundAssignment(operation, targetValue, assignedValue, targetType, assignedValueType);
            }

            protected override PointsToAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
            {
                var value = base.VisitAssignmentOperation(operation, argument);
                HandlePossibleEscapingForAssignment(operation.Target, operation.Value, operation);
                return value;
            }

            public override PointsToAbstractValue VisitDeclarationExpression(IDeclarationExpressionOperation operation, object argument)
            {
                return Visit(operation.Expression, argument);
            }

            public override PointsToAbstractValue VisitCaughtException(ICaughtExceptionOperation operation, object argument)
            {
                _ = base.VisitCaughtException(operation, argument);
                return PointsToAbstractValue.UnknownNotNull;
            }

            #endregion
        }
    }
}
