// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    internal partial class CopyAnalysis : ForwardDataFlowAnalysis<CopyAnalysisData, CopyBlockAnalysisResult, CopyAbstractValue>
    {
        /// <summary>
        /// Operation visitor to flow the copy values across a given statement in a basic block.
        /// </summary>
        private sealed class CopyDataFlowOperationVisitor : AnalysisEntityDataFlowOperationVisitor<CopyAnalysisData, CopyAbstractValue>
        {
            public CopyDataFlowOperationVisitor(
                CopyAbstractValueDomain valueDomain,
                ISymbol owningSymbol,
                WellKnownTypeProvider wellKnownTypeProvider,
                ControlFlowGraph cfg,
                bool pessimisticAnalysis,
                DataFlowAnalysisResult<PointsToAnalysis.PointsToBlockAnalysisResult, PointsToAnalysis.PointsToAbstractValue> pointsToAnalysisResultOpt)
                : base(valueDomain, owningSymbol, wellKnownTypeProvider, cfg, pessimisticAnalysis,
                      predicateAnalysis: true, copyAnalysisResultOpt: null, pointsToAnalysisResultOpt: pointsToAnalysisResultOpt)
            {
            }

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

            protected override void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder) => CurrentAnalysisData.AddTrackedEntities(builder);

            protected override bool HasAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.HasAbstractValue(analysisEntity);

            protected override bool HasAnyAbstractValue(CopyAnalysisData data) => data.HasAnyAbstractValue;

            protected override void StopTrackingEntity(AnalysisEntity analysisEntity)
            {
                AssertValidCopyAnalysisData(CurrentAnalysisData);
                
                // First set the value to unknown so we remove the entity from existing copy sets.
                SetAbstractValue(analysisEntity, CopyAbstractValue.Unknown);
                AssertValidCopyAnalysisData(CurrentAnalysisData);

                // Now it should be safe to remove the entry.
                CurrentAnalysisData.RemoveEntries(analysisEntity);
                AssertValidCopyAnalysisData(CurrentAnalysisData);
            }

            protected override CopyAbstractValue GetAbstractValue(AnalysisEntity analysisEntity) => CurrentAnalysisData.TryGetValue(analysisEntity, out var value) ? value : CopyAbstractValue.Unknown;

            protected override CopyAbstractValue GetCopyAbstractValue(IOperation operation) => base.GetCachedAbstractValue(operation);
            
            protected override CopyAbstractValue GetAbstractDefaultValue(ITypeSymbol type) => CopyAbstractValue.NotApplicable;

            protected override void SetAbstractValue(AnalysisEntity analysisEntity, CopyAbstractValue value)
            {
                Debug.Assert(analysisEntity != null);
                Debug.Assert(value != null);

                SetAbstractValue(CurrentAnalysisData, analysisEntity, value, fromPredicate: false);
            }

            private static void SetAbstractValue(CopyAnalysisData copyAnalysisData, AnalysisEntity analysisEntity, CopyAbstractValue value, bool fromPredicate)
            {
                SetAbstractValue(sourceCopyAnalysisData: copyAnalysisData, targetCopyAnalysisData: copyAnalysisData,
                    analysisEntity: analysisEntity, value: value, fromPredicate: fromPredicate);
            }

            private static void SetAbstractValue(CopyAnalysisData sourceCopyAnalysisData, CopyAnalysisData targetCopyAnalysisData, AnalysisEntity analysisEntity, CopyAbstractValue value, bool fromPredicate)
            {
                AssertValidCopyAnalysisData(sourceCopyAnalysisData);
                AssertValidCopyAnalysisData(targetCopyAnalysisData);
                Debug.Assert(ReferenceEquals(sourceCopyAnalysisData, targetCopyAnalysisData) || fromPredicate);

                // Don't track entities if do not know about it's instance location.
                if (analysisEntity.HasUnknownInstanceLocation)
                {
                    return;
                }

                if (value.AnalysisEntities.Count > 0)
                {
                    if (sourceCopyAnalysisData.TryGetValue(value.AnalysisEntities.First(), out var fixedUpValue))
                    {
                        value = fixedUpValue;
                    }

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
                        var newValueForEntitiesInOldSet = existingValue.WithEntityRemoved(analysisEntity);
                        foreach (var entityToUpdate in newValueForEntitiesInOldSet.AnalysisEntities)
                        {
                            Debug.Assert(newValueForEntitiesInOldSet.AnalysisEntities.Contains(entityToUpdate));
                            targetCopyAnalysisData.SetAbstactValue(entityToUpdate, newValueForEntitiesInOldSet, isEntityBeingAssigned: false);
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

                var newValue = new CopyAbstractValue(newAnalysisEntities);
                foreach (var entityToUpdate in newAnalysisEntities)
                {
                    Debug.Assert(newValue.AnalysisEntities.Count > 0);
                    Debug.Assert(newValue.AnalysisEntities.Contains(entityToUpdate));
                    targetCopyAnalysisData.SetAbstactValue(entityToUpdate, newValue, isEntityBeingAssigned: entityToUpdate == analysisEntity);
                }

                AssertValidCopyAnalysisData(targetCopyAnalysisData);
            }

            protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                // Create a dummy copy value for each parameter.
                SetAbstractValue(analysisEntity, new CopyAbstractValue(analysisEntity));
            }

            protected override void SetValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            {
                // Do not escape the copy value for parameter at exit.
            }

            protected override void ResetCurrentAnalysisData() => CurrentAnalysisData.Reset(ValueDomain.UnknownOrMayBeValue);

            protected override CopyAbstractValue ComputeAnalysisValueForReferenceOperation(IOperation operation, CopyAbstractValue defaultValue)
            {
                if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
                {
                    return CurrentAnalysisData.TryGetValue(analysisEntity, out CopyAbstractValue value) ? value : new CopyAbstractValue(analysisEntity);
                }
                else
                {
                    return defaultValue;
                }
            }

            protected override CopyAbstractValue ComputeAnalysisValueForOutArgument(AnalysisEntity analysisEntity, IArgumentOperation operation, CopyAbstractValue defaultValue)
            {
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
                        rightValue = new CopyAbstractValue(rightEntity);
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
                                SetAbstractValue(targetAnalysisData, entity, CopyAbstractValue.Invalid, fromPredicate: true);
                            }
                        }

                        return predicateKind;
                    }

                    if (equals)
                    {
                        SetAbstractValue(targetAnalysisData, leftEntity, rightValue, fromPredicate: true);
                    }
                }

                return PredicateValueKind.Unknown;
            }

            protected override PredicateValueKind SetValueForIsNullComparisonOperator(IOperation leftOperand, bool equals, CopyAnalysisData copyAnalysisData) => PredicateValueKind.Unknown;
            protected override CopyAnalysisData GetEmptyAnalysisDataForPredicateAnalysis() => new CopyAnalysisData();
            
            #endregion

            protected override CopyAnalysisData MergeAnalysisData(CopyAnalysisData value1, CopyAnalysisData value2)
                => s_AnalysisDomain.Merge(value1, value2);
            protected override CopyAnalysisData GetClonedAnalysisData(CopyAnalysisData analysisData)
                => (CopyAnalysisData)analysisData.Clone();
            protected override bool Equals(CopyAnalysisData value1, CopyAnalysisData value2)
                => value1.Equals(value2);

            #region Visitor overrides
            public override CopyAbstractValue DefaultVisit(IOperation operation, object argument)
            {
                var _ = base.DefaultVisit(operation, argument);
                return CopyAbstractValue.Unknown;
            }

            public override CopyAbstractValue VisitConversion(IConversionOperation operation, object argument)
            {
                var operandValue = Visit(operation.Operand, argument);

                if (TryInferConversion(operation, out bool alwaysSucceed, out bool alwaysFail))
                {
                    Debug.Assert(!alwaysSucceed || !alwaysFail);
                    
                    // Flow the copy value of the operand to the converted operation if conversion may succeed.
                    if (!alwaysFail)
                    {
                        // For try cast, also ensure conversion always succeeds before flowing copy value.
                        // TODO: For direct cast, we should check if conversion is implicit.
                        // For now, we only flow values for reference type direct cast conversions.
                        if (operation.IsTryCast && alwaysSucceed ||
                            !operation.IsTryCast && operation.Type.IsReferenceType)
                        {
                            return operandValue;
                        }
                    }
                }

                return CopyAbstractValue.Unknown;
            }
            #endregion
        }
    }
}
