// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Analyzer.Utilities.PooledObjects.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Operation visitor to flow the abstract dataflow analysis values for <see cref="AnalysisEntity"/> instances across a given statement in a basic block.
    /// </summary>
    public abstract class AnalysisEntityDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TAbstractAnalysisValue : IEquatable<TAbstractAnalysisValue>
    {
        protected AnalysisEntityDataFlowOperationVisitor(TAnalysisContext analysisContext)
            : base(analysisContext)
        {
        }

        protected void AddTrackedEntities(HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis = false)
            => AddTrackedEntities(CurrentAnalysisData, builder, forInterproceduralAnalysis);
        protected abstract void AddTrackedEntities(TAnalysisData analysisData, HashSet<AnalysisEntity> builder, bool forInterproceduralAnalysis = false);
        protected abstract void SetAbstractValue(AnalysisEntity analysisEntity, TAbstractAnalysisValue value);
        protected abstract void ResetAbstractValue(AnalysisEntity analysisEntity);
        protected abstract TAbstractAnalysisValue GetAbstractValue(AnalysisEntity analysisEntity);
        protected abstract bool HasAbstractValue(AnalysisEntity analysisEntity);
        protected abstract void StopTrackingEntity(AnalysisEntity analysisEntity, TAnalysisData analysisData);

        protected override TAbstractAnalysisValue ComputeAnalysisValueForReferenceOperation(IOperation operation, TAbstractAnalysisValue defaultValue)
        {
            if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
            {
                if (!HasAbstractValue(analysisEntity))
                {
                    SetAbstractValue(analysisEntity, defaultValue);
                }

                return GetAbstractValue(analysisEntity);
            }
            else
            {
                return defaultValue;
            }
        }

        protected sealed override TAbstractAnalysisValue ComputeAnalysisValueForEscapedRefOrOutArgument(IArgumentOperation operation, TAbstractAnalysisValue defaultValue)
        {
            Debug.Assert(operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out);

            if (AnalysisEntityFactory.TryCreate(operation, out AnalysisEntity analysisEntity))
            {
                var value = ComputeAnalysisValueForEscapedRefOrOutArgument(analysisEntity, operation, defaultValue);
                SetAbstractValueForAssignment(analysisEntity, operation, value);
                return GetAbstractValue(analysisEntity);
            }
            else
            {
                return defaultValue;
            }
        }

        protected virtual TAbstractAnalysisValue ComputeAnalysisValueForEscapedRefOrOutArgument(AnalysisEntity analysisEntity, IArgumentOperation operation, TAbstractAnalysisValue defaultValue)
        {
            Debug.Assert(operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out);

            return defaultValue;
        }

        /// <summary>
        /// Helper method to reset analysis data for analysis entities.
        /// </summary>
        protected void ResetAnalysisData(DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> currentAnalysisDataOpt)
        {
            // Reset the current analysis data, while ensuring that we don't violate the monotonicity, i.e. we cannot remove any existing key from currentAnalysisData.
            // Just set the values for existing keys to ValueDomain.UnknownOrMayBeValue.
            var keys = currentAnalysisDataOpt?.Keys.ToImmutableArray();
            foreach (var key in keys)
            {
                ResetAbstractValue(key);
            }
        }

        protected override void ProcessOutOfScopeLocalsAndFlowCaptures(IEnumerable<ILocalSymbol> locals, IEnumerable<CaptureId> flowCaptures)
        {
            Debug.Assert(locals.Any() || flowCaptures.Any());

            base.ProcessOutOfScopeLocalsAndFlowCaptures(locals, flowCaptures);

            var allEntities = PooledHashSet<AnalysisEntity>.GetInstance();
            try
            {
                AddTrackedEntities(allEntities);

                // Stop tracking entities for locals and capture Ids that are now out of scope.
                foreach (var local in locals)
                {
                    var success = AnalysisEntityFactory.TryCreateForSymbolDeclaration(local, out var analysisEntity);
                    Debug.Assert(success);

                    StopTrackingDataForEntity(analysisEntity, allEntities);
                }

                foreach (var captureId in flowCaptures)
                {
                    if (AnalysisEntityFactory.TryGetForFlowCapture(captureId, out var analysisEntity))
                    {
                        StopTrackingDataForEntity(analysisEntity, allEntities);
                    }
                }
            }
            finally
            {
                allEntities.Free();
            }
        }

        private void StopTrackingDataForEntity(AnalysisEntity analysisEntity, PooledHashSet<AnalysisEntity> allEntities)
            => StopTrackingDataForEntity(analysisEntity, CurrentAnalysisData, allEntities);

        private void StopTrackingDataForEntity(AnalysisEntity analysisEntity, TAnalysisData analysisData, PooledHashSet<AnalysisEntity> allEntities)
        {
            if (!allEntities.Contains(analysisEntity))
            {
                return;
            }

            // Stop tracking entity that is now out of scope.
            StopTrackingEntity(analysisEntity, analysisData);

            // Additionally, stop tracking all the child entities if the entity type has value copy semantics.
            if (analysisEntity.Type.HasValueCopySemantics())
            {
                foreach (var childEntity in GetChildAnalysisEntities(analysisEntity, allEntities))
                {
                    StopTrackingEntity(childEntity, analysisData);
                }
            }
        }

        private void StopTrackingDataForParamArrayParameterIndices(AnalysisEntity analysisEntity, TAnalysisData analysisData, PooledHashSet<AnalysisEntity> allEntities)
        {
            Debug.Assert(analysisEntity.SymbolOpt is IParameterSymbol parameter && parameter.IsParams);

            foreach (var entity in allEntities)
            {
                if (entity.Indices.Length > 0 &&
                    entity.InstanceLocation.Equals(analysisEntity.InstanceLocation))
                {
                    StopTrackingEntity(entity, analysisData);
                }
            }
        }

        protected sealed override void StopTrackingDataForParameter(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            => throw new InvalidOperationException("Unreachable");

        protected sealed override void StopTrackingDataForParameters(ImmutableDictionary<IParameterSymbol, AnalysisEntity> parameterEntities)
        {
            if (parameterEntities.Count > 0)
            {
                var allEntities = PooledHashSet<AnalysisEntity>.GetInstance();

                try
                {
                    AddTrackedEntities(allEntities);

                    foreach (var (parameter, parameterEntity) in parameterEntities)
                    {
                        StopTrackingDataForEntity(parameterEntity, CurrentAnalysisData, allEntities);

                        if (parameter.IsParams)
                        {
                            StopTrackingDataForParamArrayParameterIndices(parameterEntity, CurrentAnalysisData, allEntities);
                        }
                    }
                }
                finally
                {
                    allEntities.Free();
                }
            }
        }

        protected override TAnalysisData GetMergedAnalysisDataForPossibleThrowingOperation(TAnalysisData existingDataOpt, IOperation operation)
        {
            var entitiesBuilder = PooledHashSet<AnalysisEntity>.GetInstance();
            try
            {
                // Get tracked entities.
                AddTrackedEntities(entitiesBuilder);

                // Only non-child entities are tracked for now.
                var resultAnalysisData = GetTrimmedCurrentAnalysisData(entitiesBuilder.Where(e => !e.IsChildOrInstanceMember && HasAbstractValue(e)));
                if (existingDataOpt != null)
                {
                    var mergedAnalysisData = MergeAnalysisData(resultAnalysisData, existingDataOpt);
                    resultAnalysisData.Dispose();
                    resultAnalysisData = mergedAnalysisData;
                }

                return resultAnalysisData;
            }
            finally
            {
                entitiesBuilder.Free();
            }
        }

        #region Helper methods to handle initialization/assignment operations
        protected override void SetAbstractValueForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, IOperation initializer, TAbstractAnalysisValue value)
        {
            if (AnalysisEntityFactory.TryCreateForArrayElementInitializer(arrayCreation, indices, elementType, out AnalysisEntity analysisEntity))
            {
                SetAbstractValueForAssignment(analysisEntity, initializer, value);
            }
        }

        protected override void SetAbstractValueForAssignment(IOperation target, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue, bool mayBeAssignment = false)
        {
            if (AnalysisEntityFactory.TryCreate(target, out AnalysisEntity targetAnalysisEntity))
            {
                if (mayBeAssignment)
                {
                    assignedValue = ValueDomain.Merge(GetAbstractValue(targetAnalysisEntity), assignedValue);
                }

                SetAbstractValueForAssignment(targetAnalysisEntity, assignedValueOperation, assignedValue);
            }
        }

        protected override void SetAbstractValueForTupleElementAssignment(AnalysisEntity tupleElementEntity, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue)
        {
            if (tupleElementEntity.Type.IsTupleType)
            {
                // For nested tuple entity, we only want to flow the value for the tuple, not it's children.
                // Children of nested tuples should have already been assigned when visiting the nested tuple operation in the base dataflow visitor.
                SetAbstractValue(tupleElementEntity, assignedValue);
            }
            else
            {
                SetAbstractValueForAssignment(tupleElementEntity, assignedValueOperation, assignedValue);
            }
        }

        protected virtual void SetAbstractValueForAssignment(AnalysisEntity targetAnalysisEntity, IOperation assignedValueOperationOpt, TAbstractAnalysisValue assignedValue)
        {
            AnalysisEntity assignedValueEntityOpt = null;
            if (assignedValueOperationOpt != null)
            {
                var success = AnalysisEntityFactory.TryCreate(assignedValueOperationOpt, out assignedValueEntityOpt);
                Debug.Assert(success || assignedValueEntityOpt == null);
            }

            SetAbstractValueForAssignment(targetAnalysisEntity, assignedValueEntityOpt, assignedValueOperationOpt, assignedValue);
        }

        private void SetAbstractValueForAssignment(AnalysisEntity targetAnalysisEntity, AnalysisEntity assignedValueEntityOpt, IOperation assignedValueOperationOpt, TAbstractAnalysisValue assignedValue)
        {
            // Value type and string type assignment has copy semantics.
            if (HasPointsToAnalysisResult &&
                targetAnalysisEntity.Type.HasValueCopySemantics())
            {
                // Reset the analysis values for analysis entities within the target instance.
                ResetValueTypeInstanceAnalysisData(targetAnalysisEntity);

                // Transfer the values of symbols from the assigned instance to the analysis entities in the target instance.
                TransferValueTypeInstanceAnalysisDataForAssignment(targetAnalysisEntity, assignedValueEntityOpt, assignedValueOperationOpt);
            }

            var addressSharedCopyValue = TryGetAddressSharedCopyValue(targetAnalysisEntity);
            if (addressSharedCopyValue != null)
            {
                Debug.Assert(addressSharedCopyValue.AnalysisEntities.Contains(targetAnalysisEntity));
                foreach (var entity in addressSharedCopyValue.AnalysisEntities)
                {
                    SetAbstractValue(entity, assignedValue);
                }
            }
            else
            {
                SetAbstractValue(targetAnalysisEntity, assignedValue);
            }
        }

        protected override void SetValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<TAbstractAnalysisValue> assignedValueOpt)
        {
            Debug.Assert(Equals(analysisEntity.SymbolOpt, parameter));
            if (assignedValueOpt != null)
            {
                SetAbstractValueForAssignment(analysisEntity, assignedValueOpt.Operation, assignedValueOpt.Value);
            }
            else
            {
                SetAbstractValue(analysisEntity, GetDefaultValueForParameterOnEntry(parameter, analysisEntity));
            }
        }

        protected override void EscapeValueForParameterOnExit(IParameterSymbol parameter, AnalysisEntity analysisEntity)
        {
            Debug.Assert(Equals(analysisEntity.SymbolOpt, parameter));
            if (parameter.RefKind != RefKind.None)
            {
                SetAbstractValue(analysisEntity, GetDefaultValueForParameterOnExit(analysisEntity.Type));
            }
        }

        protected virtual TAbstractAnalysisValue GetDefaultValueForParameterOnEntry(IParameterSymbol parameter, AnalysisEntity analysisEntity) => ValueDomain.UnknownOrMayBeValue;
        protected virtual TAbstractAnalysisValue GetDefaultValueForParameterOnExit(ITypeSymbol parameterType) => ValueDomain.UnknownOrMayBeValue;

        #endregion

        #region Helper methods for reseting/transfer instance analysis data when PointsTo analysis results are available

        /// <summary>
        /// Resets all the analysis data for all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/>
        /// as the given <paramref name="analysisEntity"/>.
        /// </summary>
        /// <param name="analysisEntity"></param>
        protected override void ResetValueTypeInstanceAnalysisData(AnalysisEntity analysisEntity)
        {
            Debug.Assert(HasPointsToAnalysisResult);
            Debug.Assert(analysisEntity.Type.HasValueCopySemantics());

            IEnumerable<AnalysisEntity> dependantAnalysisEntities = GetChildAnalysisEntities(analysisEntity);
            ResetInstanceAnalysisDataCore(dependantAnalysisEntities.Concat(analysisEntity));
        }

        protected override void ResetReferenceTypeInstanceAnalysisData(PointsToAbstractValue pointsToValue)
        {
            Debug.Assert(HasPointsToAnalysisResult);

            IEnumerable<AnalysisEntity> dependantAnalysisEntities = GetChildAnalysisEntities(pointsToValue);
            ResetInstanceAnalysisDataCore(dependantAnalysisEntities);
        }

        /// <summary>
        /// Resets the analysis data for the given <paramref name="dependantAnalysisEntities"/>.
        /// </summary>
        /// <param name="dependantAnalysisEntities"></param>
        private void ResetInstanceAnalysisDataCore(IEnumerable<AnalysisEntity> dependantAnalysisEntities)
        {
            foreach (var dependentAnalysisEntity in dependantAnalysisEntities)
            {
                ResetAbstractValue(dependentAnalysisEntity);
            }
        }

        /// <summary>
        /// Transfers the analysis data rooted from <paramref name="valueAnalysisEntityOpt"/> or <paramref name="assignedValueOperationOpt"/> to <paramref name="targetAnalysisEntity"/>, for a value type assignment operation.
        /// This involves transfer of data for of all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/> as <paramref name="valueAnalysisEntityOpt"/> or allocation for the <paramref name="assignedValueOperationOpt"/>
        /// to all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/> as <paramref name="targetAnalysisEntity"/>.
        /// </summary>
        private void TransferValueTypeInstanceAnalysisDataForAssignment(AnalysisEntity targetAnalysisEntity, AnalysisEntity valueAnalysisEntityOpt, IOperation assignedValueOperationOpt)
        {
            Debug.Assert(HasPointsToAnalysisResult);
            Debug.Assert(targetAnalysisEntity.Type.HasValueCopySemantics());

            IEnumerable<AnalysisEntity> dependentAnalysisEntities;
            if (valueAnalysisEntityOpt != null)
            {
                if (!valueAnalysisEntityOpt.Type.HasValueCopySemantics())
                {
                    // Unboxing conversion from assigned value (reference type) to target (value copy semantics).
                    // We do not need to transfer any data for such a case as there is no entity for unboxed value.
                    return;
                }

                dependentAnalysisEntities = GetChildAnalysisEntities(valueAnalysisEntityOpt);
            }
            else if (assignedValueOperationOpt != null)
            {
                // For allocations.
                PointsToAbstractValue newValueLocation = GetPointsToAbstractValue(assignedValueOperationOpt);
                dependentAnalysisEntities = GetChildAnalysisEntities(newValueLocation);
            }
            else
            {
                return;
            }

            foreach (AnalysisEntity dependentInstance in dependentAnalysisEntities)
            {
                // Clone the dependent instance but with with target as the root.
                AnalysisEntity newAnalysisEntity = AnalysisEntityFactory.CreateWithNewInstanceRoot(dependentInstance, targetAnalysisEntity);
                var dependentValue = GetAbstractValue(dependentInstance);
                SetAbstractValue(newAnalysisEntity, dependentValue);
            }
        }

        private ImmutableHashSet<AnalysisEntity> GetChildAnalysisEntities(AnalysisEntity analysisEntity)
        {
            return GetChildAnalysisEntities(analysisEntity.InstanceLocation, entity => IsChildAnalysisEntity(entity, analysisEntity));
        }

        protected static IEnumerable<AnalysisEntity> GetChildAnalysisEntities(AnalysisEntity analysisEntity, HashSet<AnalysisEntity> allEntities)
        {
            foreach (var entity in allEntities)
            {
                if (IsChildAnalysisEntity(entity, ancestorEntity: analysisEntity))
                {
                    yield return entity;
                }
            }
        }

        protected static bool IsChildAnalysisEntity(AnalysisEntity entity, AnalysisEntity ancestorEntity)
        {
            return (!ancestorEntity.Type.HasValueCopySemantics() || entity.HasAncestor(ancestorEntity)) &&
                IsChildAnalysisEntity(entity, ancestorEntity.InstanceLocation);
        }

        protected ImmutableHashSet<AnalysisEntity> GetChildAnalysisEntities(PointsToAbstractValue instanceLocationOpt)
           => GetChildAnalysisEntities(instanceLocationOpt, predicateOpt: null);

        private ImmutableHashSet<AnalysisEntity> GetChildAnalysisEntities(PointsToAbstractValue instanceLocationOpt, Func<AnalysisEntity, bool> predicateOpt)
        {
            // We are interested only in dependent child/member infos, not the root info.
            if (instanceLocationOpt == null || instanceLocationOpt.Kind == PointsToAbstractValueKind.Unknown)
            {
                return ImmutableHashSet<AnalysisEntity>.Empty;
            }

            if (predicateOpt == null)
            {
                predicateOpt = entity => IsChildAnalysisEntity(entity, instanceLocationOpt);
            }

            return GetChildAnalysisEntities(predicateOpt);
        }

        protected static bool IsChildAnalysisEntity(AnalysisEntity entity, PointsToAbstractValue instanceLocation)
        {
            return instanceLocation != PointsToAbstractValue.NoLocation &&
                entity.InstanceLocation.Equals(instanceLocation) &&
                entity.IsChildOrInstanceMember;
        }

        private ImmutableHashSet<AnalysisEntity> GetChildAnalysisEntities(Func<AnalysisEntity, bool> predicate)
        {
            var trackedEntitiesBuilder = PooledHashSet<AnalysisEntity>.GetInstance();
            AddTrackedEntities(trackedEntitiesBuilder);
            trackedEntitiesBuilder.RemoveWhere(entity => !predicate(entity));
            return trackedEntitiesBuilder.ToImmutableAndFree();
        }

        #endregion

        #region Predicate analysis
        protected override void UpdateReachability(BasicBlock basicBlock, TAnalysisData analysisData, bool isReachable)
        {
            Debug.Assert(PredicateAnalysis);
            if (analysisData is AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue> predicatedData)
            {
                Debug.Assert(!isReachable || predicatedData.IsReachableBlockData);
                predicatedData.IsReachableBlockData = isReachable;
            }
        }

        protected override bool IsReachableBlockData(TAnalysisData analysisData)
            => (analysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.IsReachableBlockData ?? true;

        protected sealed override void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, TAnalysisData truePredicateData, TAnalysisData falsePredicateData)
                => (CurrentAnalysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.StartTrackingPredicatedData(
                        predicatedEntity,
                        truePredicateData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>,
                        falsePredicateData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>);
        protected sealed override void StopTrackingPredicatedData(AnalysisEntity predicatedEntity)
            => (CurrentAnalysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.StopTrackingPredicatedData(predicatedEntity);
        protected sealed override bool HasPredicatedDataForEntity(TAnalysisData analysisData, AnalysisEntity predicatedEntity)
            => (analysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.HasPredicatedDataForEntity(predicatedEntity) == true;
        protected sealed override void TransferPredicatedData(AnalysisEntity fromEntity, AnalysisEntity toEntity)
            => (CurrentAnalysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.TransferPredicatedData(fromEntity, toEntity);
        protected sealed override PredicateValueKind ApplyPredicatedDataForEntity(TAnalysisData analysisData, AnalysisEntity predicatedEntity, bool trueData)
            => (analysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.ApplyPredicatedDataForEntity(predicatedEntity, trueData) ?? PredicateValueKind.Unknown;
        protected override void SetPredicateValueKind(IOperation operation, TAnalysisData analysisData, PredicateValueKind predicateValueKind)
        {
            base.SetPredicateValueKind(operation, analysisData, predicateValueKind);
            if (predicateValueKind == PredicateValueKind.AlwaysFalse)
            {
                (analysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>).IsReachableBlockData = false;
            }
        }
        #endregion

        #region Interprocedural analysis
        protected override TAnalysisData GetInitialInterproceduralAnalysisData(
            IMethodSymbol invokedMethod,
            (AnalysisEntity InstanceOpt, PointsToAbstractValue PointsToValue)? invocationInstanceOpt,
            (AnalysisEntity Instance, PointsToAbstractValue PointsToValue)? thisOrMeInstanceForCallerOpt,
            ImmutableDictionary<IParameterSymbol, ArgumentInfo<TAbstractAnalysisValue>> argumentValuesMap,
            IDictionary<AnalysisEntity, PointsToAbstractValue> pointsToValuesOpt,
            IDictionary<AnalysisEntity, CopyAbstractValue> copyValuesOpt,
            IDictionary<AnalysisEntity, ValueContentAbstractValue> valueContentValuesOpt,
            bool isLambdaOrLocalFunction,
            bool hasParameterWithDelegateType)
        {
            // PERF: For non-lambda and local functions + presence of points to values, we trim down
            // the initial analysis data passed as input to interprocedural analysis.
            // We retain the analysis entities for the invocation instance, arguments and this or me instance.
            // Additionally, we also retain the transitive closure of analysis entities reachable from these
            // entities via the PointsTo values chain (i.e., recursively compute child analysis entities).
            // All the remaining entities are not accessible in the callee and are excluded from the initial
            // interprocedural analysis data.

            if (isLambdaOrLocalFunction || hasParameterWithDelegateType || pointsToValuesOpt == null)
            {
                return base.GetInitialInterproceduralAnalysisData(invokedMethod, invocationInstanceOpt,
                    thisOrMeInstanceForCallerOpt, argumentValuesMap, pointsToValuesOpt, copyValuesOpt, valueContentValuesOpt,
                    isLambdaOrLocalFunction, hasParameterWithDelegateType);
            }

            var candidateEntitiesBuilder = PooledHashSet<AnalysisEntity>.GetInstance();
            var interproceduralEntitiesToRetainBuilder = PooledHashSet<AnalysisEntity>.GetInstance();
            var worklistEntities = PooledHashSet<AnalysisEntity>.GetInstance();
            var worklistPointsToValues = PooledHashSet<PointsToAbstractValue>.GetInstance();
            var processedPointsToValues = PooledHashSet<PointsToAbstractValue>.GetInstance();
            var childWorklistEntities = PooledHashSet<AnalysisEntity>.GetInstance();

            try
            {
                // All tracked entities are candidates to be retained for initial interprocedural
                // analysis data.
                AddTrackedEntities(candidateEntitiesBuilder, forInterproceduralAnalysis: true);
                var candidateEntitiesCount = candidateEntitiesBuilder.Count;

                // Add entities and PointsTo values for invocation instance, this or me instance
                // and argument values to the initial worklist

                if (invocationInstanceOpt.HasValue)
                {
                    AddWorklistEntityAndPointsToValue(invocationInstanceOpt.Value.InstanceOpt);
                    AddWorklistPointsToValue(invocationInstanceOpt.Value.PointsToValue);
                }

                if (thisOrMeInstanceForCallerOpt.HasValue)
                {
                    AddWorklistEntityAndPointsToValue(thisOrMeInstanceForCallerOpt.Value.Instance);
                    AddWorklistPointsToValue(thisOrMeInstanceForCallerOpt.Value.PointsToValue);
                }

                foreach (var argument in argumentValuesMap.Values)
                {
                    if (!AddWorklistEntityAndPointsToValue(argument.AnalysisEntityOpt))
                    {
                        // For allocations passed as arguments.
                        AddWorklistPointsToValue(argument.InstanceLocation);
                    }
                }

                // Worklist based algorithm to compute the transitive closure of analysis entities
                // that are accessible in the callee via the PointsTo value chain.
                while (worklistEntities.Count > 0 || worklistPointsToValues.Count > 0)
                {
                    if (worklistEntities.Count > 0)
                    {
                        // Add all the worklistEntities to interproceduralEntitiesBuilder
                        // to ensure these entities are retained.
                        interproceduralEntitiesToRetainBuilder.AddRange(worklistEntities);

                        // Remove the worklistEntities from tracked candidate entities.
                        candidateEntitiesBuilder.ExceptWith(worklistEntities);

                        // Add child entities of worklistEntities to childWorklistEntities.
                        foreach (var candidateEntity in candidateEntitiesBuilder)
                        {
                            foreach (var ancestorEntity in worklistEntities)
                            {
                                if (IsChildAnalysisEntity(candidateEntity, ancestorEntity))
                                {
                                    childWorklistEntities.Add(candidateEntity);
                                    break;
                                }
                            }
                        }

                        worklistEntities.Clear();
                    }

                    if (worklistPointsToValues.Count > 0)
                    {
                        // Add child entities which are accessible from PointsTo chain to childWorklistEntities.
                        foreach (var candidateEntity in candidateEntitiesBuilder)
                        {
                            foreach (var pointsToValue in worklistPointsToValues)
                            {
                                Debug.Assert(ShouldProcessPointsToValue(pointsToValue));
                                if (IsChildAnalysisEntity(candidateEntity, pointsToValue))
                                {
                                    childWorklistEntities.Add(candidateEntity);
                                    break;
                                }
                            }
                        }

                        worklistPointsToValues.Clear();
                    }

                    // Move all the child work list entities and their PointsTo values to the worklist.
                    foreach (var childEntity in childWorklistEntities)
                    {
                        AddWorklistEntityAndPointsToValue(childEntity);
                    }

                    childWorklistEntities.Clear();
                }

                // If all candidates being retained, just retain the cloned current analysis data.
                if (interproceduralEntitiesToRetainBuilder.Count == candidateEntitiesCount)
                {
                    return GetClonedCurrentAnalysisData();
                }

                // Otherwise, return cloned current analysis data with trimmed keys.
                return GetTrimmedCurrentAnalysisData(interproceduralEntitiesToRetainBuilder);
            }
            finally
            {
                candidateEntitiesBuilder.Free();
                interproceduralEntitiesToRetainBuilder.Free();
                worklistEntities.Free();
                worklistPointsToValues.Free();
                processedPointsToValues.Free();
                childWorklistEntities.Free();
            }

            // Local functions.
            bool AddWorklistEntityAndPointsToValue(AnalysisEntity analysisEntityOpt)
            {
                if (analysisEntityOpt != null && candidateEntitiesBuilder.Contains(analysisEntityOpt))
                {
                    worklistEntities.Add(analysisEntityOpt);

                    if (pointsToValuesOpt.TryGetValue(analysisEntityOpt, out var pointsToValue))
                    {
                        AddWorklistPointsToValue(pointsToValue);
                    }

                    return true;
                }

                return false;
            }

            void AddWorklistPointsToValue(PointsToAbstractValue pointsToValue)
            {
                if (ShouldProcessPointsToValue(pointsToValue) &&
                    processedPointsToValues.Add(pointsToValue))
                {
                    worklistPointsToValues.Add(pointsToValue);
                }
            }

            static bool ShouldProcessPointsToValue(PointsToAbstractValue pointsToValue)
                => pointsToValue.Kind == PointsToAbstractValueKind.KnownLocations &&
                   pointsToValue != PointsToAbstractValue.NoLocation;
        }

        /// <summary>
        /// Returns a cloned CurrentAnalysisData, trimmed down to only have key-value pairs for the given <paramref name="withEntities"/>.
        /// </summary>
        protected abstract TAnalysisData GetTrimmedCurrentAnalysisData(IEnumerable<AnalysisEntity> withEntities);

        protected TAnalysisData GetTrimmedCurrentAnalysisDataHelper(
            IEnumerable<AnalysisEntity> withEntities,
            IDictionary<AnalysisEntity, TAbstractAnalysisValue> existingValues,
            Action<TAnalysisData, AnalysisEntity, TAbstractAnalysisValue> setAbstractValue)
        {
            var initialAnalysisData = GetEmptyAnalysisData();
            foreach (var entity in withEntities)
            {
                setAbstractValue(initialAnalysisData, entity, existingValues[entity]);
            }

            return initialAnalysisData;
        }

        protected abstract void ApplyInterproceduralAnalysisResultCore(TAnalysisData resultData);

        protected sealed override void ApplyInterproceduralAnalysisResult(
            TAnalysisData resultData,
            bool isLambdaOrLocalFunction,
            bool hasParameterWithDelegateType,
            TAnalysisResult interproceduralResult)
        {
            if (isLambdaOrLocalFunction || hasParameterWithDelegateType)
            {
                base.ApplyInterproceduralAnalysisResult(resultData, isLambdaOrLocalFunction, hasParameterWithDelegateType, interproceduralResult);
                return;
            }

            ApplyInterproceduralAnalysisResultCore(resultData);
        }

        protected void ApplyInterproceduralAnalysisResultHelper(IDictionary<AnalysisEntity, TAbstractAnalysisValue> resultToApply)
        {
            foreach (var kvp in resultToApply)
            {
                var entity = kvp.Key;
                var newValue = kvp.Value;
                var currentValue = GetAbstractValue(entity);
                if (!currentValue.Equals(newValue))
                {
                    SetAbstractValue(entity, newValue);
                }
            }
        }

        internal bool ShouldStopTrackingEntityAtExit(AnalysisEntity entity)
        {
            Debug.Assert(DataFlowAnalysisContext.InterproceduralAnalysisDataOpt != null);

            // Filter out all the parameter, local symbol and flow capture entities from the analysis data.
            return IsParameterEntityForCurrentMethod(entity) ||
                entity.SymbolOpt?.Kind == SymbolKind.Local &&
                entity.SymbolOpt.ContainingSymbol.Equals(DataFlowAnalysisContext.OwningSymbol) ||
                entity.CaptureIdOpt.HasValue &&
                entity.CaptureIdOpt.Value.ControlFlowGraph == DataFlowAnalysisContext.ControlFlowGraph;
        }

        public override TAnalysisData GetMergedDataForUnhandledThrowOperations()
        {
            // For interprocedural analysis, prune analysis data for unhandled exceptions
            // to remove analysis entities that are only valid in the callee.
            if (DataFlowAnalysisContext.InterproceduralAnalysisDataOpt != null &&
                AnalysisDataForUnhandledThrowOperations != null &&
                AnalysisDataForUnhandledThrowOperations.Values.Any(HasAnyAbstractValue))
            {
                var allAnalysisEntities = PooledHashSet<AnalysisEntity>.GetInstance();

                try
                {
                    foreach (var dataAtException in AnalysisDataForUnhandledThrowOperations.Values)
                    {
                        AddTrackedEntities(dataAtException, allAnalysisEntities, forInterproceduralAnalysis: true);
                    }

                    foreach (var entity in allAnalysisEntities)
                    {
                        if (ShouldStopTrackingEntityAtExit(entity))
                        {
                            foreach (var dataAtException in AnalysisDataForUnhandledThrowOperations.Values)
                            {
                                StopTrackingDataForEntity(entity, dataAtException, allAnalysisEntities);
                            }
                        }
                    }
                }
                finally
                {
                    allAnalysisEntities.Free();
                }
            }

            return base.GetMergedDataForUnhandledThrowOperations();
        }

        #endregion

        protected DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> GetClonedAnalysisDataHelper(IDictionary<AnalysisEntity, TAbstractAnalysisValue> analysisData)
            => new DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue>(analysisData);

        protected void ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(
            DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> coreDataAtException,
            DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> coreCurrentAnalysisData,
            ThrownExceptionInfo throwBranchWithExceptionType)
        {
            Func<AnalysisEntity, bool> predicateOpt = null;
            if (throwBranchWithExceptionType.IsDefaultExceptionForExceptionsPathAnalysis)
            {
                // Only tracking non-child analysis entities for exceptions path analysis for now.
                Debug.Assert(throwBranchWithExceptionType.ExceptionType.Equals(ExceptionNamedType));
                predicateOpt = e => !e.IsChildOrInstanceMember;
            }

            base.ApplyMissingCurrentAnalysisDataForUnhandledExceptionData(coreDataAtException, coreCurrentAnalysisData, predicateOpt);
        }

        #region Visitor methods

        public override TAbstractAnalysisValue VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation, object argument)
        {
            var value = base.VisitDeconstructionAssignment(operation, argument);
            var assignedInstance = GetPointsToAbstractValue(operation.Value);
            HandleDeconstructionAssignment(operation.Target, GetChildAnalysisEntities(assignedInstance));
            return value;
        }

        private void HandleDeconstructionAssignment(IOperation target, ImmutableHashSet<AnalysisEntity> childEntities)
        {
            if (target is IDeclarationExpressionOperation declarationExpressionOperation)
            {
                target = declarationExpressionOperation.Expression;
            }

            if (target is ITupleOperation tupleOperation &&
                AnalysisEntityFactory.TryCreateForTupleElements(tupleOperation, out var tupleElementEntities))
            {
                Debug.Assert(tupleOperation.Elements.Length == tupleElementEntities.Length);
                for (int i = 0; i < tupleOperation.Elements.Length; i++)
                {
                    var element = tupleOperation.Elements[i];
                    var tupleElementEntity = tupleElementEntities[i];
                    if (element is ITupleOperation tupleElement)
                    {
                        Debug.Assert(tupleElementEntity.SymbolOpt is IFieldSymbol field);
                        HandleDeconstructionAssignment(tupleElement, childEntities);
                    }
                    else if (AnalysisEntityFactory.TryCreate(element, out var elementEntity))
                    {
                        var assignedValueEntityOpt = childEntities.FirstOrDefault(c => IsMatchingAssignedEntity(tupleElementEntity, c));
                        var assignedValue = assignedValueEntityOpt != null ? GetAbstractValue(assignedValueEntityOpt) : ValueDomain.UnknownOrMayBeValue;
                        SetAbstractValueForAssignment(elementEntity, assignedValueEntityOpt, assignedValueOperationOpt: null, assignedValue);
                    }
                }
            }

            return;

            // Local function
            static bool IsMatchingAssignedEntity(AnalysisEntity tupleElementEntity, AnalysisEntity childEntity)
            {
                Debug.Assert(tupleElementEntity != null);
                if (childEntity == null)
                {
                    return false;
                }

                if (tupleElementEntity.ParentOpt == null)
                {
                    // Root tuple entity, compare the underlying tuple types.
                    return childEntity.ParentOpt == null &&
                        tupleElementEntity.Type.OriginalDefinition.Equals(childEntity.Type.OriginalDefinition);
                }

                // Must be a tuple element field entity.
                return tupleElementEntity.SymbolOpt is IFieldSymbol tupleElementField &&
                    childEntity.SymbolOpt is IFieldSymbol childEntityField &&
                    tupleElementField.OriginalDefinition.Equals(childEntityField.OriginalDefinition) &&
                    IsMatchingAssignedEntity(tupleElementEntity.ParentOpt, childEntity.ParentOpt);
            }
        }

        #endregion
    }
}