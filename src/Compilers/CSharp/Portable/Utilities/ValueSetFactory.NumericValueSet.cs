// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// The implementation of a value set for an numeric type <typeparamref name="T"/>.
        /// </summary>
        private sealed class NumericValueSet<T> : IConstantValueSet<T>
        {
            private readonly ImmutableArray<(T first, T last)> _intervals;
            private readonly INumericTC<T> _tc;

            public static NumericValueSet<T> AllValues(INumericTC<T> tc) => new NumericValueSet<T>(tc.MinValue, tc.MaxValue, tc);

            public static NumericValueSet<T> NoValues(INumericTC<T> tc) => new NumericValueSet<T>(ImmutableArray<(T first, T last)>.Empty, tc);

            internal NumericValueSet(T first, T last, INumericTC<T> tc) : this(ImmutableArray.Create((first, last)), tc)
            {
                Debug.Assert(tc.Related(LessThanOrEqual, first, last));
            }

            internal NumericValueSet(ImmutableArray<(T first, T last)> intervals, INumericTC<T> tc)
            {
#if DEBUG
                Debug.Assert(intervals.Length == 0 || tc.Related(GreaterThanOrEqual, intervals[0].first, tc.MinValue));
                for (int i = 0, n = intervals.Length; i < n; i++)
                {
                    Debug.Assert(tc.Related(LessThanOrEqual, intervals[i].first, intervals[i].last));
                    if (i != 0)
                    {
                        // intervals are in increasing order with a gap between them
                        Debug.Assert(tc.Related(LessThan, tc.Next(intervals[i - 1].last), intervals[i].first));
                    }
                }
#endif
                _intervals = intervals;
                _tc = tc;
            }

            public bool IsEmpty => _intervals.Length == 0;

            ConstantValue IConstantValueSet.Sample
            {
                get
                {
                    if (IsEmpty)
                        throw new ArgumentException();

                    // Prefer a value near zero.
                    var gz = new NumericValueSetFactory<T>(_tc).Related(BinaryOperatorKind.GreaterThanOrEqual, _tc.Zero);
                    var t = (NumericValueSet<T>)this.Intersect(gz);
                    if (!t.IsEmpty)
                        return _tc.ToConstantValue(t._intervals[0].first);
                    return _tc.ToConstantValue(this._intervals[this._intervals.Length - 1].last);
                }
            }

            public bool Any(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case LessThan:
                    case LessThanOrEqual:
                        return _intervals.Length > 0 && _tc.Related(relation, _intervals[0].first, value);
                    case GreaterThan:
                    case GreaterThanOrEqual:
                        return _intervals.Length > 0 && _tc.Related(relation, _intervals[_intervals.Length - 1].last, value);
                    case Equal:
                        return anyIntervalContains(0, _intervals.Length - 1, value);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(relation);
                }

                bool anyIntervalContains(int firstIntervalIndex, int lastIntervalIndex, T value)
                {
                    while (true)
                    {
                        if (lastIntervalIndex < firstIntervalIndex)
                            return false;

                        if (lastIntervalIndex == firstIntervalIndex)
                            return _tc.Related(GreaterThanOrEqual, value, _intervals[lastIntervalIndex].first) && _tc.Related(LessThanOrEqual, value, _intervals[lastIntervalIndex].last);

                        int midIndex = firstIntervalIndex + (lastIntervalIndex - firstIntervalIndex) / 2;
                        if (_tc.Related(LessThanOrEqual, value, _intervals[midIndex].last))
                            lastIntervalIndex = midIndex;
                        else
                            firstIntervalIndex = midIndex + 1;
                    }
                }
            }

            bool IConstantValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, _tc.FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, T value)
            {
                if (_intervals.Length == 0)
                    return true;

                switch (relation)
                {
                    case LessThan:
                    case LessThanOrEqual:
                        return _tc.Related(relation, _intervals[_intervals.Length - 1].last, value);
                    case GreaterThan:
                    case GreaterThanOrEqual:
                        return _tc.Related(relation, _intervals[0].first, value);
                    case Equal:
                        return _intervals.Length == 1 && _tc.Related(Equal, _intervals[0].first, value) && _tc.Related(Equal, _intervals[0].last, value);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(relation);
                }
            }

            bool IConstantValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, _tc.FromConstantValue(value));

            public IConstantValueSet<T> Complement()
            {
                if (_intervals.Length == 0)
                    return AllValues(_tc);

                var builder = ArrayBuilder<(T first, T last)>.GetInstance();

                // add a prefix if apropos.
                if (_tc.Related(LessThan, _tc.MinValue, _intervals[0].first))
                {
                    builder.Add((_tc.MinValue, _tc.Prev(_intervals[0].first)));
                }

                // add the in-between intervals
                int lastIndex = _intervals.Length - 1;
                for (int i = 0; i < lastIndex; i++)
                {
                    builder.Add((_tc.Next(_intervals[i].last), _tc.Prev(_intervals[i + 1].first)));
                }

                // add a suffix if apropos
                if (_tc.Related(LessThan, _intervals[lastIndex].last, _tc.MaxValue))
                {
                    builder.Add((_tc.Next(_intervals[lastIndex].last), _tc.MaxValue));
                }

                return new NumericValueSet<T>(builder.ToImmutableAndFree(), _tc);
            }

            IValueSet IValueSet.Complement() => this.Complement();

            public IConstantValueSet<T> Intersect(IConstantValueSet<T> o)
            {
                var other = (NumericValueSet<T>)o;
                Debug.Assert(this._tc.GetType() == other._tc.GetType());

                var builder = ArrayBuilder<(T first, T last)>.GetInstance();
                var left = this._intervals;
                var right = other._intervals;
                int l = 0;
                int r = 0;
                while (l < left.Length && r < right.Length)
                {
                    var leftInterval = left[l];
                    var rightInterval = right[r];
                    if (_tc.Related(LessThan, leftInterval.last, rightInterval.first))
                    {
                        l++;
                    }
                    else if (_tc.Related(LessThan, rightInterval.last, leftInterval.first))
                    {
                        r++;
                    }
                    else
                    {
                        Add(builder, Max(leftInterval.first, rightInterval.first, _tc), Min(leftInterval.last, rightInterval.last, _tc), _tc);
                        if (_tc.Related(LessThan, leftInterval.last, rightInterval.last))
                        {
                            l++;
                        }
                        else if (_tc.Related(LessThan, rightInterval.last, leftInterval.last))
                        {
                            r++;
                        }
                        else
                        {
                            l++;
                            r++;
                        }
                    }
                }

                return new NumericValueSet<T>(builder.ToImmutableAndFree(), _tc);
            }

            /// <summary>
            /// Add an interval to the end of the builder.
            /// </summary>
            private static void Add(ArrayBuilder<(T first, T last)> builder, T first, T last, INumericTC<T> tc)
            {
                Debug.Assert(tc.Related(LessThanOrEqual, first, last));
                Debug.Assert(tc.Related(GreaterThanOrEqual, first, tc.MinValue));
                Debug.Assert(tc.Related(LessThanOrEqual, last, tc.MaxValue));
                Debug.Assert(builder.Count == 0 || tc.Related(LessThanOrEqual, builder.Last().first, first));
                if (builder.Count > 0 && (tc.Related(Equal, tc.MinValue, first) || tc.Related(GreaterThanOrEqual, builder.Last().last, tc.Prev(first))))
                {
                    // merge with previous interval when adjacent
                    var oldLastInterval = builder.Pop();
                    oldLastInterval.last = Max(last, oldLastInterval.last, tc);
                    builder.Push(oldLastInterval);
                }
                else
                {
                    builder.Add((first, last));
                }
            }
            private static T Min(T a, T b, INumericTC<T> tc)
            {
                return tc.Related(LessThan, a, b) ? a : b;
            }

            private static T Max(T a, T b, INumericTC<T> tc)
            {
                return tc.Related(LessThan, a, b) ? b : a;
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IConstantValueSet<T>)other);

            public IConstantValueSet<T> Union(IConstantValueSet<T> o)
            {
                var other = (NumericValueSet<T>)o;
                Debug.Assert(this._tc.GetType() == other._tc.GetType());

                var builder = ArrayBuilder<(T first, T last)>.GetInstance();
                var left = this._intervals;
                var right = other._intervals;
                int l = 0;
                int r = 0;
                while (l < left.Length && r < right.Length)
                {
                    var leftInterval = left[l];
                    var rightInterval = right[r];
                    if (_tc.Related(LessThan, leftInterval.last, rightInterval.first))
                    {
                        Add(builder, leftInterval.first, leftInterval.last, _tc);
                        l++;
                    }
                    else if (_tc.Related(LessThan, rightInterval.last, leftInterval.first))
                    {
                        Add(builder, rightInterval.first, rightInterval.last, _tc);
                        r++;
                    }
                    else
                    {
                        Add(builder, Min(leftInterval.first, rightInterval.first, _tc), Max(leftInterval.last, rightInterval.last, _tc), _tc);
                        l++;
                        r++;
                    }
                }

                while (l < left.Length)
                {
                    var leftInterval = left[l];
                    Add(builder, leftInterval.first, leftInterval.last, _tc);
                    l++;
                }

                while (r < right.Length)
                {
                    var rightInterval = right[r];
                    Add(builder, rightInterval.first, rightInterval.last, _tc);
                    r++;
                }

                return new NumericValueSet<T>(builder.ToImmutableAndFree(), _tc);
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IConstantValueSet<T>)other);

            /// <summary>
            /// Produce a random value set for testing purposes.
            /// </summary>
            internal static IConstantValueSet<T> Random(int expectedSize, Random random, INumericTC<T> tc)
            {
                T[] values = new T[expectedSize * 2];
                for (int i = 0, n = expectedSize * 2; i < n; i++)
                {
                    values[i] = tc.Random(random);
                }
                Array.Sort(values);
                var builder = ArrayBuilder<(T first, T last)>.GetInstance();
                for (int i = 0, n = values.Length; i < n; i += 2)
                {
                    T first = values[i];
                    T last = values[i + 1];
                    Add(builder, first, last, tc);
                }

                return new NumericValueSet<T>(builder.ToImmutableAndFree(), tc);
            }

            /// <summary>
            /// A string representation for testing purposes.
            /// </summary>
            public override string ToString()
            {
                return string.Join(",", this._intervals.Select(p => $"[{_tc.ToString(p.first)}..{_tc.ToString(p.last)}]"));
            }

            public override bool Equals(object? obj) =>
                obj is NumericValueSet<T> other &&
                this._intervals.SequenceEqual(other._intervals);

            public override int GetHashCode()
            {
                return Hash.Combine(Hash.CombineValues(_intervals), _intervals.Length);
            }
        }
    }
}
