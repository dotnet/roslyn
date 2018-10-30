// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Base class for all the aggregate analysis data with predicated analysis data,
    /// whose <see cref="CoreAnalysisData"/> is keyed by an <see cref="AnalysisEntity"/>.
    /// </summary>
    internal abstract class AnalysisEntityBasedPredicateAnalysisData<TValue> : PredicatedAnalysisData<AnalysisEntity, TValue>
    {
        protected AnalysisEntityBasedPredicateAnalysisData()
        {
            CoreAnalysisData = new Dictionary<AnalysisEntity, TValue>();
        }

        protected AnalysisEntityBasedPredicateAnalysisData(IDictionary<AnalysisEntity, TValue> fromData)
        {
            CoreAnalysisData = new Dictionary<AnalysisEntity, TValue>(fromData);
        }

        protected AnalysisEntityBasedPredicateAnalysisData(AnalysisEntityBasedPredicateAnalysisData<TValue> fromData)
            : base(fromData)
        {
            CoreAnalysisData = new Dictionary<AnalysisEntity, TValue>(fromData.CoreAnalysisData);
        }

        protected AnalysisEntityBasedPredicateAnalysisData(
            AnalysisEntityBasedPredicateAnalysisData<TValue> data1,
            AnalysisEntityBasedPredicateAnalysisData<TValue> data2,
            MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain)
            : base(data1, data2, data1.CoreAnalysisData,
                  data2.CoreAnalysisData, data1.IsReachableBlockData, coreDataAnalysisDomain)
        {
            Debug.Assert(data1.IsReachableBlockData == data1.IsReachableBlockData);

            CoreAnalysisData = coreDataAnalysisDomain.Merge(data1.CoreAnalysisData, data2.CoreAnalysisData);
        }

        protected AnalysisEntityBasedPredicateAnalysisData(
            IDictionary<AnalysisEntity, TValue> mergedCoreAnalysisData,
            PredicatedAnalysisData<AnalysisEntity, TValue> predicatedData1,
            PredicatedAnalysisData<AnalysisEntity, TValue> predicatedData2,
            bool isReachableData,
            MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain)
            : base(predicatedData1, predicatedData2, mergedCoreAnalysisData,
                  mergedCoreAnalysisData, isReachableData, coreDataAnalysisDomain)
        {
            CoreAnalysisData = mergedCoreAnalysisData;
        }

        public IDictionary<AnalysisEntity, TValue> CoreAnalysisData { get; }

        public virtual bool HasAnyAbstractValue => CoreAnalysisData.Count > 0 || HasPredicatedData;

        public abstract AnalysisEntityBasedPredicateAnalysisData<TValue> Clone();
        public abstract AnalysisEntityBasedPredicateAnalysisData<TValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<TValue> data, MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain);
        public abstract int Compare(AnalysisEntityBasedPredicateAnalysisData<TValue> other, MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain);

        protected int BaseCompareHelper(AnalysisEntityBasedPredicateAnalysisData<TValue> newData, MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain)
        {
            var baseCompareResult = base.BaseCompareHelper(newData);
            if (baseCompareResult != 0)
            {
                Debug.Assert(baseCompareResult < 0, "Non-monotonic Merge function");
                return baseCompareResult;
            }

            var coreAnalysisDataCompareResult = coreDataAnalysisDomain.Compare(CoreAnalysisData, newData.CoreAnalysisData);
            Debug.Assert(coreAnalysisDataCompareResult <= 0, "Non-monotonic Merge function");
            return coreAnalysisDataCompareResult;
        }

        public bool HasAbstractValue(AnalysisEntity analysisEntity) => CoreAnalysisData.ContainsKey(analysisEntity);

        public bool TryGetValue(AnalysisEntity key, out TValue value) => CoreAnalysisData.TryGetValue(key, out value);

        public TValue this[AnalysisEntity key] => CoreAnalysisData[key];

        public virtual void SetAbstractValue(AnalysisEntity key, TValue value)
        {
            if (HasPredicatedData)
            {
                RemoveEntriesInPredicatedData(key);
            }

            CoreAnalysisData[key] = value;
        }

        public void RemoveEntries(AnalysisEntity key)
        {
            CoreAnalysisData.Remove(key);
            if (HasPredicatedData)
            {
                RemoveEntriesInPredicatedData(key);
            }
        }

        public bool Equals(AnalysisEntityBasedPredicateAnalysisData<TValue> other)
        {
            return base.Equals(other) &&
                EqualsHelper(CoreAnalysisData, other.CoreAnalysisData);
        }

        public virtual void Reset(TValue resetValue)
        {
            // Reset the current analysis data, while ensuring that we don't violate the monotonicity, i.e. we cannot remove any existing key from currentAnalysisData.
            // Just set the values for existing keys to ValueDomain.UnknownOrMayBeValue.
            if (CoreAnalysisData.Count > 0)
            {
                var keys = CoreAnalysisData.Keys.ToImmutableArray();
                foreach (var key in keys)
                {
                    CoreAnalysisData[key] = resetValue;
                }
            }

            ResetPredicatedData();
        }

        public void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, AnalysisEntityBasedPredicateAnalysisData<TValue> truePredicateData, AnalysisEntityBasedPredicateAnalysisData<TValue> falsePredicateData)
            => StartTrackingPredicatedData(predicatedEntity, truePredicateData?.CoreAnalysisData, falsePredicateData?.CoreAnalysisData);

        public PredicateValueKind ApplyPredicatedDataForEntity(AnalysisEntity predicatedEntity, bool trueData)
            => ApplyPredicatedDataForEntity(CoreAnalysisData, predicatedEntity, trueData);

        public void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder) => builder.AddRange(CoreAnalysisData.Keys);
    }
}
