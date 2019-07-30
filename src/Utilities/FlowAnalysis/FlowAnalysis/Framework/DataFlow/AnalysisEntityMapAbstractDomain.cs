// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// An abstract domain implementation for analyses that store dictionary typed data.
    /// </summary>
    public abstract class AnalysisEntityMapAbstractDomain<TValue> : MapAbstractDomain<AnalysisEntity, TValue>
    {
        protected AnalysisEntityMapAbstractDomain(AbstractValueDomain<TValue> valueDomain)
            : base(valueDomain)
        {
        }

        protected abstract TValue GetDefaultValue(AnalysisEntity analysisEntity);
        protected abstract bool CanSkipNewEntry(AnalysisEntity analysisEntity, TValue value);

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

        public override DictionaryAnalysisData<AnalysisEntity, TValue> Merge(DictionaryAnalysisData<AnalysisEntity, TValue> map1, DictionaryAnalysisData<AnalysisEntity, TValue> map2)
        {
            Debug.Assert(map1 != null);
            AssertValidAnalysisData(map1);
            Debug.Assert(map2 != null);
            AssertValidAnalysisData(map2);

            TValue GetMergedValueForEntityPresentInOneMap(AnalysisEntity key, TValue value)
            {
                if (key.HasConstantValue)
                {
                    return value;
                }

                var defaultValue = GetDefaultValue(key);
                return ValueDomain.Merge(value, defaultValue);
            }

            var resultMap = new DictionaryAnalysisData<AnalysisEntity, TValue>();
            var newKeys = PooledHashSet<AnalysisEntity>.GetInstance();

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
                        AssertValidEntryForMergedMap(key1, mergedValue);
                        resultMap.Add(key1, mergedValue);
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
                        TValue mergedValue = ValueDomain.Merge(value1, value2);
                        Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                        Debug.Assert(ValueDomain.Compare(value2, mergedValue) <= 0);

                        if (key1.InstanceLocation.Equals(key2.InstanceLocation))
                        {
                            AssertValidEntryForMergedMap(key1, mergedValue);
                            resultMap[key1] = mergedValue;
                        }
                        else
                        {
                            if (key1.SymbolOpt == null || !Equals(key1.SymbolOpt, key2.SymbolOpt))
                            {
                                // PERF: Do not add a new key-value pair to the resultMap for unrelated entities or non-symbol based entities.
                                continue;
                            }

                            AnalysisEntity mergedKey = key1.WithMergedInstanceLocation(key2);
                            TValue newMergedValue = mergedValue;
                            var isExistingKeyInInput = false;
                            var isExistingKeyInResult = false;
                            if (resultMap.TryGetValue(mergedKey, out var existingValue))
                            {
                                newMergedValue = ValueDomain.Merge(newMergedValue, existingValue);
                                isExistingKeyInResult = true;
                            }

                            if (map1.TryGetValue(mergedKey, out existingValue))
                            {
                                newMergedValue = ValueDomain.Merge(newMergedValue, existingValue);
                                isExistingKeyInInput = true;
                            }

                            if (map2.TryGetValue(mergedKey, out existingValue))
                            {
                                newMergedValue = ValueDomain.Merge(newMergedValue, existingValue);
                                isExistingKeyInInput = true;
                            }

                            Debug.Assert(ValueDomain.Compare(value1, newMergedValue) <= 0);
                            Debug.Assert(ValueDomain.Compare(value2, newMergedValue) <= 0);
                            Debug.Assert(ValueDomain.Compare(mergedValue, newMergedValue) <= 0);
                            mergedValue = newMergedValue;

                            if (!isExistingKeyInInput && !isExistingKeyInResult && CanSkipNewEntry(mergedKey, mergedValue))
                            {
                                // PERF: Do not add a new key-value pair to the resultMap if the value can be skipped.
                                continue;
                            }

                            if (!isExistingKeyInInput)
                            {
                                newKeys.Add(mergedKey);
                            }

                            AssertValidEntryForMergedMap(mergedKey, mergedValue);
                            resultMap[mergedKey] = mergedValue;
                        }
                    }
                }
                else if (map2.TryGetValue(key1, out var value2))
                {
                    TValue mergedValue = ValueDomain.Merge(value1, value2);
                    Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                    Debug.Assert(ValueDomain.Compare(value2, mergedValue) <= 0);
                    resultMap.Add(key1, mergedValue);
                    continue;
                }

                if (!resultMap.ContainsKey(key1))
                {
                    TValue mergedValue = GetMergedValueForEntityPresentInOneMap(key1, value1);
                    Debug.Assert(ValueDomain.Compare(value1, mergedValue) <= 0);
                    AssertValidEntryForMergedMap(key1, mergedValue);
                    resultMap.Add(key1, mergedValue);
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
                    AssertValidEntryForMergedMap(key2, mergedValue);
                    resultMap.Add(key2, mergedValue);
                }
            }

            foreach (var newKey in newKeys)
            {
                Debug.Assert(!map1.ContainsKey(newKey));
                Debug.Assert(!map2.ContainsKey(newKey));
                if (ReferenceEquals(resultMap[newKey], GetDefaultValue(newKey)))
                {
                    resultMap.Remove(newKey);
                }
            }

            newKeys.Free();

            Debug.Assert(Compare(map1, resultMap) <= 0);
            Debug.Assert(Compare(map2, resultMap) <= 0);
            AssertValidAnalysisData(resultMap);

            return resultMap;
            static bool IsAnalysisEntityForFieldOrProperty(AnalysisEntity entity)
                => entity.SymbolOpt?.Kind == SymbolKind.Field || entity.SymbolOpt?.Kind == SymbolKind.Property;
        }
    }
}