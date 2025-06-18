// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisDomain = PredicatedAnalysisDataDomain<CopyAnalysisData, CopyAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    public partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the copy values across a given statement in a basic block.
        /// </summary>
        private sealed class CopyDataFlowOperationVisitor : PredicateAnalysisEntityDataFlowOperationVisitor<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyAbstractValue>
        {
            public CopyDataFlowOperationVisitor(CopyAnalysisContext analysisContext)
                : base(analysisContext)
            {
                var coreAnalysisDomain = new CoreCopyAnalysisDataDomain(CopyAbstractValueDomain.Default, GetDefaultCopyValue);
                AnalysisDomain = new CopyAnalysisDomain(coreAnalysisDomain);

                analysisContext.InterproceduralAnalysisData?.InitialAnalysisData?.AssertValidCopyAnalysisData();
            }

            public CopyAnalysisDomain AnalysisDomain { get; }

            public override CopyAnalysisData Flow(IOperation statement, BasicBlock block, CopyAnalysisData input)
            {
                AssertValidCopyAnalysisData(input);
                var output = base.Flow(statement, block, input);
                AssertValidCopyAnalysisData(output);
                return output;
            }

            public override (CopyAnalysisData output, bool isFeasibleBranch) FlowBranch(
                BasicBlock fromBlock,
                BranchWithInfo branch,
                CopyAnalysisData input)
            {
                AssertValidCopyAnalysisData(input);
                (CopyAnalysisData output, bool isFeasibleBranch) result = base.FlowBranch(fromBlock, branch, input);
                AssertValidCopyAnalysisData(result.output);
                return result;
            }

            [Conditional("DEBUG")]
            private void AssertValidCopyAnalysisData(CopyAnalysisData copyAnalysisData)
            {
                copyAnalysisData.AssertValidCopyAnalysisData(GetDefaultCopyValue);
            }

            protected override void AddTrackedEntities(CopyAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
                => analysisData.AddTrackedEntities(builder);

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override bool HasAnyAbstractValue(CopyAnalysisData data) => data.HasAnyAbstractValue;

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity, CopyAnalysisData analysisData)
                => StopTrackingEntity(analysisEntity, analysisData, GetDefaultCopyValue);

            private static void StopTrackingEntity(
                AnalysisEntity analysisEntity,
                CopyAnalysisData analysisData,
                Func<AnalysisEntity, CopyAbstractValue> getDefaultCopyValue)
            {
                analysisData.AssertValidCopyAnalysisData(getDefaultCopyValue);

                // First set the value to unknown so we remove the entity from existing copy sets.
                // Note that we pass 'tryGetAddressSharedCopyValue = null' to ensure that
                // we do not reset the entries for address shared entities.
                SetAbstractValue(analysisData, analysisEntity, CopyAbstractValue.Unknown, tryGetAddressSharedCopyValue: _ => null);
                analysisData.AssertValidCopyAnalysisData(tryGetDefaultCopyValue: null);

                // Now it should be safe to remove the entry.
                analysisData.RemoveEntries(analysisEntity);
                analysisData.AssertValidCopyAnalysisData(getDefaultCopyValue);
            }

            protected override CopyAbstractValue GetAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : CopyAbstractValue.Unknown;

            protected override CopyAbstractValue GetCopyAbstractValue(IOperation operation) => base.GetCachedAbstractValue(operation);

            protected override CopyAbstractValue GetAbstractDefaultValue(ITypeSymbol? type) => CopyAbstractValue.NotApplicable;

            protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
                => SetAbstractValue(analysisEntity, GetResetValue(analysisEntity));

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, CopyAbstractValue value)
            {
                SetAbstractValue(CurrentAnalysisData, analysisEntity, value, TryGetAddressSharedCopyValue);
            }

            protected override void SetAbstractValueForAssignment(AnalysisEntity targetAnalysisEntity, IOperation? assignedValueOperation, CopyAbstractValue assignedValue)
            {
                if (assignedValue.AnalysisEntities.Contains(targetAnalysisEntity))
                {
                    // Dead assignment (assigning the same value).
                    return;
                }

                base.SetAbstractValueForAssignment(targetAnalysisEntity, assignedValueOperation, assignedValue);
                CurrentAnalysisData.AssertValidCopyAnalysisData();
            }

            protected override void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, CopyAbstractValue assignedValue)
            {
                // Copy value of assignedValueOperation entity might change between tuple element assignments within the same tuple.
                // For example, '(a, a)'
                if (!assignedValue.AnalysisEntities.IsEmpty)
                {
                    var currentCopyValue = GetAbstractValue(assignedValue.AnalysisEntities.First());
                    if (currentCopyValue.Kind != CopyAbstractValueKind.Unknown)
                    {
                        assignedValue = currentCopyValue;
                    }
                }

                base.SetAbstractValueForTupleElementAssignment(tupleElementEntity, assignedValueOperation, assignedValue);
            }

            private static void SetAbstractValue(
                CopyAnalysisData copyAnalysisData,
                AnalysisEntity analysisEntity,
                CopyAbstractValue value,
                Func<AnalysisEntity, CopyAbstractValue?> tryGetAddressSharedCopyValue,
                SetCopyAbstractValuePredicateKind? fromPredicateKind = null,
                bool initializingParameters = false)
            {
                SetAbstractValue(sourceCopyAnalysisData: copyAnalysisData, targetCopyAnalysisData: copyAnalysisData,
                    analysisEntity, value, tryGetAddressSharedCopyValue, fromPredicateKind, initializingParameters);
            }

            private static void SetAbstractValue(
                CopyAnalysisData sourceCopyAnalysisData,
                CopyAnalysisData targetCopyAnalysisData,
                AnalysisEntity analysisEntity,
                CopyAbstractValue value,
                Func<AnalysisEntity, CopyAbstractValue?> tryGetAddressSharedCopyValue,
                SetCopyAbstractValuePredicateKind? fromPredicateKind,
                bool initializingParameters)
            {
                sourceCopyAnalysisData.AssertValidCopyAnalysisData(tryGetAddressSharedCopyValue, initializingParameters);
                targetCopyAnalysisData.AssertValidCopyAnalysisData(tryGetAddressSharedCopyValue, initializingParameters);
                Debug.Assert(ReferenceEquals(sourceCopyAnalysisData, targetCopyAnalysisData) || fromPredicateKind.HasValue);

                // Don't track entities if do not know about it's instance location.
                if (analysisEntity.HasUnknownInstanceLocation)
                {
                    return;
                }

                if (!value.AnalysisEntities.IsEmpty)
                {
                    var validEntities = value.AnalysisEntities.Where(entity => !entity.HasUnknownInstanceLocation).ToImmutableHashSet();
                    if (validEntities.Count < value.AnalysisEntities.Count)
                    {
                        value = !validEntities.IsEmpty ? new CopyAbstractValue(validEntities, value.Kind) : CopyAbstractValue.Unknown;
                    }
                }

                // Handle updating the existing value if not setting the value from predicate analysis.
                if (!fromPredicateKind.HasValue &&
                    sourceCopyAnalysisData.TryGetValue(analysisEntity, out var existingValue))
                {
                    if (existingValue == value)
                    {
                        // Assigning the same value to the entity.
                        Debug.Assert(existingValue.AnalysisEntities.Contains(analysisEntity));
                        return;
                    }

                    if (existingValue.AnalysisEntities.Count > 1)
                    {
                        CopyAbstractValue? addressSharedCopyValue = tryGetAddressSharedCopyValue(analysisEntity);
                        if (addressSharedCopyValue == null || addressSharedCopyValue != existingValue)
                        {
                            CopyAbstractValue newValueForEntitiesInOldSet = addressSharedCopyValue != null ?
                                existingValue.WithEntitiesRemoved(addressSharedCopyValue.AnalysisEntities) :
                                existingValue.WithEntityRemoved(analysisEntity);
                            targetCopyAnalysisData.SetAbstactValueForEntities(newValueForEntitiesInOldSet, entityBeingAssigned: analysisEntity);
                        }
                    }
                }

                // Handle setting the new value.
                var newAnalysisEntities = value.AnalysisEntities.Add(analysisEntity);
                CopyAbstractValueKind newKind;
                if (newAnalysisEntities.Count == 1)
                {
                    newKind = analysisEntity.Type.IsValueType ?
                        CopyAbstractValueKind.KnownValueCopy :
                        CopyAbstractValueKind.KnownReferenceCopy;
                }
                else
                {
                    newKind = fromPredicateKind != SetCopyAbstractValuePredicateKind.ValueCompare &&
                        !analysisEntity.Type.IsValueType &&
                        value.Kind == CopyAbstractValueKind.KnownReferenceCopy ?
                        CopyAbstractValueKind.KnownReferenceCopy :
                        CopyAbstractValueKind.KnownValueCopy;
                }

                if (fromPredicateKind.HasValue)
                {
                    // Also include the existing values for the analysis entity.
                    if (sourceCopyAnalysisData.TryGetValue(analysisEntity, out existingValue))
                    {
                        if (existingValue.Kind == CopyAbstractValueKind.Invalid)
                        {
                            return;
                        }

                        newAnalysisEntities = newAnalysisEntities.Union(existingValue.AnalysisEntities);
                        newKind = newKind.MergeIfBothKnown(existingValue.Kind);
                    }
                }
                else
                {
                    // Include address shared entities, if any.
                    CopyAbstractValue? addressSharedCopyValue = tryGetAddressSharedCopyValue(analysisEntity);
                    if (addressSharedCopyValue != null)
                    {
                        newAnalysisEntities = newAnalysisEntities.Union(addressSharedCopyValue.AnalysisEntities);
                    }
                }

                var newValue = new CopyAbstractValue(newAnalysisEntities, newKind);
                targetCopyAnalysisData.SetAbstactValueForEntities(newValue, entityBeingAssigned: analysisEntity);

                targetCopyAnalysisData.AssertValidCopyAnalysisData(tryGetAddressSharedCopyValue, initializingParameters);
            }

            protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<CopyAbstractValue>? assignedValue)
            {
                CopyAbstractValue copyValue;
                if (assignedValue != null)
                {
                    var assignedEntities = assignedValue.Value.AnalysisEntities;
                    if (assignedValue.AnalysisEntity != null && !assignedEntities.Contains(assignedValue.AnalysisEntity))
                    {
                        assignedEntities = assignedEntities.Add(assignedValue.AnalysisEntity);
                    }

                    var newAnalysisEntities = assignedEntities;
                    CopyAbstractValueKind newKind;
                    if (assignedValue.Value.Kind.IsKnown())
                    {
                        newKind = assignedValue.Value.Kind;
                    }
                    else if (assignedValue.AnalysisEntity == null || assignedValue.AnalysisEntity.Type.IsValueType)
                    {
                        newKind = CopyAbstractValueKind.KnownValueCopy;
                    }
                    else
                    {
                        newKind = CopyAbstractValueKind.KnownReferenceCopy;
                    }

                    foreach (var entity in assignedEntities)
                    {
                        if (CurrentAnalysisData.TryGetValue(entity, out var existingValue))
                        {
                            newAnalysisEntities = newAnalysisEntities.Union(existingValue.AnalysisEntities);
                            newKind = newKind.MergeIfBothKnown(existingValue.Kind);
                        }
                    }

                    copyValue = assignedValue.Value.AnalysisEntities.Count == newAnalysisEntities.Count ?
                        assignedValue.Value :
                        new CopyAbstractValue(newAnalysisEntities, newKind);
                }
                else
                {
                    copyValue = GetDefaultCopyValue(analysisEntity);
                }

                SetAbstractValue(CurrentAnalysisData, analysisEntity, copyValue, TryGetAddressSharedCopyValue, initializingParameters: true);
            }

            protected override void EscapeValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                // Do not escape the copy value for parameter at exit.
            }

            private CopyAbstractValue GetResetValue(AnalysisEntity analysisEntity)
                => GetResetValue(analysisEntity, GetAbstractValue(analysisEntity));
            private CopyAbstractValue GetResetValue(AnalysisEntity analysisEntity, CopyAbstractValue currentValue)
                => currentValue.AnalysisEntities.Count > 1 ? GetDefaultCopyValue(analysisEntity) : currentValue;

            protected override void ResetCurrentAnalysisData() => CurrentAnalysisData.Reset(GetResetValue);

            protected override CopyAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, CopyAbstractValue defaultValue)
            {
                if (AnalysisEntityFactory.TryCreate(operation, out var analysisEntity))
                {
                    return CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : GetDefaultCopyValue(analysisEntity);
                }
                else
                {
                    return defaultValue;
                }
            }

            protected override CopyAbstractValue ComputeAnalysisValueForEscapedRefOrOutArgument(AnalysisEntity analysisEntity, IArgumentOperation operation, CopyAbstractValue defaultValue)
            {
                Debug.Assert(operation.Parameter?.RefKind is RefKind.Ref or RefKind.Out);

                SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);
                return GetAbstractValue(analysisEntity);
            }

            #region Predicate analysis
            protected override PredicateValueKind SetValueForEqualsOrNotEqualsComparisonOperator(
                IOperation leftOperand,
                IOperation rightOperand,
                bool equals,
                bool isReferenceEquality,
                CopyAnalysisData targetAnalysisData)
            {
                var predicateKind = PredicateValueKind.Unknown;

                var leftCopyValue = GetCopyAbstractValue(leftOperand);
                var rightCopyValue = GetCopyAbstractValue(rightOperand);
                if (leftCopyValue.AnalysisEntities.IsEmpty ||
                    rightCopyValue.AnalysisEntities.IsEmpty)
                {
                    return predicateKind;
                }

                if (leftCopyValue == rightCopyValue)
                {
                    // We have "a == b && a == b" or "a == b && a != b"
                    // For both cases, condition on right is always true or always false and redundant.
                    if (!isReferenceEquality ||
                        rightCopyValue.Kind == CopyAbstractValueKind.KnownReferenceCopy)
                    {
                        predicateKind = equals ? PredicateValueKind.AlwaysTrue : PredicateValueKind.AlwaysFalse;
                    }
                }

                var setCopyValuePredicateKind = isReferenceEquality ?
                        SetCopyAbstractValuePredicateKind.ReferenceCompare :
                        SetCopyAbstractValuePredicateKind.ValueCompare;

                if (predicateKind != PredicateValueKind.Unknown)
                {
                    if (!equals)
                    {
                        // "a == b && a != b" or "a == b || a != b"
                        foreach (var entity in rightCopyValue.AnalysisEntities)
                        {
                            SetAbstractValue(targetAnalysisData, entity, CopyAbstractValue.Invalid, TryGetAddressSharedCopyValue, setCopyValuePredicateKind);
                        }
                    }

                    return predicateKind;
                }

                if (equals)
                {
                    SetAbstractValue(targetAnalysisData, leftCopyValue.AnalysisEntities.First(), rightCopyValue, TryGetAddressSharedCopyValue, setCopyValuePredicateKind);
                }

                return PredicateValueKind.Unknown;
            }

            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, CopyAnalysisData targetAnalysisData) => PredicateValueKind.Unknown;
            #endregion

            protected override CopyAnalysisData MergeAnalysisData(CopyAnalysisData value1, CopyAnalysisData value2)
                => AnalysisDomain.Merge(value1, value2);

            protected override void UpdateValuesForAnalysisData(CopyAnalysisData targetAnalysisData)
            {
                // We need to trim the copy values to only include the entities that are existing keys in targetAnalysisData.
                using var _1 = PooledHashSet<AnalysisEntity>.GetInstance(out var processedEntities);
                using var _2 = ArrayBuilder<AnalysisEntity>.GetInstance(targetAnalysisData.CoreAnalysisData.Count, out var builder);
                builder.AddRange(targetAnalysisData.CoreAnalysisData.Keys);

                for (int i = 0; i < builder.Count; i++)
                {
                    var key = builder[i];
                    if (!processedEntities.Add(key))
                    {
                        continue;
                    }

                    var existingValue = targetAnalysisData[key];
                    if (CurrentAnalysisData.TryGetValue(key, out var newValue))
                    {
                        if (newValue.AnalysisEntities.Count == 1)
                        {
                            if (existingValue.AnalysisEntities.Count == 1)
                            {
                                continue;
                            }
                        }
                        else if (newValue.AnalysisEntities.Count > 1)
                        {
                            var entitiesToExclude = newValue.AnalysisEntities.Where(e => !targetAnalysisData.HasAbstractValue(e));
                            if (entitiesToExclude.Any())
                            {
                                newValue = newValue.WithEntitiesRemoved(entitiesToExclude);
                            }
                        }

                        if (newValue != existingValue)
                        {
                            targetAnalysisData.SetAbstactValueForEntities(newValue, entityBeingAssigned: null);
                        }

                        processedEntities.AddRange(newValue.AnalysisEntities);
                    }
                    else
                    {
                        var entitiesToExclude = existingValue.AnalysisEntities.Where(e => !CurrentAnalysisData.HasAbstractValue(e));
                        var excludedCount = 0;
                        foreach (var entity in entitiesToExclude)
                        {
                            targetAnalysisData.RemoveEntries(entity);
                            processedEntities.Add(entity);
                            excludedCount++;
                        }

                        if (excludedCount < existingValue.AnalysisEntities.Count)
                        {
                            newValue = existingValue.WithEntitiesRemoved(entitiesToExclude);
                            targetAnalysisData.SetAbstactValueForEntities(newValue, entityBeingAssigned: null);
                        }
                    }
                }
            }

            protected override void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(CopyAnalysisData dataAtException, ThrownExceptionInfo throwBranchWithExceptionType)
            {
                Func<AnalysisEntity, bool>? predicate = null;
                if (throwBranchWithExceptionType.IsDefaultExceptionForExceptionsPathAnalysis)
                {
                    // Only tracking non-child analysis entities for exceptions path analysis for now.
                    Debug.Assert(SymbolEqualityComparer.Default.Equals(throwBranchWithExceptionType.ExceptionType, ExceptionNamedType));
                    predicate = e => !e.IsChildOrInstanceMember;
                }

                ApplyMissingCurrentAnalysisDataCore(dataAtException, predicate);
            }

            protected override CopyAnalysisData GetClonedAnalysisData(CopyAnalysisData analysisData)
                => (CopyAnalysisData)analysisData.Clone();
            public override CopyAnalysisData GetEmptyAnalysisData()
                => new();
            protected override CopyAnalysisData GetExitBlockOutputData(CopyAnalysisResult analysisResult)
                => new(analysisResult.ExitBlockOutput.Data);
            protected override void AssertValidAnalysisData(CopyAnalysisData analysisData)
                => AssertValidCopyAnalysisData(analysisData);
            protected override bool Equals(CopyAnalysisData value1, CopyAnalysisData value2)
                => value1.Equals(value2);

            public override (CopyAbstractValue Value, PredicateValueKind PredicateValueKind)? GetReturnValueAndPredicateKind()
            {
                // Filter out all the local symbol and flow capture entities from the return value for interprocedural analysis.
                var returnValueAndPredicateKind = base.GetReturnValueAndPredicateKind();
                if (returnValueAndPredicateKind.HasValue &&
                    returnValueAndPredicateKind.Value.Value.Kind.IsKnown() &&
                    DataFlowAnalysisContext.InterproceduralAnalysisData != null)
                {
                    using var _ = PooledHashSet<AnalysisEntity>.GetInstance(out var entitiesToFilterBuilder);
                    var copyValue = returnValueAndPredicateKind.Value.Value;
                    var copyValueEntities = copyValue.AnalysisEntities;

                    foreach (var entity in copyValueEntities)
                    {
                        if (ShouldStopTrackingEntityAtExit(entity))
                        {
                            // Stop tracking entity that is now out of scope.
                            entitiesToFilterBuilder.Add(entity);

                            // Additionally, stop tracking all the child entities if the entity type has value copy semantics.
                            if (entity.Type.HasValueCopySemantics())
                            {
                                var childEntities = copyValueEntities.Where(e => IsChildAnalysisEntity(e, ancestorEntity: entity));
                                entitiesToFilterBuilder.AddRange(childEntities);
                            }
                        }
                    }

                    if (entitiesToFilterBuilder.Count > 0)
                    {
                        copyValue = entitiesToFilterBuilder.Count == copyValueEntities.Count ?
                            CopyAbstractValue.Unknown :
                            copyValue.WithEntitiesRemoved(entitiesToFilterBuilder);
                    }

                    return (copyValue, returnValueAndPredicateKind.Value.PredicateValueKind);
                }

                return returnValueAndPredicateKind;
            }

            #region Interprocedural analysis

            protected override void ApplyInterproceduralAnalysisResultCore(CopyAnalysisData resultData)
            {
                using var mergedData = GetClonedAnalysisData(resultData);
                ApplyMissingCurrentAnalysisDataCore(mergedData, predicate: null);
                CurrentAnalysisData.CoreAnalysisData.Clear();
                CurrentAnalysisData.CoreAnalysisData.AddRange(mergedData.CoreAnalysisData);
            }

            private void ApplyMissingCurrentAnalysisDataCore(CopyAnalysisData mergedData, Func<AnalysisEntity, bool>? predicate)
            {
                using var _ = PooledHashSet<AnalysisEntity>.GetInstance(out var processedEntities);
                foreach (var kvp in CurrentAnalysisData.CoreAnalysisData)
                {
                    var key = kvp.Key;
                    var copyValue = kvp.Value;
                    if (mergedData.CoreAnalysisData.ContainsKey(key) ||
                        (predicate != null && !predicate(key)) ||
                        !processedEntities.Add(key))
                    {
                        continue;
                    }

                    if (predicate != null && copyValue.AnalysisEntities.Count > 1)
                    {
                        var entitiesToRemove = copyValue.AnalysisEntities.Where(entity => key != entity && !predicate(entity));
                        if (entitiesToRemove.Any())
                        {
                            copyValue = copyValue.WithEntitiesRemoved(entitiesToRemove);
                        }
                    }

                    Debug.Assert(copyValue.AnalysisEntities.Contains(key));
                    Debug.Assert(predicate == null || copyValue.AnalysisEntities.All(predicate));
                    mergedData.SetAbstactValueForEntities(copyValue, entityBeingAssigned: null);
                    processedEntities.AddRange(copyValue.AnalysisEntities);
                }

                AssertValidCopyAnalysisData(mergedData);
            }

            protected override CopyAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            {
                using var _ = PooledHashSet<AnalysisEntity>.GetInstance(out var processedEntities);
                var analysisData = new CopyAnalysisData();
                foreach (var entity in withEntities)
                {
                    if (processedEntities.Add(entity))
                    {
                        var copyValue = GetAbstractValue(entity);
                        analysisData.SetAbstactValueForEntities(copyValue, entityBeingAssigned: null);
                        processedEntities.AddRange(copyValue.AnalysisEntities);
                    }
                }

                AssertValidCopyAnalysisData(analysisData);
                return analysisData;
            }

            protected override CopyAnalysisData GetInitialInterproceduralAnalysisData(
                IMethodSymbol invokedMethod,
                (AnalysisEntity? Instance, PointsToAbstractValue PointsToValue)? invocationInstance,
                (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCaller,
                ImmutableDictionary<IParameterSymbol, ArgumentInfo<CopyAbstractValue>> argumentValuesMap,
                IDictionary<AnalysisEntity, PointsToAbstractValue>? pointsToValues,
                IDictionary<AnalysisEntity, CopyAbstractValue>? copyValues,
                IDictionary<AnalysisEntity, ValueContentAbstractValue>? valueContentValues,
                bool isLambdaOrLocalFunction,
                bool hasParameterWithDelegateType)
            {
                copyValues = CurrentAnalysisData.CoreAnalysisData;
                var initialAnalysisData = base.GetInitialInterproceduralAnalysisData(invokedMethod, invocationInstance,
                    thisOrMeInstanceForCaller, argumentValuesMap, pointsToValues, copyValues, valueContentValues,
                    isLambdaOrLocalFunction, hasParameterWithDelegateType);
                AssertValidCopyAnalysisData(initialAnalysisData);
                return initialAnalysisData;
            }

            #endregion

            #region Visitor overrides
            public override CopyAbstractValue DefaultVisit(IOperation operation, object? argument)
            {
                _ = base.DefaultVisit(operation, argument);
                return CopyAbstractValue.Unknown;
            }

            public override CopyAbstractValue VisitConversion(IConversionOperation operation, object? argument)
            {
                var operandValue = Visit(operation.Operand, argument);

                if (TryInferConversion(operation, out var conversionInference) &&
                    FlowConversionOperandValue(conversionInference, operation.Type))
                {
                    return operandValue;
                }

                return CopyAbstractValue.Unknown;
            }

            public override CopyAbstractValue GetAssignedValueForPattern(IIsPatternOperation operation, CopyAbstractValue operandValue)
            {
                if (TryInferConversion(operation, out var conversionInference))
                {
                    if (FlowConversionOperandValue(conversionInference, targetType: operation.Pattern.GetPatternType()))
                    {
                        return operandValue;
                    }
                }

                return CopyAbstractValue.Unknown;
            }

            private static bool FlowConversionOperandValue(ConversionInference inference, ITypeSymbol? targetType)
            {
                Debug.Assert(!inference.AlwaysSucceed || !inference.AlwaysFail);

                // Flow the copy value of the operand for boxing and unboxing conversions.
                if (inference.IsBoxing || inference.IsUnboxing)
                {
                    return true;
                }

                // Otherwise, flow the copy value of the operand to the converted operation if conversion may succeed.
                if (!inference.AlwaysFail)
                {
                    // For try cast, also ensure conversion always succeeds before flowing copy value.
                    // TODO: For direct cast, we should check if conversion is implicit.
                    // For now, we only flow values for reference type direct cast conversions.
                    if (inference.IsTryCast && inference.AlwaysSucceed ||
                        !inference.IsTryCast && targetType?.IsReferenceType == true)
                    {
                        return true;
                    }
                }

                return false;
            }

            protected override CopyAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object? argument)
            {
                var value = base.VisitAssignmentOperation(operation, argument);
                if (AnalysisEntityFactory.TryCreate(operation.Target, out var analysisEntity))
                {
                    return GetAbstractValue(analysisEntity);
                }

                return value;
            }

            #endregion
        }
    }
}
