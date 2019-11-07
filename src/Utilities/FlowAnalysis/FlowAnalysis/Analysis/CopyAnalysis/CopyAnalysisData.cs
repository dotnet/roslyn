// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CoreCopyAnalysisData = DictionaryAnalysisData<AnalysisEntity, CopyAbstractValue>;

    /// <summary>
    /// Aggregated copy analysis data tracked by <see cref="CopyAnalysis"/>.
    /// Contains the <see cref="CoreCopyAnalysisData"/> for entity copy values and
    /// the predicated copy values based on true/false runtime values of predicated entities.
    /// </summary>
    public sealed class CopyAnalysisData : AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue>
    {
        internal CopyAnalysisData()
        {
        }

        internal CopyAnalysisData(IDictionary<AnalysisEntity, CopyAbstractValue> fromData)
            : base(fromData)
        {
            AssertValidCopyAnalysisData();
        }

        private CopyAnalysisData(CopyAnalysisData fromData)
            : base(fromData)
        {
            AssertValidCopyAnalysisData();
        }

        private CopyAnalysisData(CopyAnalysisData data1, CopyAnalysisData data2, MapAbstractDomain<AnalysisEntity, CopyAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
            AssertValidCopyAnalysisData();
        }

        public override AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> Clone() => new CopyAnalysisData(this);

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> other, MapAbstractDomain<AnalysisEntity, CopyAbstractValue> coreDataAnalysisDomain)
            => BaseCompareHelper(other, coreDataAnalysisDomain);

        public override AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> data, MapAbstractDomain<AnalysisEntity, CopyAbstractValue> coreDataAnalysisDomain)
        {
            Debug.Assert(IsReachableBlockData || !data.IsReachableBlockData);
            return new CopyAnalysisData(this, (CopyAnalysisData)data, coreDataAnalysisDomain);
        }

        /// <summary>
        /// Updates the the copy values for all entities that are part of the given <paramref name="copyValue"/> set,
        /// i.e. <see cref="CopyAbstractValue.AnalysisEntities"/>.
        /// We do not support the <see cref="SetAbstractValue(AnalysisEntity, CopyAbstractValue)"/> overload
        /// that updates copy value for each individual entity.
        /// </summary>
        internal void SetAbstactValueForEntities(CopyAbstractValue copyValue, AnalysisEntity? entityBeingAssignedOpt)
        {
            foreach (var entity in copyValue.AnalysisEntities)
            {
                // If we have any predicate data based on the previous value of this entity,
                // and we are changing the copy value for an assigment (i.e. entity == entityBeingAssigned),
                // we need to drop all the predicate data based on this entity.
                if (entity == entityBeingAssignedOpt && HasPredicatedDataForEntity(entity))
                {
                    StopTrackingPredicatedData(entity);
                }

                // Remove all predicated values for this entity as we are going to set
                // a new value in CoreAnalysisData below, which is non-predicated.
                if (HasPredicatedData)
                {
                    RemoveEntriesInPredicatedData(entity);
                }

                // Finally, set the value in the core analysis data.
                CoreAnalysisData[entity] = copyValue;
            }
        }

        public override void SetAbstractValue(AnalysisEntity key, CopyAbstractValue value)
        {
            throw new NotSupportedException("Use SetAbstactValueForEntities API");
        }

        protected override void RemoveEntryInPredicatedData(AnalysisEntity key, CoreCopyAnalysisData predicatedData)
        {
            Debug.Assert(HasPredicatedData);

            var hasEntry = predicatedData.TryGetValue(key, out CopyAbstractValue value);
            base.RemoveEntryInPredicatedData(key, predicatedData);

            // If we are removing an entity from predicated data, we need to adjust the copy values of its copy entities.
            if (hasEntry && value.AnalysisEntities.Count > 1)
            {
                var newValueForOldCopyEntities = value.WithEntityRemoved(key);
                if (newValueForOldCopyEntities.AnalysisEntities.Count == 1)
                {
                    predicatedData.Remove(newValueForOldCopyEntities.AnalysisEntities.Single());
                }
                else
                {
                    foreach (var copyEntity in newValueForOldCopyEntities.AnalysisEntities)
                    {
                        predicatedData[copyEntity] = newValueForOldCopyEntities;
                    }
                }
            }
        }

        protected override void ApplyPredicatedData(CoreCopyAnalysisData coreAnalysisData, CoreCopyAnalysisData predicatedData)
        {
            if (predicatedData.Count == 0)
            {
                return;
            }

#if DEBUG
            var originalCoreAnalysisData = new Dictionary<AnalysisEntity, CopyAbstractValue>(coreAnalysisData);
#endif

            AssertValidCopyAnalysisData(coreAnalysisData);
            AssertValidCopyAnalysisData(predicatedData);

            // Applying predicated copy data to current copy analysis data needs us to merge the copy sets of an entity from both the maps.
            foreach (var kvp in predicatedData)
            {
                var predicatedValue = kvp.Value;

                // Check if the entity has a copy value in both the predicated and current copy anaylysis data.
                if (coreAnalysisData.TryGetValue(kvp.Key, out var currentValue))
                {
                    var newCopyEntities = currentValue.AnalysisEntities;
                    var newKind = currentValue.Kind;
                    foreach (var predicatedCopyEntity in predicatedValue.AnalysisEntities)
                    {
                        // Predicate copy value has an entity which is not part of the current copy value.
                        // Include this entity and it's copy entities in the new copy value.
                        if (!newCopyEntities.Contains(predicatedCopyEntity))
                        {
                            if (coreAnalysisData.TryGetValue(predicatedCopyEntity, out var predicatedCopyEntityValue))
                            {
                                newCopyEntities = newCopyEntities.Union(predicatedCopyEntityValue.AnalysisEntities);
                                newKind = newKind.MergeIfBothKnown(predicatedCopyEntityValue.Kind);
                            }
                            else
                            {
                                newCopyEntities = newCopyEntities.Add(predicatedCopyEntity);
                            }
                        }
                    }

                    // Check if we need to change the current copy value.
                    if (newCopyEntities.Count != currentValue.AnalysisEntities.Count)
                    {
                        var newCopyValue = new CopyAbstractValue(newCopyEntities, newKind);
                        foreach (var copyEntity in newCopyEntities)
                        {
                            coreAnalysisData[copyEntity] = newCopyValue;
                        }
                    }
                    else
                    {
                        Debug.Assert(newCopyEntities.SetEquals(currentValue.AnalysisEntities));
                    }
                }
                else
                {
                    // Predicated copy entity has no entry in the current copy analysis data, so just add the entry.
                    coreAnalysisData[kvp.Key] = kvp.Value;
                }
            }

            // Ensure that applying predicated data to the current copy data can only increase the copy value entities in the current copy data.
            Debug.Assert(predicatedData.All(kvp => kvp.Value.AnalysisEntities.IsSubsetOf(coreAnalysisData[kvp.Key].AnalysisEntities)));
            AssertValidCopyAnalysisData(coreAnalysisData);
        }

        public override void Reset(Func<AnalysisEntity, CopyAbstractValue, CopyAbstractValue> getResetValue)
        {
            this.AssertValidCopyAnalysisData();
            base.Reset(getResetValue);
            this.AssertValidCopyAnalysisData();
        }

        [Conditional("DEBUG")]
        internal void AssertValidCopyAnalysisData(Func<AnalysisEntity, CopyAbstractValue?>? tryGetDefaultCopyValueOpt = null, bool initializingParameters = false)
        {
            AssertValidCopyAnalysisData(CoreAnalysisData, tryGetDefaultCopyValueOpt, initializingParameters);
            AssertValidPredicatedAnalysisData(map => AssertValidCopyAnalysisData(map, tryGetDefaultCopyValueOpt, initializingParameters));
        }

        [Conditional("DEBUG")]
        internal static void AssertValidCopyAnalysisData(
            IDictionary<AnalysisEntity, CopyAbstractValue> map,
            Func<AnalysisEntity, CopyAbstractValue?>? tryGetDefaultCopyValueOpt = null,
            bool initializingParameters = false)
        {
            if (map is CoreCopyAnalysisData coreCopyAnalysisData)
            {
                Debug.Assert(!coreCopyAnalysisData.IsDisposed);
            }

            foreach (var kvp in map)
            {
                AssertValidCopyAnalysisEntity(kvp.Key);
                Debug.Assert(kvp.Value.AnalysisEntities.Contains(kvp.Key));
                foreach (var analysisEntity in kvp.Value.AnalysisEntities)
                {
                    AssertValidCopyAnalysisEntity(analysisEntity);
                    Debug.Assert(map[analysisEntity] == kvp.Value);
                }

                // Validate consistency for all address shared values, if we are not in
                // the middle of initializing parameter input values with address shared entities.
                if (!initializingParameters)
                {
                    var defaultCopyValueOpt = tryGetDefaultCopyValueOpt?.Invoke(kvp.Key);
                    if (defaultCopyValueOpt != null)
                    {
                        foreach (var defaultCopyValyeEntity in defaultCopyValueOpt.AnalysisEntities)
                        {
                            Debug.Assert(kvp.Value.AnalysisEntities.Contains(defaultCopyValyeEntity));
                            Debug.Assert(map.ContainsKey(defaultCopyValyeEntity));
                        }
                    }
                }
            }
        }

        [Conditional("DEBUG")]
        private static void AssertValidCopyAnalysisEntity(AnalysisEntity analysisEntity)
        {
            Debug.Assert(!analysisEntity.HasUnknownInstanceLocation, "Don't track entities if do not know about it's instance location");
        }
    }
}
