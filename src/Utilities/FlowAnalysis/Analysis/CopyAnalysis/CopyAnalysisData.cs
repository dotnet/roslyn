// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    using CoreCopyAnalysisData = IDictionary<AnalysisEntity, CopyAbstractValue>;

    /// <summary>
    /// Aggregated copy analysis data tracked by <see cref="CopyAnalysis"/>.
    /// Contains the <see cref="CoreCopyAnalysisData"/> for entity copy values and
    /// the predicated copy values based on true/false runtime values of predicated entities.
    /// </summary>
    internal sealed class CopyAnalysisData : AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue>
    {
        public CopyAnalysisData()
        {
        }

        public CopyAnalysisData(CoreCopyAnalysisData fromData)
            : base(fromData)
        {
        }

        private CopyAnalysisData(CopyAnalysisData fromData)
            : base(fromData)
        {
        }

        private CopyAnalysisData(CopyAnalysisData data1, CopyAnalysisData data2, MapAbstractDomain<AnalysisEntity, CopyAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
        }

        public override AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> Clone() => new CopyAnalysisData(this);

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> other, MapAbstractDomain<AnalysisEntity, CopyAbstractValue> coreDataAnalysisDomain)
            => BaseCompareHelper(other, coreDataAnalysisDomain);

        public override AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<CopyAbstractValue> data, MapAbstractDomain<AnalysisEntity, CopyAbstractValue> coreDataAnalysisDomain)
        {
            Debug.Assert(IsReachableBlockData || !data.IsReachableBlockData);
            var mergedData = new CopyAnalysisData(this, (CopyAnalysisData)data, coreDataAnalysisDomain);
            mergedData.AssertValidCopyAnalysisData();
            return mergedData;
        }

        public void SetAbstactValue(AnalysisEntity key, CopyAbstractValue value, bool isEntityBeingAssigned)
        {
            // If we have any predicate data, and we are changing the copy value for an assigment,
            // we need to drop all the predicate data based on this entity,
            // i.e. we need to remove all predicated values for this key and
            // also remove data predicated by the true/false value of this key, if any.
            if (HasPredicatedData && isEntityBeingAssigned)
            {
                RemoveEntries(key);
            }

            CoreAnalysisData[key] = value;
        }

        public override void SetAbstractValue(AnalysisEntity key, CopyAbstractValue value)
        {
            throw new NotSupportedException("Use the other overload of SetAbstactValue");
        }

        protected override void RemoveEntryInPredicatedData(AnalysisEntity key, CoreCopyAnalysisData predicatedData)
        {
            Debug.Assert(HasPredicatedData);
            Debug.Assert(predicatedData != null);

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
                    foreach (var predicatedCopyEntity in predicatedValue.AnalysisEntities)
                    {
                        // Predicate copy value has an entity which is not part of the current copy value.
                        // Include this entity and it's copy entities in the new copy value.
                        if (!newCopyEntities.Contains(predicatedCopyEntity))
                        {
                            if (coreAnalysisData.TryGetValue(predicatedCopyEntity, out var predicatedCopyEntityValue))
                            {
                                newCopyEntities = newCopyEntities.Union(predicatedCopyEntityValue.AnalysisEntities);
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
                        var newCopyValue = new CopyAbstractValue(newCopyEntities);
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

        public override void Reset(CopyAbstractValue resetValue)
        {
            throw new NotImplementedException("Use the other overload of Reset");
        }

        public void Reset(Func<AnalysisEntity, CopyAbstractValue> getDefaultCopyValue)
        {
            if (CoreAnalysisData.Count > 0)
            {
                var keys = CoreAnalysisData.Keys.ToImmutableArray();
                foreach (var key in keys)
                {
                    if (CoreAnalysisData[key].AnalysisEntities.Count > 1)
                    {
                        CoreAnalysisData[key] = getDefaultCopyValue(key);
                    }
                }
            }

            ResetPredicatedData();

            this.AssertValidCopyAnalysisData();
        }

        [Conditional("DEBUG")]
        public void AssertValidCopyAnalysisData()
        {
            AssertValidCopyAnalysisData(CoreAnalysisData);
            AssertValidPredicatedAnalysisData(map => AssertValidCopyAnalysisData(map));
        }

        [Conditional("DEBUG")]
        public static void AssertValidCopyAnalysisData(CoreCopyAnalysisData map)
        {
            foreach (var kvp in map)
            {
                AssertValidCopyAnalysisEntity(kvp.Key);
                Debug.Assert(kvp.Value.AnalysisEntities.Contains(kvp.Key));
                foreach (var analysisEntity in kvp.Value.AnalysisEntities)
                {
                    AssertValidCopyAnalysisEntity(analysisEntity);
                    Debug.Assert(map[analysisEntity] == kvp.Value);
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
