// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// An abstract domain implementation for analyses that store dictionary typed data.
    /// </summary>
    public abstract class AnalysisEntityMapAbstractDomain<TValue> : MapAbstractDomain<AnalysisEntity, TValue>
    {
        private static readonly Func<AnalysisEntity, bool> s_defaultIsTrackedEntity = new(_ => true);
        private static readonly Func<PointsToAbstractValue, bool> s_defaultIsTrackedPointsToValue = new(_ => true);

        private readonly Func<AnalysisEntity, bool> _isTrackedEntity;
        private readonly Func<PointsToAbstractValue, bool> _isTrackedPointsToValue;

        private protected AnalysisEntityMapAbstractDomain(
            AbstractValueDomain<TValue> valueDomain,
            Func<AnalysisEntity, bool> isTrackedEntity,
            Func<PointsToAbstractValue, bool> isTrackedPointsToValue)
            : base(valueDomain)
        {
            _isTrackedEntity = isTrackedEntity ?? throw new ArgumentNullException(nameof(isTrackedEntity));
            _isTrackedPointsToValue = isTrackedPointsToValue ?? throw new ArgumentNullException(nameof(isTrackedPointsToValue));
        }

        protected AnalysisEntityMapAbstractDomain(AbstractValueDomain<TValue> valueDomain, PointsToAnalysisResult? pointsToAnalysisResult)
            : this(valueDomain,
                  pointsToAnalysisResult != null ? pointsToAnalysisResult.IsTrackedEntity : s_defaultIsTrackedEntity,
                  pointsToAnalysisResult != null ? pointsToAnalysisResult.IsTrackedPointsToValue : s_defaultIsTrackedPointsToValue)
        {
        }

        protected abstract TValue GetDefaultValue(AnalysisEntity analysisEntity);
        protected abstract bool CanSkipNewEntry(AnalysisEntity analysisEntity, TValue value);
        protected virtual void OnNewMergedValue(TValue value)
        {
        }

        private bool CanSkipNewEntity(AnalysisEntity analysisEntity)
        {
            if (_isTrackedEntity(analysisEntity) ||
                _isTrackedPointsToValue(analysisEntity.InstanceLocation))
            {
                return false;
            }

            if (analysisEntity.Parent != null &&
                !CanSkipNewEntity(analysisEntity.Parent))
            {
                return false;
            }

            return true;
        }

        protected abstract void AssertValidEntryForMergedMap(AnalysisEntity analysisEntity, TValue value);
        protected virtual void AssertValidAnalysisData(DictionaryAnalysisData<AnalysisEntity, TValue> map)
        {
#if DEBUG
            foreach (var kvp in map)
            {
                AssertValidEntryForMergedMap(kvp.Key, kvp.Value);
            }
#endif
        }

#pragma warning disable CA1725 // Parameter names should match base declaration
        public override DictionaryAnalysisData<AnalysisEntity, TValue> Merge(DictionaryAnalysisData<AnalysisEntity, TValue> map1, DictionaryAnalysisData<AnalysisEntity, TValue> map2)
#pragma warning restore CA1725 // Parameter names should match base declaration
        {
            AssertValidAnalysisData(map1);
            AssertValidAnalysisData(map2);

            var resultMap = new DictionaryAnalysisData<AnalysisEntity, TValue>();
            using var _1 = PooledHashSet<AnalysisEntity>.GetInstance(out var newKeys);
            using var _2 = ArrayBuilder<TValue>.GetInstance(5, out var valuesToMergeBuilder);

            var map2LookupIgnoringInstanceLocation = map2.Keys.Where(IsAnalysisEntityForFieldOrProperty)
                                                              .ToLookup(entity => entity.EqualsIgnoringInstanceLocationId);
            foreach (var entry1 in map1)
            {
                AnalysisEntity key1 = entry1.Key;
                TValue value1 = entry1.Value;

                if (map2LookupIgnoringInstanceLocation.Count > 0 && IsAnalysisEntityForFieldOrProperty(key1))
                {
                    var equivalentKeys2 = map2LookupIgnoringInstanceLocation[key1.EqualsIgnoringInstanceLocationId];
                    if (!equivalentKeys2.Any())
                    {
                        TValue mergedValue = GetMergedValueForEntityPresentInOneMap(key1, value1);
                        Debug.Assert(!map2.ContainsKey(key1));
                        Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                        AddNewEntryToResultMap(key1, mergedValue);
                        continue;
                    }

                    foreach (AnalysisEntity key2 in equivalentKeys2)
                    {
                        // Confirm that key2 and key1 are indeed EqualsIgnoringInstanceLocation
                        // This ensures that we handle hash code clashes of EqualsIgnoringInstanceLocationId.
                        if (!key1.EqualsIgnoringInstanceLocation(key2))
                        {
                            continue;
                        }

                        TValue value2 = map2[key2];

                        valuesToMergeBuilder.Clear();
                        valuesToMergeBuilder.Add(value1);
                        valuesToMergeBuilder.Add(value2);

                        if (key1.InstanceLocation.Equals(key2.InstanceLocation))
                        {
                            var mergedValue = GetMergedValue(valuesToMergeBuilder);
                            AddNewEntryToResultMap(key1, mergedValue);
                        }
                        else
                        {
                            if (key1.Symbol == null || !SymbolEqualityComparer.Default.Equals(key1.Symbol, key2.Symbol))
                            {
                                // PERF: Do not add a new key-value pair to the resultMap for unrelated entities or non-symbol based entities.
                                continue;
                            }

                            AnalysisEntity mergedKey = key1.WithMergedInstanceLocation(key2);

                            var isExistingKeyInInput = false;
                            var isExistingKeyInResult = false;
                            if (resultMap.TryGetValue(mergedKey, out var existingValue))
                            {
                                valuesToMergeBuilder.Add(existingValue);
                                isExistingKeyInResult = true;
                            }

                            if (map1.TryGetValue(mergedKey, out existingValue))
                            {
                                valuesToMergeBuilder.Add(existingValue);
                                isExistingKeyInInput = true;
                            }

                            if (map2.TryGetValue(mergedKey, out existingValue))
                            {
                                valuesToMergeBuilder.Add(existingValue);
                                isExistingKeyInInput = true;
                            }

                            var isCandidateToBeSkipped = !isExistingKeyInInput && !isExistingKeyInResult;
                            if (isCandidateToBeSkipped && CanSkipNewEntity(mergedKey))
                            {
                                // PERF: Do not add a new key-value pair to the resultMap if the key is not reachable from tracked entities and PointsTo values.
                                continue;
                            }

                            var mergedValue = GetMergedValue(valuesToMergeBuilder);

                            Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                            Debug.Assert(ValueDomain.Compare(value2, mergedValue) <= 0);

                            if (isCandidateToBeSkipped && CanSkipNewEntry(mergedKey, mergedValue))
                            {
                                // PERF: Do not add a new key-value pair to the resultMap if the value can be skipped.
                                continue;
                            }

                            if (!isExistingKeyInInput)
                            {
                                newKeys.Add(mergedKey);
                            }

                            AddNewEntryToResultMap(mergedKey, mergedValue, isNewKey: !isExistingKeyInInput);
                        }
                    }
                }
                else if (map2.TryGetValue(key1, out var value2))
                {
                    TValue mergedValue = ValueDomain.Merge(value1, value2);
                    Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                    Debug.Assert(ValueDomain.Compare(value2, mergedValue) <= 0);
                    AddNewEntryToResultMap(key1, mergedValue);
                    continue;
                }

                if (!resultMap.ContainsKey(key1))
                {
                    TValue mergedValue = GetMergedValueForEntityPresentInOneMap(key1, value1);
                    Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                    AddNewEntryToResultMap(key1, mergedValue);
                }
            }

            foreach (var kvp in map2)
            {
                var key2 = kvp.Key;
                var value2 = kvp.Value;
                if (!resultMap.ContainsKey(key2))
                {
                    TValue mergedValue = GetMergedValueForEntityPresentInOneMap(key2, value2);
                    Debug.Assert(ValueDomain.Compare(value2, mergedValue) <= 0);
                    AddNewEntryToResultMap(key2, mergedValue);
                }
            }

            foreach (var newKey in newKeys)
            {
                Debug.Assert(!map1.ContainsKey(newKey));
                Debug.Assert(!map2.ContainsKey(newKey));
                var value = resultMap[newKey];
                if (ReferenceEquals(value, GetDefaultValue(newKey)))
                {
                    resultMap.Remove(newKey);
                }
                else
                {
                    OnNewMergedValue(value);
                }
            }

            Debug.Assert(Compare(map1, resultMap) <= 0);
            Debug.Assert(Compare(map2, resultMap) <= 0);
            AssertValidAnalysisData(resultMap);

            return resultMap;
            static bool IsAnalysisEntityForFieldOrProperty(AnalysisEntity entity)
                => entity.Symbol?.Kind is SymbolKind.Field or SymbolKind.Property;

            TValue GetMergedValueForEntityPresentInOneMap(AnalysisEntity key, TValue value)
            {
                if (key.HasConstantValue)
                {
                    return value;
                }

                var defaultValue = GetDefaultValue(key);
                return ValueDomain.Merge(value, defaultValue);
            }

            TValue GetMergedValue(ArrayBuilder<TValue> values)
            {
                Debug.Assert(values.Count > 0);
                var mergedValue = values[0];
                for (var i = 1; i < values.Count; i++)
                {
                    mergedValue = GetMergedValueCore(mergedValue, values[i]);
                }

                return mergedValue;

                TValue GetMergedValueCore(TValue value1, TValue value2)
                {
                    TValue mergedValue = ValueDomain.Merge(value1, value2);
                    Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                    Debug.Assert(ValueDomain.Compare(value2, mergedValue) <= 0);
                    return mergedValue;
                }
            }

            void AddNewEntryToResultMap(AnalysisEntity key, TValue value, bool isNewKey = false)
            {
                Debug.Assert(isNewKey == (!map1.ContainsKey(key) && !map2.ContainsKey(key)));
                AssertValidEntryForMergedMap(key, value);
                resultMap[key] = value;
                if (!isNewKey)
                {
                    OnNewMergedValue(value);
                }
            }
        }
    }
}
