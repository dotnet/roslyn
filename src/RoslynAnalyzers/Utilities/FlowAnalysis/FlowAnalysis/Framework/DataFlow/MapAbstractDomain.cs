﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// An abstract domain implementation for analyses that store dictionary typed data.
    /// </summary>
    public class MapAbstractDomain<TKey, TValue> : AbstractAnalysisDomain<DictionaryAnalysisData<TKey, TValue>>
        where TKey : notnull
    {
        public MapAbstractDomain(AbstractValueDomain<TValue> valueDomain)
        {
            ValueDomain = valueDomain;
        }

        protected AbstractValueDomain<TValue> ValueDomain { get; }
        public override DictionaryAnalysisData<TKey, TValue> Clone(DictionaryAnalysisData<TKey, TValue> value) => new(value);

        /// <summary>
        /// Compares if the abstract dataflow values in <paramref name="oldValue"/> against the values in <paramref name="newValue"/> to ensure
        /// dataflow function is a monotically increasing function. See https://en.wikipedia.org/wiki/Monotonic_function for understanding monotonic functions.
        /// </summary>
        /// <returns>
        /// 1) 0, if both the dictionaries are identical.
        /// 2) -1, if dictionaries are not identical and for every key in <paramref name="oldValue"/>, the corresponding key exists in <paramref name="newValue"/> and
        ///    the value of each such key in <paramref name="oldValue"/> is lesser than or equals the value in <paramref name="newValue"/>.
        /// 3) 1, otherwise.
        /// </returns>
        public sealed override int Compare(DictionaryAnalysisData<TKey, TValue> oldValue, DictionaryAnalysisData<TKey, TValue> newValue)
            => Compare(oldValue, newValue, assertMonotonicity: true);

        public sealed override bool Equals(DictionaryAnalysisData<TKey, TValue> value1, DictionaryAnalysisData<TKey, TValue> value2)
            => Compare(value1, value2, assertMonotonicity: false) == 0;

        private int Compare(DictionaryAnalysisData<TKey, TValue> oldValue, DictionaryAnalysisData<TKey, TValue> newValue, bool assertMonotonicity)
        {
            if (ReferenceEquals(oldValue, newValue))
            {
                return 0;
            }

            if (newValue.Count < oldValue.Count)
            {
                FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                return 1;
            }

            // Ensure that every key in oldValue exists in newValue and the value corresponding to that key
            // is not greater in oldValue as compared to the value in newValue
            bool newValueIsBigger = false;
            foreach (var kvp in oldValue)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                if (!newValue.TryGetValue(key, out var otherValue))
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    return 1;
                }

                var result = ValueDomain.Compare(value, otherValue, assertMonotonicity);

                if (result > 0)
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    return 1;
                }
                else if (result < 0)
                {
                    newValueIsBigger = true;
                }
            }

            if (!newValueIsBigger)
            {
                newValueIsBigger = newValue.Count > oldValue.Count;
            }

            return newValueIsBigger ? -1 : 0;
        }

#pragma warning disable CA1030 // Use events where appropriate
        [Conditional("DEBUG")]
        private static void FireNonMonotonicAssertIfNeeded(bool assertMonotonicity)
        {
            if (assertMonotonicity)
            {
                Debug.Fail("Non-monotonic merge");
            }
        }
#pragma warning restore CA1030 // Use events where appropriate

        public override DictionaryAnalysisData<TKey, TValue> Merge(DictionaryAnalysisData<TKey, TValue> value1, DictionaryAnalysisData<TKey, TValue> value2)
        {
            var result = new DictionaryAnalysisData<TKey, TValue>(value1);
            foreach (var entry in value2)
            {
                if (result.TryGetValue(entry.Key, out var value))
                {
                    value = ValueDomain.Merge(value, entry.Value);

                    if (value != null)
                    {
                        result[entry.Key] = value;
                    }
                    else
                    {
                        result.Remove(entry.Key);
                    }
                }
                else
                {
                    result.Add(entry.Key, entry.Value);
                }
            }

            return result;
        }

        internal DictionaryAnalysisData<TKey, TValue> Intersect(
            DictionaryAnalysisData<TKey, TValue> map1,
            DictionaryAnalysisData<TKey, TValue> map2,
            Func<TValue, TValue, TValue> intersect)
        {
            var result = new DictionaryAnalysisData<TKey, TValue>();
            foreach (var kvp in map1)
            {
                if (!map2.TryGetValue(kvp.Key, out var value2))
                {
                    value2 = ValueDomain.UnknownOrMayBeValue;
                }

                result.Add(kvp.Key, intersect(kvp.Value, value2));
            }

            foreach (var key in map2.Keys)
            {
                if (!result.ContainsKey(key))
                {
                    result.Add(key, ValueDomain.UnknownOrMayBeValue);
                }
            }

            return result;
        }
    }
}
