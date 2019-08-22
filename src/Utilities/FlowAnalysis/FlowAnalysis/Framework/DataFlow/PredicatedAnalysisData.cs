// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
#pragma warning disable CA2000 // Dispose objects before losing scope - https://github.com/dotnet/roslyn-analyzers/issues/2715

    /// <summary>
    /// Base class for all the predicated analysis data.
    /// It tracks <see cref="_lazyPredicateDataMap"/>, which contains the true/false <see cref="PerEntityPredicatedAnalysisData"/> for every predicated <see cref="AnalysisEntity"/>, and
    /// <see cref="IsReachableBlockData"/>, which tracks if the current data is for a reachable code path based on the predicate analysis.
    /// Predicate analysis data is used to improve the preciseness of analysis when we can apply the <see cref="PerEntityPredicatedAnalysisData.TruePredicatedData"/> or <see cref="PerEntityPredicatedAnalysisData.FalsePredicatedData"/>
    /// on the control flow paths where the corresonding <see cref="AnalysisEntity"/> is known to have <code>true</code> or <code>false</code> value respectively.
    /// </summary>
    public abstract partial class PredicatedAnalysisData<TKey, TValue> : AbstractAnalysisData
    {
#pragma warning disable CA2213 // Disposable fields should be disposed - https://github.com/dotnet/roslyn-analyzers/issues/2182
        private DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> _lazyPredicateDataMap;
#pragma warning restore CA2213 // Disposable fields should be disposed

        protected PredicatedAnalysisData()
        {
            IsReachableBlockData = true;
        }

        protected PredicatedAnalysisData(PredicatedAnalysisData<TKey, TValue> fromData)
        {
            IsReachableBlockData = fromData.IsReachableBlockData;
            _lazyPredicateDataMap = Clone(fromData._lazyPredicateDataMap);
            AssertValidAnalysisData();
        }

        protected PredicatedAnalysisData(
           PredicatedAnalysisData<TKey, TValue> predicatedData1,
           PredicatedAnalysisData<TKey, TValue> predicatedData2,
           DictionaryAnalysisData<TKey, TValue> coreAnalysisData1,
           DictionaryAnalysisData<TKey, TValue> coreAnalysisData2,
           bool isReachableData,
           MapAbstractDomain<TKey, TValue> coreDataAnalysisDomain)
        {
            IsReachableBlockData = isReachableData;
            _lazyPredicateDataMap = Merge(predicatedData1._lazyPredicateDataMap, predicatedData2._lazyPredicateDataMap,
                coreAnalysisData1, coreAnalysisData2, coreDataAnalysisDomain, ApplyPredicatedData);
            AssertValidAnalysisData();
        }

        public bool IsReachableBlockData { get; set; }

        public bool HasPredicatedData => _lazyPredicateDataMap != null;

        [Conditional("DEBUG")]
        private void AssertValidAnalysisData()
        {
            if (_lazyPredicateDataMap != null)
            {
                Debug.Assert(!_lazyPredicateDataMap.IsDisposed);
                var builder = PooledHashSet<DictionaryAnalysisData<TKey, TValue>>.GetInstance();
                foreach (var value in _lazyPredicateDataMap.Values)
                {
                    if (value.TruePredicatedData != null)
                    {
                        Debug.Assert(!value.TruePredicatedData.IsDisposed);
                        Debug.Assert(builder.Add(value.TruePredicatedData));
                    }

                    if (value.FalsePredicatedData != null)
                    {
                        Debug.Assert(!value.FalsePredicatedData.IsDisposed);
                        Debug.Assert(builder.Add(value.FalsePredicatedData));
                    }
                }

                builder.Free();
            }
        }

        private void EnsurePredicatedData()
        {
            _lazyPredicateDataMap ??= new DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData>();
        }

        protected void StartTrackingPredicatedData(AnalysisEntity predicatedEntity, DictionaryAnalysisData<TKey, TValue> truePredicatedData, DictionaryAnalysisData<TKey, TValue> falsePredicatedData)
        {
            Debug.Assert(predicatedEntity.IsCandidatePredicateEntity());
            Debug.Assert(predicatedEntity.CaptureIdOpt != null, "Currently we only support predicated data tracking for flow captures");

            AssertValidAnalysisData();

            EnsurePredicatedData();
            _lazyPredicateDataMap[predicatedEntity] = new PerEntityPredicatedAnalysisData(truePredicatedData, falsePredicatedData);

            AssertValidAnalysisData();
        }

        public void StopTrackingPredicatedData(AnalysisEntity predicatedEntity)
        {
            Debug.Assert(HasPredicatedDataForEntity(predicatedEntity));
            Debug.Assert(predicatedEntity.CaptureIdOpt != null, "Currently we only support predicated data tracking for flow captures");
            AssertValidAnalysisData();

            if (_lazyPredicateDataMap.TryGetValue(predicatedEntity, out var perEntityPredicatedAnalysisData))
            {
                perEntityPredicatedAnalysisData.Dispose();
            }

            _lazyPredicateDataMap.Remove(predicatedEntity);
            if (_lazyPredicateDataMap.Count == 0)
            {
                ResetPredicatedData();
            }

            AssertValidAnalysisData();
        }

        public bool HasPredicatedDataForEntity(AnalysisEntity predicatedEntity)
            => HasPredicatedData && _lazyPredicateDataMap.ContainsKey(predicatedEntity);

        public void TransferPredicatedData(AnalysisEntity fromEntity, AnalysisEntity toEntity)
        {
            Debug.Assert(HasPredicatedDataForEntity(fromEntity));
            Debug.Assert(fromEntity.CaptureIdOpt != null, "Currently we only support predicated data tracking for flow captures");
            Debug.Assert(toEntity.CaptureIdOpt != null, "Currently we only support predicated data tracking for flow captures");
            AssertValidAnalysisData();

            if (_lazyPredicateDataMap.TryGetValue(fromEntity, out var fromEntityPredicatedData))
            {
                _lazyPredicateDataMap[toEntity] = new PerEntityPredicatedAnalysisData(fromEntityPredicatedData);
            }

            AssertValidAnalysisData();
        }

        protected PredicateValueKind ApplyPredicatedDataForEntity(DictionaryAnalysisData<TKey, TValue> coreAnalysisData, AnalysisEntity predicatedEntity, bool trueData)
        {
            Debug.Assert(HasPredicatedDataForEntity(predicatedEntity));
            AssertValidAnalysisData();

            var perEntityPredicateData = _lazyPredicateDataMap[predicatedEntity];
            var predicatedDataToApply = trueData ? perEntityPredicateData.TruePredicatedData : perEntityPredicateData.FalsePredicatedData;
            if (predicatedDataToApply == null)
            {
                // Infeasible branch.
                return PredicateValueKind.AlwaysFalse;
            }

            ApplyPredicatedData(coreAnalysisData, predicatedDataToApply);

            // Predicate is always true if other branch predicate data is null.
            var otherBranchPredicatedData = trueData ? perEntityPredicateData.FalsePredicatedData : perEntityPredicateData.TruePredicatedData;
            return otherBranchPredicatedData == null ?
                PredicateValueKind.AlwaysTrue :
                PredicateValueKind.Unknown;
        }

        protected virtual void ApplyPredicatedData(DictionaryAnalysisData<TKey, TValue> coreAnalysisData, DictionaryAnalysisData<TKey, TValue> predicatedData)
        {
            Debug.Assert(coreAnalysisData != null);
            Debug.Assert(predicatedData != null);

            foreach (var kvp in predicatedData)
            {
                coreAnalysisData[kvp.Key] = kvp.Value;
            }
        }

        protected void RemoveEntriesInPredicatedData(TKey key)
        {
            Debug.Assert(HasPredicatedData);
            AssertValidAnalysisData();

            foreach (var kvp in _lazyPredicateDataMap)
            {
                if (kvp.Value.TruePredicatedData != null)
                {
                    RemoveEntryInPredicatedData(key, kvp.Value.TruePredicatedData);
                }

                if (kvp.Value.FalsePredicatedData != null)
                {
                    RemoveEntryInPredicatedData(key, kvp.Value.FalsePredicatedData);
                }
            }

            AssertValidAnalysisData();
        }

        protected virtual void RemoveEntryInPredicatedData(TKey key, DictionaryAnalysisData<TKey, TValue> predicatedData)
        {
            predicatedData.Remove(key);
        }

        private static DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> Clone(DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> fromData)
        {
            if (fromData == null)
            {
                return null;
            }

            var clonedMap = new DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData>();
            foreach (var kvp in fromData)
            {
                clonedMap.Add(kvp.Key, new PerEntityPredicatedAnalysisData(kvp.Value));
            }

            return clonedMap;
        }

        private static DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> Merge(
            DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> predicatedData1,
            DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> predicatedData2,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData1,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData2,
            MapAbstractDomain<TKey, TValue> coreDataAnalysisDomain,
            Action<DictionaryAnalysisData<TKey, TValue>, DictionaryAnalysisData<TKey, TValue>> applyPredicatedData)
        {
            Debug.Assert(coreAnalysisData1 != null);
            Debug.Assert(coreAnalysisData2 != null);

            if (predicatedData1 == null)
            {
                if (predicatedData2 == null)
                {
                    return null;
                }

                return MergeForPredicatedDataInOneBranch(predicatedData2, coreAnalysisData1, coreDataAnalysisDomain);
            }
            else if (predicatedData2 == null)
            {
                return MergeForPredicatedDataInOneBranch(predicatedData1, coreAnalysisData2, coreDataAnalysisDomain);
            }

            return MergeForPredicatedDataInBothBranches(predicatedData1, predicatedData2,
                coreAnalysisData1, coreAnalysisData2, coreDataAnalysisDomain, applyPredicatedData);
        }

        private static DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> MergeForPredicatedDataInOneBranch(
            DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> predicatedData,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisDataForOtherBranch,
            MapAbstractDomain<TKey, TValue> coreDataAnalysisDomain)
        {
            Debug.Assert(predicatedData != null);
            Debug.Assert(coreAnalysisDataForOtherBranch != null);

            var result = new DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData>();
            foreach (var kvp in predicatedData)
            {
                var resultTruePredicatedData = MergeForPredicatedDataInOneBranch(kvp.Value.TruePredicatedData, coreAnalysisDataForOtherBranch, coreDataAnalysisDomain);
                var resultFalsePredicatedData = MergeForPredicatedDataInOneBranch(kvp.Value.FalsePredicatedData, coreAnalysisDataForOtherBranch, coreDataAnalysisDomain);
                var perEntityPredicatedData = new PerEntityPredicatedAnalysisData(resultTruePredicatedData, resultFalsePredicatedData);
                result.Add(kvp.Key, perEntityPredicatedData);
            }

            return result;
        }

        private static DictionaryAnalysisData<TKey, TValue> MergeForPredicatedDataInOneBranch(
            DictionaryAnalysisData<TKey, TValue> predicatedData,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisDataForOtherBranch,
            MapAbstractDomain<TKey, TValue> coreDataAnalysisDomain)
        {
            if (predicatedData == null)
            {
                return null;
            }

            return coreDataAnalysisDomain.Merge(predicatedData, coreAnalysisDataForOtherBranch);
        }

        private static DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> MergeForPredicatedDataInBothBranches(
            DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> predicatedData1,
            DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData> predicatedData2,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData1,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData2,
            MapAbstractDomain<TKey, TValue> coreDataAnalysisDomain,
            Action<DictionaryAnalysisData<TKey, TValue>, DictionaryAnalysisData<TKey, TValue>> applyPredicatedData)
        {
            Debug.Assert(predicatedData1 != null);
            Debug.Assert(predicatedData2 != null);
            Debug.Assert(coreAnalysisData1 != null);
            Debug.Assert(coreAnalysisData2 != null);

            var result = new DictionaryAnalysisData<AnalysisEntity, PerEntityPredicatedAnalysisData>();
            foreach (var kvp in predicatedData1)
            {
                DictionaryAnalysisData<TKey, TValue> resultTruePredicatedData;
                DictionaryAnalysisData<TKey, TValue> resultFalsePredicatedData;
                if (!predicatedData2.TryGetValue(kvp.Key, out var value2))
                {
                    // Data predicated by the analysis entity present in only one branch.
                    // We should merge with the core non-predicate data in other branch.
                    resultTruePredicatedData = MergeForPredicatedDataInOneBranch(kvp.Value.TruePredicatedData, coreAnalysisData2, coreDataAnalysisDomain);
                    resultFalsePredicatedData = MergeForPredicatedDataInOneBranch(kvp.Value.FalsePredicatedData, coreAnalysisData2, coreDataAnalysisDomain);
                }
                else
                {
                    // Data predicated by the analysis entity present in both branches.
                    resultTruePredicatedData = Merge(kvp.Value.TruePredicatedData, value2.TruePredicatedData,
                        coreAnalysisData1, coreAnalysisData2, coreDataAnalysisDomain, applyPredicatedData);
                    resultFalsePredicatedData = Merge(kvp.Value.FalsePredicatedData, value2.FalsePredicatedData,
                        coreAnalysisData1, coreAnalysisData2, coreDataAnalysisDomain, applyPredicatedData);
                }

                var perEntityPredicatedData = new PerEntityPredicatedAnalysisData(resultTruePredicatedData, resultFalsePredicatedData);
                result.Add(kvp.Key, perEntityPredicatedData);
            }

            foreach (var kvp in predicatedData2)
            {
                if (!predicatedData1.TryGetValue(kvp.Key, out var value2))
                {
                    // Data predicated by the analysis entity present in only one branch.
                    // We should merge with the core non-predicate data in other branch.
                    var resultTruePredicatedData = MergeForPredicatedDataInOneBranch(kvp.Value.TruePredicatedData, coreAnalysisData1, coreDataAnalysisDomain);
                    var resultFalsePredicatedData = MergeForPredicatedDataInOneBranch(kvp.Value.FalsePredicatedData, coreAnalysisData1, coreDataAnalysisDomain);
                    var perEntityPredicatedData = new PerEntityPredicatedAnalysisData(resultTruePredicatedData, resultFalsePredicatedData);
                    result.Add(kvp.Key, perEntityPredicatedData);
                }
            }

            return result;
        }

        private static DictionaryAnalysisData<TKey, TValue> Merge(
            DictionaryAnalysisData<TKey, TValue> predicateTrueOrFalseData1,
            DictionaryAnalysisData<TKey, TValue> predicateTrueOrFalseData2,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData1,
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData2,
            MapAbstractDomain<TKey, TValue> coreDataAnalysisDomain,
            Action<DictionaryAnalysisData<TKey, TValue>, DictionaryAnalysisData<TKey, TValue>> applyPredicatedData)
        {
            if (predicateTrueOrFalseData1 == null)
            {
                return predicateTrueOrFalseData2 != null ?
                    CloneAndApplyPredicatedData(coreAnalysisData2, predicateTrueOrFalseData2, applyPredicatedData) :
                    null;
            }
            else if (predicateTrueOrFalseData2 == null)
            {
                return CloneAndApplyPredicatedData(coreAnalysisData1, predicateTrueOrFalseData1, applyPredicatedData);
            }

            var appliedPredicatedData1 = CloneAndApplyPredicatedData(coreAnalysisData1, predicateTrueOrFalseData1, applyPredicatedData);
            var appliedPredicatedData2 = CloneAndApplyPredicatedData(coreAnalysisData2, predicateTrueOrFalseData2, applyPredicatedData);

            return coreDataAnalysisDomain.Merge(appliedPredicatedData1, appliedPredicatedData2);
        }

        private static DictionaryAnalysisData<TKey, TValue> CloneAndApplyPredicatedData(
            DictionaryAnalysisData<TKey, TValue> coreAnalysisData,
            DictionaryAnalysisData<TKey, TValue> predicateTrueOrFalseData,
            Action<DictionaryAnalysisData<TKey, TValue>, DictionaryAnalysisData<TKey, TValue>> applyPredicatedData)
        {
            Debug.Assert(predicateTrueOrFalseData != null);
            Debug.Assert(coreAnalysisData != null);

            var result = new DictionaryAnalysisData<TKey, TValue>(coreAnalysisData);
            applyPredicatedData(result, predicateTrueOrFalseData);
            return result;
        }

        protected int BaseCompareHelper(PredicatedAnalysisData<TKey, TValue> newData)
        {
            Debug.Assert(newData != null);

            if (!IsReachableBlockData && newData.IsReachableBlockData)
            {
                return -1;
            }

            if (_lazyPredicateDataMap == null)
            {
                return newData._lazyPredicateDataMap == null ? 0 : -1;
            }
            else if (newData._lazyPredicateDataMap == null)
            {
                return 1;
            }

            if (ReferenceEquals(this, newData))
            {
                return 0;
            }

            // Note that predicate maps can add or remove entries based on core analysis data entries.
            // We can only determine if the predicate data is equal or not.
            return Equals(newData) ? 0 : -1;
        }

        protected bool Equals(PredicatedAnalysisData<TKey, TValue> other)
        {
            if (_lazyPredicateDataMap == null)
            {
                return other._lazyPredicateDataMap == null;
            }
            else if (other._lazyPredicateDataMap == null ||
                _lazyPredicateDataMap.Count != other._lazyPredicateDataMap.Count)
            {
                return false;
            }
            else
            {
                foreach (var kvp in _lazyPredicateDataMap)
                {
                    if (!other._lazyPredicateDataMap.TryGetValue(kvp.Key, out var otherValue) ||
                        !EqualsHelper(kvp.Value.TruePredicatedData, otherValue.TruePredicatedData) ||
                        !EqualsHelper(kvp.Value.FalsePredicatedData, otherValue.FalsePredicatedData))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected static bool EqualsHelper(DictionaryAnalysisData<TKey, TValue> dict1, DictionaryAnalysisData<TKey, TValue> dict2)
        {
            if (dict1 == null)
            {
                return dict2 == null;
            }
            else if (dict2 == null || dict1.Count != dict2.Count)
            {
                return false;
            }

            return dict1.Keys.All(key => dict2.TryGetValue(key, out TValue value2) &&
                                         EqualityComparer<TValue>.Default.Equals(dict1[key], value2));
        }

        protected void ResetPredicatedData()
        {
            if (_lazyPredicateDataMap == null)
            {
                return;
            }

            if (!_lazyPredicateDataMap.IsDisposed)
            {
                _lazyPredicateDataMap.Values.Dispose();
                _lazyPredicateDataMap.Dispose();
            }

            Debug.Assert(_lazyPredicateDataMap.IsDisposed);
            _lazyPredicateDataMap = null;
        }

        [Conditional("DEBUG")]
        protected void AssertValidPredicatedAnalysisData(Action<DictionaryAnalysisData<TKey, TValue>> assertValidAnalysisData)
        {
            if (HasPredicatedData)
            {
                AssertValidAnalysisData();

                foreach (var kvp in _lazyPredicateDataMap)
                {
                    if (kvp.Value.TruePredicatedData != null)
                    {
                        assertValidAnalysisData(kvp.Value.TruePredicatedData);
                    }

                    if (kvp.Value.FalsePredicatedData != null)
                    {
                        assertValidAnalysisData(kvp.Value.FalsePredicatedData);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                ResetPredicatedData();
            }

            base.Dispose(disposing);
        }
    }
}
