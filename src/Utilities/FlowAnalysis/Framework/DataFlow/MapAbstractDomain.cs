// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// An abstract domain implementation for analyses that store dictionary typed data.
    /// </summary>
    internal class MapAbstractDomain<TKey, TValue> : AbstractAnalysisDomain<IDictionary<TKey, TValue>>
    {
        public MapAbstractDomain(AbstractValueDomain<TValue> valueDomain)
        {
            ValueDomain = valueDomain;
        }

        protected AbstractValueDomain<TValue> ValueDomain { get; }
        public override IDictionary<TKey, TValue> Bottom => new Dictionary<TKey, TValue>();
        public override IDictionary<TKey, TValue> Clone(IDictionary<TKey, TValue> value) => new Dictionary<TKey, TValue>(value);

        /// <summary>
        /// Compares if the abstract dataflow values in <paramref name="oldValue"/> against the values in <paramref name="newValue"/> to ensure
        /// dataflow function is a monotically increasing function. See https://en.wikipedia.org/wiki/Monotonic_function for understanding monotonic functions.
        /// <returns>
        /// 1) 0, if both the dictionaries are identical.
        /// 2) -1, if dictionaries are not identical and for every key in <paramref name="oldValue"/>, the corresponding key exists in <paramref name="newValue"/> and
        ///    the value of each such key in <paramref name="oldValue"/> is lesser than or equals the value in <paramref name="newValue"/>.
        /// 3) 1, otherwise.
        /// </returns>
        public sealed override int Compare(IDictionary<TKey, TValue> oldValue, IDictionary<TKey, TValue> newValue)
        {
            Debug.Assert(oldValue != null);
            Debug.Assert(newValue != null);

            if (ReferenceEquals(oldValue, newValue))
            {
                return 0;
            }

            // Ensure that every key in oldValue exists in newValue and the value corresponding to that key
            // is not greater in oldValue as compared to the value in newValue
            bool newValueIsBigger = false;
            foreach (var kvp in oldValue)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                if (!newValue.TryGetValue(key, out TValue otherValue))
                {
                    Debug.Fail("Non-monotonic Merge function");
                    return 1;
                }

                var result = ValueDomain.Compare(value, otherValue);

                if (result > 0)
                {
                    Debug.Fail("Non-monotonic Merge function");
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

        public override IDictionary<TKey, TValue> Merge(IDictionary<TKey, TValue> value1, IDictionary<TKey, TValue> value2)
        {
            Debug.Assert(value1 != null);
            Debug.Assert(value2 != null);

            var result = new Dictionary<TKey, TValue>(value1);
            foreach (var entry in value2)
            {
                if (result.TryGetValue(entry.Key, out TValue value))
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
    }
}