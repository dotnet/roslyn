// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CopyAnalysisDomain = PredicatedAnalysisDataDomain<CopyAnalysisData, CopyAbstractValue>;
    using CopyAnalysisResult = DataFlowAnalysisResult<CopyBlockAnalysisResult, CopyAbstractValue>;

    internal partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the copy values across a given statement in a basic block.
        /// </summary>
        private sealed class CopyDataFlowOperationVisitor : AnalysisEntityDataFlowOperationVisitor<CopyAnalysisData, CopyAnalysisContext, CopyAnalysisResult, CopyAbstractValue>
        {
            public CopyDataFlowOperationVisitor(CopyAnalysisContext analysisContext)
                : base(analysisContext)
            {
                var coreAnalysisDomain = new CoreCopyAnalysisDataDomain(CopyAbstractValueDomain.Default, GetDefaultCopyValue);
                AnalysisDomain = new CopyAnalysisDomain(coreAnalysisDomain);

                analysisContext.InterproceduralAnalysisDataOpt?.InitialAnalysisData.AssertValidCopyAnalysisData();
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

            protected override void AddTrackedEntities(PooledHashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis)
                => CurrentAnalysisData.AddTrackedEntities(builder);

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override bool HasAnyAbstractValue(CopyAnalysisData data) => data.HasAnyAbstractValue;

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity)
                => StopTrackingEntity(analysisEntity, CurrentAnalysisData, GetDefaultCopyValue);

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
                analysisData.AssertValidCopyAnalysisData(tryGetDefaultCopyValueOpt: null);

                // Now it should be safe to remove the entry.
                analysisData.RemoveEntries(analysisEntity);
                analysisData.AssertValidCopyAnalysisData(getDefaultCopyValue);
            }

            protected override CopyAbstractValue GetAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : CopyAbstractValue.Unknown;

            protected override CopyAbstractValue GetCopyAbstractValue(IOperation operation) => base.GetCachedAbstractValue(operation);

            protected override CopyAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => CopyAbstractValue.NotApplicable;

            protected override void ResetAbstractValue(AnalysisEntity analysisEntity)
                => SetAbstractValue(analysisEntity, GetResetValue(analysisEntity));

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, CopyAbstractValue value)
            {
                Debug.Assert(analysisEntity != null);
                Debug.Assert(value != null);

                SetAbstractValue(CurrentAnalysisData, analysisEntity, value, TryGetAddressSharedCopyValue);
            }

            protected override void SetAbstractValueForAssignment(AnalysisEntity targetAnalysisEntity, IOperation assignedValueOperation, CopyAbstractValue assignedValue)
            {
                if (assignedValue.AnalysisEntities.Contains(targetAnalysisEntity))
                {
                    // Dead assignment (assigning the same value).
                    return;
                }

                base.SetAbstractValueForAssignment(targetAnalysisEntity, assignedValueOperation, assignedValue);
                CurrentAnalysisData.AssertValidCopyAnalysisData();
            }

            private static void SetAbstractValue(
                CopyAnalysisData copyAnalysisData,
                AnalysisEntity analysisEntity,
                CopyAbstractValue value,
                Func<AnalysisEntity, CopyAbstractValue> tryGetAddressSharedCopyValue,
                bool fromPredicate = false,
                bool initializingParameters = false)
            {
                SetAbstractValue(sourceCopyAnalysisData: copyAnalysisData, targetCopyAnalysisData: copyAnalysisData,
                    analysisEntity: analysisEntity, value: value, tryGetAddressSharedCopyValue: tryGetAddressSharedCopyValue, fromPredicate: fromPredicate,
                    initializingParameters: initializingParameters);
            }

            private static void SetAbstractValue(
                CopyAnalysisData sourceCopyAnalysisData,
                CopyAnalysisData targetCopyAnalysisData,
                AnalysisEntity analysisEntity,
                CopyAbstractValue value,
                Func<AnalysisEntity, CopyAbstractValue> tryGetAddressSharedCopyValue,
                bool fromPredicate,
                bool initializingParameters)
            {
                sourceCopyAnalysisData.AssertValidCopyAnalysisData(tryGetAddressSharedCopyValue, initializingParameters);
                targetCopyAnalysisData.AssertValidCopyAnalysisData(tryGetAddressSharedCopyValue, initializingParameters);
                Debug.Assert(ReferenceEquals(sourceCopyAnalysisData, targetCopyAnalysisData) || fromPredicate);
                Debug.Assert(tryGetAddressSharedCopyValue != null);

                // Don't track entities if do not know about it's instance location.
                if (analysisEntity.HasUnknownInstanceLocation)
                {
                    return;
                }

                if (value.AnalysisEntities.Count > 0)
                {
                    var validEntities = value.AnalysisEntities.Where(entity => !entity.HasUnknownInstanceLocation).ToImmutableHashSet();
                    if (validEntities.Count < value.AnalysisEntities.Count)
                    {
                        value = validEntities.Count > 0 ? new CopyAbstractValue(validEntities) : CopyAbstractValue.Unknown;
                    }
                }

                // Handle updating the existing value if not setting the value from predicate analysis.
                if (!fromPredicate &&
                    sourceCopyAnalysisData.TryGetValue(analysisEntity, out CopyAbstractValue existingValue))
                {
                    if (existingValue == value)
                    {
                        // Assigning the same value to the entity.
                        Debug.Assert(existingValue.AnalysisEntities.Contains(analysisEntity));
                        return;
                    }

                    if (existingValue.AnalysisEntities.Count > 1)
                    {
                        CopyAbstractValue addressSharedCopyValue = tryGetAddressSharedCopyValue(analysisEntity);
                        if (addressSharedCopyValue == null || addressSharedCopyValue != existingValue)
                        {
                            CopyAbstractValue newValueForEntitiesInOldSet = addressSharedCopyValue != null ?
                                existingValue.WithEntitiesRemoved(addressSharedCopyValue.AnalysisEntities) :
                                existingValue.WithEntityRemoved(analysisEntity);
                            targetCopyAnalysisData.SetAbstactValueForEntities(newValueForEntitiesInOldSet, entityBeingAssignedOpt: analysisEntity);
                        }
                    }
                }

                // Handle setting the new value.
                var newAnalysisEntities = value.AnalysisEntities.Add(analysisEntity);
                if (fromPredicate)
                {
                    // Also include the existing values for the analysis entity.
                    if (sourceCopyAnalysisData.TryGetValue(analysisEntity, out existingValue))
                    {
                        if (existingValue.Kind == CopyAbstractValueKind.Invalid)
                        {
                            return;
                        }

                        newAnalysisEntities = newAnalysisEntities.Union(existingValue.AnalysisEntities);
                    }
                }
                else
                {
                    // Include address shared entities, if any.
                    CopyAbstractValue addressSharedCopyValue = tryGetAddressSharedCopyValue(analysisEntity);
                    if (addressSharedCopyValue != null)
                    {
                        newAnalysisEntities = newAnalysisEntities.Union(addressSharedCopyValue.AnalysisEntities);
                    }
                }

                var newValue = new CopyAbstractValue(newAnalysisEntities);
                targetCopyAnalysisData.SetAbstactValueForEntities(newValue, entityBeingAssignedOpt: analysisEntity);

                targetCopyAnalysisData.AssertValidCopyAnalysisData(tryGetAddressSharedCopyValue, initializingParameters);
            }

            protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<CopyAbstractValue> assignedValueOpt)
            {
                CopyAbstractValue copyValue;
                if (assignedValueOpt != null)
                {
                    var assignedEntities = assignedValueOpt.Value.AnalysisEntities;
                    if (assignedValueOpt.AnalysisEntityOpt != null && !assignedEntities.Contains(assignedValueOpt.AnalysisEntityOpt))
                    {
                        assignedEntities = assignedEntities.Add(assignedValueOpt.AnalysisEntityOpt);
                    }

                    var newAnalysisEntities = assignedEntities;
                    foreach (var entity in assignedEntities)
                    {
                        if (CurrentAnalysisData.TryGetValue(entity, out var existingValue))
                        {
                            newAnalysisEntities = newAnalysisEntities.Union(existingValue.AnalysisEntities);
                        }
                    }

                    copyValue = assignedValueOpt.Value.AnalysisEntities.Count == newAnalysisEntities.Count ?
                        assignedValueOpt.Value :
                        new CopyAbstractValue(newAnalysisEntities);
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
                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return CurrentAnalysisData.TryGetValue(analysisEntity, out CopyAbstractValue value) ? value : GetDefaultCopyValue(analysisEntity);
                }
                else
                {
                    return defaultValue;
                }
            }

            protected override CopyAbstractValue ComputeAnalysisValueForEscapedRefOrOutArgument(AnalysisEntity analysisEntity, IArgumentOperation operation, CopyAbstractValue defaultValue)
            {
                Debug.Assert(operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out);

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
                if (GetCopyAbstractValue(leftOperand).Kind != CopyAbstractValueKind.Unknown &&
                    GetCopyAbstractValue(rightOperand).Kind != CopyAbstractValueKind.Unknown &&
                    AnalysisEntityFactory.TryCreate(leftOperand, out AnalysisEntity leftEntity) &&
                    AnalysisEntityFactory.TryCreate(rightOperand, out AnalysisEntity rightEntity))
                {
                    var predicateKind = PredicateValueKind.Unknown;
                    if (!CurrentAnalysisData.TryGetValue(rightEntity, out CopyAbstractValue rightValue))
                    {
                        rightValue = GetDefaultCopyValue(rightEntity);
                    }
                    else if (rightValue.AnalysisEntities.Contains(leftEntity))
                    {
                        // We have "a == b && a == b" or "a == b && a != b"
                        // For both cases, condition on right is always true or always false and redundant.
                        // NOTE: CopyAnalysis only tracks value equal entities
                        if (!isReferenceEquality)
                        {
                            predicateKind = equals ? PredicateValueKind.AlwaysTrue : PredicateValueKind.AlwaysFalse;
                        }
                    }

                    if (predicateKind != PredicateValueKind.Unknown)
                    {
                        if (!equals)
                        {
                            // "a == b && a != b" or "a == b || a != b"
                            foreach (var entity in rightValue.AnalysisEntities)
                            {
                                SetAbstractValue(targetAnalysisData, entity, CopyAbstractValue.Invalid, TryGetAddressSharedCopyValue, fromPredicate: true);
                            }
                        }

                        return predicateKind;
                    }

                    if (equals)
                    {
                        SetAbstractValue(targetAnalysisData, leftEntity, rightValue, TryGetAddressSharedCopyValue, fromPredicate: true);
                    }
                }

                return PredicateValueKind.Unknown;
            }

            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, CopyAnalysisData copyAnalysisData) => PredicateValueKind.Unknown;
            #endregion

            protected override CopyAnalysisData MergeAnalysisData(CopyAnalysisData value1, CopyAnalysisData value2)
                => AnalysisDomain.Merge(value1, value2);
            protected override CopyAnalysisData GetClonedAnalysisData(CopyAnalysisData analysisData)
                => (CopyAnalysisData)analysisData.Clone();
            public override CopyAnalysisData GetEmptyAnalysisData()
                => new CopyAnalysisData();
            protected override CopyAnalysisData GetExitBlockOutputData(CopyAnalysisResult analysisResult)
                => new CopyAnalysisData(analysisResult.ExitBlockOutput.Data);
            protected override bool Equals(CopyAnalysisData value1, CopyAnalysisData value2)
                => value1.Equals(value2);

            public override (CopyAbstractValue Value, PredicateValueKind PredicateValueKind)? GetReturnValueAndPredicateKind()
            {
                // Filter out all the local symbol and flow capture entities from the return value.
                var returnValueAndPredicateKindOpt = base.GetReturnValueAndPredicateKind();
                if (returnValueAndPredicateKindOpt.HasValue &&
                    returnValueAndPredicateKindOpt.Value.Value.Kind == CopyAbstractValueKind.Known)
                {
                    var entitiesToFilterBuilder = PooledHashSet<AnalysisEntity>.GetInstance();

                    try
                    {
                        var copyValue = returnValueAndPredicateKindOpt.Value.Value;
                        var copyValueEntities = copyValue.AnalysisEntities;
                        foreach (var entity in copyValueEntities)
                        {
                            if (ShouldRemoveEntityAtExit(entity))
                            {
                                ProcessEntityToRemoveAtExit(entity, entitiesToFilterBuilder, copyValueEntities);
                            }
                        }

                        if (entitiesToFilterBuilder.Count > 0)
                        {
                            copyValue = entitiesToFilterBuilder.Count == copyValueEntities.Count ?
                                CopyAbstractValue.Unknown :
                                copyValue.WithEntitiesRemoved(entitiesToFilterBuilder);
                        }

                        return (copyValue, returnValueAndPredicateKindOpt.Value.PredicateValueKind);
                    }
                    finally
                    {
                        entitiesToFilterBuilder.Free();
                    }
                }

                return returnValueAndPredicateKindOpt;
            }

            private bool ShouldRemoveEntityAtExit(AnalysisEntity entity)
            {
                return entity.SymbolOpt?.Kind == SymbolKind.Local &&
                    entity.SymbolOpt.ContainingSymbol.Equals(DataFlowAnalysisContext.OwningSymbol) ||
                    entity.CaptureIdOpt.HasValue &&
                    entity.CaptureIdOpt.Value.ControlFlowGraph == DataFlowAnalysisContext.ControlFlowGraph;
            }

            private void ProcessEntityToRemoveAtExit(
                AnalysisEntity entity,
                PooledHashSet<AnalysisEntity> entitiesToFilterBuilder,
                IEnumerable<AnalysisEntity> allAnalysisEntities)
            {
                Debug.Assert(ShouldRemoveEntityAtExit(entity));

                // Stop tracking entity that is now out of scope.
                entitiesToFilterBuilder.Add(entity);

                // Additionally, stop tracking all the child entities if the entity type has value copy semantics.
                if (entity.Type.HasValueCopySemantics())
                {
                    var childEntities = allAnalysisEntities.Where(e => IsChildAnalysisEntity(e, ancestorEntity: entity));
                    entitiesToFilterBuilder.AddRange(childEntities);
                }
            }

            public override CopyAnalysisData GetMergedDataForUnhandledThrowOperations()
            {
                var copyAnalysisData = base.GetMergedDataForUnhandledThrowOperations();
                if (copyAnalysisData == null)
                {
                    return null;
                }

                // Filter out all the local symbol and flow capture entities from the returned analysis data.
                var entitiesToFilterBuilder = PooledHashSet<AnalysisEntity>.GetInstance();

                try
                {
                    var allAnalysisEntities = copyAnalysisData.CoreAnalysisData.Keys;
                    foreach (var entity in allAnalysisEntities)
                    {
                        if (ShouldRemoveEntityAtExit(entity))
                        {
                            ProcessEntityToRemoveAtExit(entity, entitiesToFilterBuilder, allAnalysisEntities);
                        }
                    }

                    foreach (var entity in entitiesToFilterBuilder)
                    {
                        StopTrackingEntity(entity, copyAnalysisData, GetDefaultCopyValue);
                    }
                }
                finally
                {
                    entitiesToFilterBuilder.Free();
                }

                return copyAnalysisData;
            }

            #region Interprocedural analysis

            protected override void ApplyInterproceduralAnalysisResultCore(CopyAnalysisData resultData)
            {
                var processedEntities = PooledHashSet<AnalysisEntity>.GetInstance();
                try
                {
                    foreach (var kvp in resultData.CoreAnalysisData)
                    {
                        var entity = kvp.Key;
                        var newCopyValue = kvp.Value;
                        if (processedEntities.Add(entity))
                        {
                            var currentCopyValue = GetAbstractValue(entity);
                            if (currentCopyValue != newCopyValue)
                            {
                                CurrentAnalysisData.SetAbstactValueForEntities(newCopyValue, entityBeingAssignedOpt: null);
                            }

                            processedEntities.AddRange(newCopyValue.AnalysisEntities);
                        }
                    }

                    AssertValidCopyAnalysisData(CurrentAnalysisData);
                }
                finally
                {
                    processedEntities.Free();
                }
            }

            protected override CopyAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities)
            {
                var processedEntities = PooledHashSet<AnalysisEntity>.GetInstance();
                try
                {
                    var analysisData = new CopyAnalysisData();
                    foreach (var entity in withEntities)
                    {
                        if (processedEntities.Add(entity))
                        {
                            var copyValue = GetAbstractValue(entity);
                            analysisData.SetAbstactValueForEntities(copyValue, entityBeingAssignedOpt: null);
                            processedEntities.AddRange(copyValue.AnalysisEntities);
                        }
                    }

                    AssertValidCopyAnalysisData(analysisData);
                    return analysisData;
                }
                finally
                {
                    processedEntities.Free();
                }
            }

            protected override CopyAnalysisData GetInitialInterproceduralAnalysisData(
                IMethodSymbol invokedMethod,
                (AnalysisEntity InstanceOpt, PointsToAbstractValue PointsToValue)? invocationInstanceOpt,
                (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCallerOpt,
                ImmutableArray<ArgumentInfo<CopyAbstractValue>> argumentValues,
                IDictionary<AnalysisEntity, PointsToAbstractValue> pointsToValuesOpt,
                IDictionary<AnalysisEntity, CopyAbstractValue> copyValuesOpt,
                bool isLambdaOrLocalFunction)
            {
                copyValuesOpt = CurrentAnalysisData.CoreAnalysisData;
                var initialAnalysisData = base.GetInitialInterproceduralAnalysisData(invokedMethod, invocationInstanceOpt,
                    thisOrMeInstanceForCallerOpt, argumentValues, pointsToValuesOpt, copyValuesOpt, isLambdaOrLocalFunction);
                AssertValidCopyAnalysisData(initialAnalysisData);
                return initialAnalysisData;
            }

            #endregion

            #region Visitor overrides
            public override CopyAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                _ = base.DefaultVisit(operation, argument);
                return CopyAbstractValue.Unknown;
            }

            public override CopyAbstractValue VisitConversion(IConversionOperation operation, object argument)
            {
                var operandValue = Visit(operation.Operand, argument);

                if (TryInferConversion(operation, out bool alwaysSucceed, out bool alwaysFail) &&
                    ConversionAlwaysSucceeds(alwaysSucceed, alwaysFail, operation.IsTryCast, operation.Type))
                {
                    return operandValue;
                }

                return CopyAbstractValue.Unknown;
            }

            public override CopyAbstractValue GetAssignedValueForPattern(IIsPatternOperation operation, CopyAbstractValue operandValue)
            {
                if (TryInferConversion(operation, out bool alwaysSucceed, out bool alwaysFail))
                {
                    var targetType = operation.Pattern.GetPatternType();
                    if (ConversionAlwaysSucceeds(alwaysSucceed, alwaysFail, isTryCast: true, targetType: targetType))
                    {
                        return operandValue;
                    }
                }

                return CopyAbstractValue.Unknown;
            }

            private static bool ConversionAlwaysSucceeds(bool alwaysSucceed, bool alwaysFail, bool isTryCast, ITypeSymbol targetType)
            {
                Debug.Assert(!alwaysSucceed || !alwaysFail);

                // Flow the copy value of the operand to the converted operation if conversion may succeed.
                if (!alwaysFail)
                {
                    // For try cast, also ensure conversion always succeeds before flowing copy value.
                    // TODO: For direct cast, we should check if conversion is implicit.
                    // For now, we only flow values for reference type direct cast conversions.
                    if (isTryCast && alwaysSucceed ||
                        !isTryCast && targetType.IsReferenceType)
                    {
                        return true;
                    }
                }

                return false;
            }

            protected override CopyAbstractValue VisitAssignmentOperation(IAssignmentOperation operation, object argument)
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
