// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Operation visitor to flow the abstract dataflow analysis values for <see cref="AnalysisEntity"/> instances across a given statement in a basic block.
    /// </summary>
    internal abstract class AnalysisEntityDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : DataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult: IDataFlowAnalysisResult<TAbstractAnalysisValue>
    {
        protected AnalysisEntityDataFlowOperationVisitor(TAnalysisContext analysisContext)
            : base(analysisContext)
        {
        }

        protected abstract void AddTrackedEntities(PooledHashSet<AnalysisEntity> builder);
        protected abstract void SetAbstractValue(AnalysisEntity analysisEntity, TAbstractAnalysisValue value);
        protected abstract TAbstractAnalysisValue GetAbstractValue(AnalysisEntity analysisEntity);
        protected abstract bool HasAbstractValue(AnalysisEntity analysisEntity);
        protected abstract void StopTrackingEntity(AnalysisEntity analysisEntity);

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
                SetAbstractValue(key, ValueDomain.UnknownOrMayBeValue);
            }
        }

        protected override void OnLeavingRegion(ControlFlowRegion region)
        {
            base.OnLeavingRegion(region);

            // Stop tracking entities for locals and capture Ids that are now out of scope.
            foreach (var local in region.Locals)
            {
                var success = AnalysisEntityFactory.TryCreateForSymbolDeclaration(local, out var analysisEntity);
                Debug.Assert(success);

                StopTrackingDataForEntity(analysisEntity);
            }

            foreach (var captureId in region.CaptureIds)
            {
                if (AnalysisEntityFactory.TryGetForFlowCapture(captureId, out var analysisEntity))
                {
                    StopTrackingDataForEntity(analysisEntity);
                }
            }
        }

        private void StopTrackingDataForEntity(AnalysisEntity analysisEntity)
        {
            // Stop tracking entity that is now out of scope.
            StopTrackingEntity(analysisEntity);

            // Additionally, stop tracking all the child entities if the entity type has value copy semantics.
            if (analysisEntity.Type.HasValueCopySemantics())
            {
                foreach (var childEntity in GetChildAnalysisEntities(analysisEntity))
                {
                    StopTrackingEntity(childEntity);
                }
            }
        }

        protected override void StopTrackingDataForParameter(IParameterSymbol parameter, AnalysisEntity analysisEntity)
            => StopTrackingDataForEntity(analysisEntity);

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

        protected virtual void SetAbstractValueForAssignment(AnalysisEntity targetAnalysisEntity, IOperation assignedValueOperation, TAbstractAnalysisValue assignedValue)
        {
            // Value type and string type assignment has copy semantics.
            if (HasPointsToAnalysisResult &&
                targetAnalysisEntity.Type.HasValueCopySemantics())
            {
                // Reset the analysis values for analysis entities within the target instance.
                ResetValueTypeInstanceAnalysisData(targetAnalysisEntity);

                if (assignedValueOperation != null)
                {
                    // Transfer the values of symbols from the assigned instance to the analysis entities in the target instance.
                    TransferValueTypeInstanceAnalysisDataForAssignment(targetAnalysisEntity, assignedValueOperation);
                }
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
            Debug.Assert(analysisEntity.SymbolOpt == parameter);
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
            Debug.Assert(analysisEntity.SymbolOpt == parameter);
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
                // Reset value.
                SetAbstractValue(dependentAnalysisEntity, ValueDomain.UnknownOrMayBeValue);
            }
        }

        /// <summary>
        /// Transfers the analysis data rooted from <paramref name="assignedValueOperation"/> to <paramref name="targetAnalysisEntity"/>, for a value type assignment operation.
        /// This involves transfer of data for of all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/> as the valueAnalysisEntity for the <paramref name="assignedValueOperation"/>
        /// to all <see cref="AnalysisEntity"/> instances that share the same <see cref="AnalysisEntity.InstanceLocation"/> as <paramref name="targetAnalysisEntity"/>.
        /// </summary>
        private void TransferValueTypeInstanceAnalysisDataForAssignment(AnalysisEntity targetAnalysisEntity, IOperation assignedValueOperation)
        {
            Debug.Assert(HasPointsToAnalysisResult);
            Debug.Assert(targetAnalysisEntity.Type.HasValueCopySemantics());

            IEnumerable<AnalysisEntity> dependentAnalysisEntities;
            if (AnalysisEntityFactory.TryCreate(assignedValueOperation, out AnalysisEntity valueAnalysisEntity))
            {
                dependentAnalysisEntities = GetChildAnalysisEntities(valueAnalysisEntity);
            }
            else
            {
                // For allocations.
                PointsToAbstractValue newValueLocation = GetPointsToAbstractValue(assignedValueOperation);
                dependentAnalysisEntities = GetChildAnalysisEntities(newValueLocation);
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
            if (analysisEntity.InstanceLocation == null)
            {
                return ImmutableHashSet<AnalysisEntity>.Empty;

            }

            var hasValueCopySemantics = analysisEntity.Type.HasValueCopySemantics();
            return GetChildAnalysisEntities(analysisEntity.InstanceLocation, entity => !hasValueCopySemantics || entity.HasAncestor(analysisEntity));
        }

        protected ImmutableHashSet<AnalysisEntity> GetChildAnalysisEntities(PointsToAbstractValue instanceLocationOpt)
           => GetChildAnalysisEntities(instanceLocationOpt, predicateOpt: null);

        private ImmutableHashSet<AnalysisEntity> GetChildAnalysisEntities(PointsToAbstractValue instanceLocationOpt, Func<AnalysisEntity, bool> predicateOpt)
        {
            // We are interested only in dependent child/member infos, not the root info.
            if (instanceLocationOpt == null)
            {
                return ImmutableHashSet<AnalysisEntity>.Empty;
            }

            return GetChildAnalysisEntities(entity =>
                entity.InstanceLocation.Equals(instanceLocationOpt) &&
                entity.IsChildOrInstanceMember &&
                (predicateOpt == null || predicateOpt(entity)));
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
            var predicatedData = analysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>;
            if (predicatedData != null)
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
        protected sealed override bool HasPredicatedDataForEntity(AnalysisEntity predicatedEntity)
            => (CurrentAnalysisData as AnalysisEntityBasedPredicateAnalysisData<TAbstractAnalysisValue>)?.HasPredicatedDataForEntity(predicatedEntity) == true;
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

        protected DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue> GetClonedAnalysisDataHelper(IDictionary<AnalysisEntity, TAbstractAnalysisValue> analysisData)
            => new DictionaryAnalysisData<AnalysisEntity, TAbstractAnalysisValue>(analysisData);
    }
}