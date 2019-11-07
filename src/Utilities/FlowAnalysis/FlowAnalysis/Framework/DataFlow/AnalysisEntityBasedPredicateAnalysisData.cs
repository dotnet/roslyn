// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Base class for all the aggregate analysis data with predicated analysis data,
    /// whose <see cref="CoreAnalysisData"/> is keyed by an <see cref="AnalysisEntity"/>.
    /// </summary>
    public abstract class AnalysisEntityBasedPredicateAnalysisData<TValue> : PredicatedAnalysisData<AnalysisEntity, TValue>
    {
        protected AnalysisEntityBasedPredicateAnalysisData()
        {
            CoreAnalysisData = new DictionaryAnalysisData<AnalysisEntity, TValue>();
        }

        protected AnalysisEntityBasedPredicateAnalysisData(IDictionary<AnalysisEntity, TValue> fromData)
        {
            CoreAnalysisData = new DictionaryAnalysisData<AnalysisEntity, TValue>(fromData);
        }

        protected AnalysisEntityBasedPredicateAnalysisData(AnalysisEntityBasedPredicateAnalysisData<TValue> fromData)
            : base(fromData)
        {
            CoreAnalysisData = new DictionaryAnalysisData<AnalysisEntity, TValue>(fromData.CoreAnalysisData);
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
            DictionaryAnalysisData<AnalysisEntity, TValue> mergedCoreAnalysisData,
            PredicatedAnalysisData<AnalysisEntity, TValue> predicatedData1,
            PredicatedAnalysisData<AnalysisEntity, TValue> predicatedData2,
            bool isReachableData,
            MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain)
            : base(predicatedData1, predicatedData2, mergedCoreAnalysisData,
                  mergedCoreAnalysisData, isReachableData, coreDataAnalysisDomain)
        {
            CoreAnalysisData = mergedCoreAnalysisData;
        }

        public DictionaryAnalysisData<AnalysisEntity, TValue> CoreAnalysisData { get; }

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

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        public TValue this[AnalysisEntity key] => CoreAnalysisData[key];
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers

        [Conditional("DEBUG")]
        private void AssertValidAnalysisData()
        {
            Debug.Assert(!CoreAnalysisData.IsDisposed);
            AssertValidPredicatedAnalysisData(map => Debug.Assert(!map.IsDisposed));
        }

        public virtual void SetAbstractValue(AnalysisEntity key, TValue value)
        {
            AssertValidAnalysisData();
            if (HasPredicatedData)
            {
                RemoveEntriesInPredicatedData(key);
            }

            CoreAnalysisData[key] = value;
        }

        public void RemoveEntries(AnalysisEntity key)
        {
            AssertValidAnalysisData();

            CoreAnalysisData.Remove(key);
            if (HasPredicatedData)
            {
                RemoveEntriesInPredicatedData(key);
            }
        }

        public bool Equals(AnalysisEntityBasedPredicateAnalysisData<TValue> other)
        {
            AssertValidAnalysisData();

            return base.Equals(other) &&
                EqualsHelper(CoreAnalysisData, other.CoreAnalysisData);
        }

        public virtual void Reset(Func<AnalysisEntity, TValue, TValue> getResetValue)
        {
            AssertValidAnalysisData();

            // Reset the current analysis data, while ensuring that we don't violate the monotonicity, i.e. we cannot remove any existing key from currentAnalysisData.
            // Just set the values for existing keys to ValueDomain.UnknownOrMayBeValue.
            if (CoreAnalysisData.Count > 0)
            {
                var keys = CoreAnalysisData.Keys.ToImmutableArray();
                foreach (var key in keys)
                {
                    CoreAnalysisData[key] = getResetValue(key, CoreAnalysisData[key]);
                }
            }

            ResetPredicatedData();
            AssertValidAnalysisData();
        }

        public void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, AnalysisEntityBasedPredicateAnalysisData<TValue>? truePredicateData, AnalysisEntityBasedPredicateAnalysisData<TValue>? falsePredicateData)
        {
            AssertValidAnalysisData();

            StartTrackingPredicatedData(predicatedEntity, truePredicateData?.CoreAnalysisData, falsePredicateData?.CoreAnalysisData);
            AssertValidAnalysisData();
        }

        public PredicateValueKind ApplyPredicatedDataForEntity(AnalysisEntity predicatedEntity, bool trueData)
        {
            AssertValidAnalysisData();

            var result = ApplyPredicatedDataForEntity(CoreAnalysisData, predicatedEntity, trueData);
            AssertValidAnalysisData();
            return result;
        }

        public void AddTrackedEntities(HashSet<AnalysisEntity> builder) => builder.UnionWith(CoreAnalysisData.Keys);

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                CoreAnalysisData.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
