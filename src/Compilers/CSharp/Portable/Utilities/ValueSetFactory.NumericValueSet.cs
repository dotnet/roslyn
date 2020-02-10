// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// The implementation of a value set for an numeric type <typeparamref name="T"/>.
        /// The implementation is based loosely on <em>interval trees</em>.
        /// When used for floating-point values, only numeric values are represented (i.e. values between
        /// MinValue and MaxValue, inclusive).
        /// </summary>
        private class NumericValueSet<T, TTC> : IValueSet<T> where TTC : struct, NumericTC<T>
        {

            private Interval rootInterval;

            internal NumericValueSet(Interval rootInterval) => this.rootInterval = rootInterval;

            IValueSetFactory IValueSet.Factory => NumericValueSetFactory<T, TTC>.Instance;

            bool IValueSet.IsEmpty => rootInterval is Interval.Excluded;

            IValueSetFactory<T> IValueSet<T>.Factory => NumericValueSetFactory<T, TTC>.Instance;

            public bool Any(BinaryOperatorKind relation, T value)
            {
                TTC tc = default;
                return AnyInterval(rootInterval, relation, value, tc.MinValue, tc.MaxValue);
            }

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, default(TTC).FromConstantValue(value));
            /// <summary>
            /// Compute the result of <see cref="IValueSet{T}.Any"/> for a given interval.
            /// </summary>
            /// <param name="minValue">the interval's minimum value, inclusive</param>
            /// <param name="maxValue">the interval's maximum value, inclusive</param>

            private static bool AnyInterval(Interval interval, BinaryOperatorKind relation, T value, T minValue, T maxValue)
            {
                TTC tc = default;
                switch (interval)
                {
                    case Interval.Excluded _:
                        return false;
                    case Interval.Included _:
                        switch (relation)
                        {
                            case LessThan:
                                return tc.Related(LessThan, minValue, value);
                            case LessThanOrEqual:
                                return tc.Related(LessThanOrEqual, minValue, value);
                            case GreaterThan:
                                return tc.Related(GreaterThan, maxValue, value);
                            case GreaterThanOrEqual:
                                return tc.Related(GreaterThanOrEqual, maxValue, value);
                            case Equal:
                                return tc.Related(LessThanOrEqual, minValue, value) && tc.Related(GreaterThanOrEqual, maxValue, value);
                            default:
                                throw new ArgumentException("relation");
                        }
                    case Interval.Mixed mixed:
                        switch (relation)
                        {
                            case LessThan when tc.Related(LessThan, value, minValue):
                                return false;
                            case LessThan when tc.Related(LessThan, maxValue, value):
                                return true;
                            case GreaterThan when tc.Related(GreaterThan, value, maxValue):
                                return false;
                            case GreaterThan when tc.Related(GreaterThan, minValue, value):
                                return true;
                            case LessThanOrEqual when tc.Related(LessThanOrEqual, value, minValue):
                                return false;
                            case LessThanOrEqual when tc.Related(LessThanOrEqual, maxValue, value):
                                return true;
                            case GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, value, maxValue):
                                return false;
                            case GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, minValue, value):
                                return true;
                            case Equal when tc.Related(LessThan, value, minValue) || tc.Related(LessThan, maxValue, value):
                                return false;
                            default:
                                var (leftMax, rightMin) = tc.Partition(minValue, maxValue);
                                return
                                    AnyInterval(mixed.Left, relation, value, minValue, leftMax) ||
                                    AnyInterval(mixed.Right, relation, value, rightMin, maxValue);
                        };
                    default:
                        throw new ArgumentException("interval");
                };
            }

            public bool All(BinaryOperatorKind relation, T value)
            {
                TTC tc = default;
                return AllInterval(rootInterval, relation, value, tc.MinValue, tc.MaxValue);
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TTC).FromConstantValue(value));
            /// <summary>
            /// Compute the result of <see cref="IValueSet{T}.All"/> for a given interval.
            /// </summary>
            /// <param name="minValue">the interval's minimum value, inclusive</param>
            /// <param name="maxValue">the interval's maximum value, inclusive</param>

            private static bool AllInterval(Interval interval, BinaryOperatorKind relation, T value, T minValue, T maxValue)
            {
                TTC tc = default;
                switch (interval)
                {
                    case Interval.Excluded _:
                        return true;
                    case Interval.Included _:
                        switch (relation)
                        {
                            case LessThan:
                                return tc.Related(LessThan, maxValue, value);
                            case LessThanOrEqual:
                                return tc.Related(LessThanOrEqual, maxValue, value);
                            case GreaterThan:
                                return tc.Related(GreaterThan, minValue, value);
                            case GreaterThanOrEqual:
                                return tc.Related(GreaterThanOrEqual, minValue, value);
                            case Equal:
                                return tc.Related(Equal, minValue, value) && tc.Related(Equal, maxValue, value);
                            default:
                                throw new ArgumentException("relation");
                        }
                    case Interval.Mixed mixed:
                        switch (relation)
                        {
                            case LessThan when tc.Related(LessThan, value, minValue):
                                return false;
                            case LessThan when tc.Related(LessThan, maxValue, value):
                                return true;
                            case GreaterThan when tc.Related(GreaterThan, value, maxValue):
                                return false;
                            case GreaterThan when tc.Related(GreaterThan, minValue, value):
                                return true;
                            case LessThanOrEqual when tc.Related(LessThanOrEqual, value, minValue):
                                return false;
                            case LessThanOrEqual when tc.Related(LessThanOrEqual, maxValue, value):
                                return true;
                            case GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, value, maxValue):
                                return false;
                            case GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, minValue, value):
                                return true;
                            default:
                                var (leftMax, rightMin) = tc.Partition(minValue, maxValue);
                                return
                                    AllInterval(mixed.Left, relation, value, minValue, leftMax) &&
                                    AllInterval(mixed.Right, relation, value, rightMin, maxValue);
                        }
                    default:
                        throw new ArgumentException("interval");
                };
            }

            public IValueSet<T> Complement() => new NumericValueSet<T, TTC>(ComplementInterval(rootInterval));

            IValueSet IValueSet.Complement() => this.Complement();

            private static Interval ComplementInterval(Interval interval)
            {
                switch (interval)
                {
                    case Interval.Included _:
                        return Interval.Excluded.Instance;
                    case Interval.Excluded _:
                        return Interval.Included.Instance;
                    case Interval.Mixed(var left, var right):
                        return Interval.Mixed.Create(ComplementInterval(left), ComplementInterval(right));
                    default:
                        throw new ArgumentException("interval");
                }
            }

            public IValueSet<T> Intersect(IValueSet<T> o)
            {
                var other = ((NumericValueSet<T, TTC>)o);
                var newInterval = IntersectInterval(rootInterval, other.rootInterval);
                if (newInterval == rootInterval)
                    return this;
                if (newInterval == other.rootInterval)
                    return other;
                return new NumericValueSet<T, TTC>(newInterval);
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IValueSet<T>)other);

            private Interval IntersectInterval(Interval left, Interval right)
            {
                switch (left, right)
                {
                    case (Interval.Excluded _, _):
                        return left;
                    case (_, Interval.Excluded _):
                        return right;
                    case (Interval.Included _, _):
                        return right;
                    case (_, Interval.Included _):
                        return left;
                    case (Interval.Mixed m1, Interval.Mixed m2):
                        return IntersectMixed(m1, m2);
                    default:
                        throw new ArgumentException("(left, right)");
                }
            }

            private Interval IntersectMixed(Interval.Mixed m1, Interval.Mixed m2)
            {
                if (m1 == m2)
                    return m1;
                var newLeft = IntersectInterval(m1.Left, m2.Left);
                var newRight = IntersectInterval(m1.Right, m2.Right);
                if (newLeft == m1.Left && newRight == m1.Right)
                    return m1;
                if (newLeft == m2.Left && newRight == m2.Right)
                    return m2;
                return Interval.Mixed.Create(newLeft, newRight);
            }

            public IValueSet<T> Union(IValueSet<T> o)
            {
                var other = ((NumericValueSet<T, TTC>)o);
                var newInterval = UnionInterval(rootInterval, other.rootInterval);
                if (newInterval == rootInterval)
                    return this;
                if (newInterval == other.rootInterval)
                    return other;
                return new NumericValueSet<T, TTC>(newInterval);
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<T>)other);

            private Interval UnionInterval(Interval left, Interval right)
            {
                switch (left, right)
                {
                    case (Interval.Excluded _, _):
                        return right;
                    case (_, Interval.Excluded _):
                        return left;
                    case (Interval.Included _, _):
                        return left;
                    case (_, Interval.Included _):
                        return right;
                    case (Interval.Mixed m1, Interval.Mixed m2) when m1 == m2:
                        return m1;
                    case (Interval.Mixed m1, Interval.Mixed m2):
                        return UnionMixed(m1, m2);
                    default:
                        throw new ArgumentException("(left, right)");
                }
            }

            private Interval UnionMixed(Interval.Mixed m1, Interval.Mixed m2)
            {
                var newLeft = UnionInterval(m1.Left, m2.Left);
                var newRight = UnionInterval(m1.Right, m2.Right);
                if (newLeft == m1.Left && newRight == m1.Right)
                    return m1;
                if (newLeft == m2.Left && newRight == m2.Right)
                    return m2;
                return Interval.Mixed.Create(newLeft, newRight);
            }

            internal static IValueSet<T> Random(int expectedSize, Random random)
            {
                TTC tc = default;

                // Since 2 out of 3 of the intervals will be merged with neighbors, triple the expected size
                return new NumericValueSet<T, TTC>(randomInterval(3 * expectedSize, tc.MinValue, tc.MaxValue));

                Interval randomInterval(int size, T minValue, T maxValue)
                {
                    if (size < 2 || tc.Related(Equal, minValue, maxValue))
                    {
                        return random.NextDouble() < 0.5 ? Interval.Included.Instance : Interval.Excluded.Instance;
                    }

                    int leftSize = 1 + random.Next(size - 1);
                    int rightSize = size - leftSize;
                    (T leftMax, T rightMin) = tc.Partition(minValue, maxValue);
                    return Interval.Mixed.Create(randomInterval(leftSize, minValue, leftMax), randomInterval(rightSize, rightMin, maxValue));
                }
            }

            /// <summary>
            /// An (inefficiently produced) string representation for testing purposes.
            /// </summary>
            public override string ToString()
            {
                TTC tc = default;
                var intervalSequence = asIntervalSequence(rootInterval, tc.MinValue, tc.MaxValue);
                intervalSequence = compressIntervalSequence(intervalSequence);
                return string.Join(",", intervalSequence.Select(p => $"[{tc.ToString(p.min)}..{tc.ToString(p.max)}]"));

                static IEnumerable<(T min, T max)> asIntervalSequence(Interval interval, T minValue, T maxValue)
                {
                    switch (interval)
                    {
                        case Interval.Included _:
                            yield return (minValue, maxValue);
                            break;
                        case Interval.Excluded _:
                            break;
                        case Interval.Mixed(var left, var right):
                            TTC tc = default;
                            (T leftMax, T rightMin) = tc.Partition(minValue, maxValue);
                            foreach (var p in asIntervalSequence(left, minValue, leftMax))
                                yield return p;
                            foreach (var p in asIntervalSequence(right, rightMin, maxValue))
                                yield return p;
                            break;
                    }
                }

                static IEnumerable<(T min, T max)> compressIntervalSequence(IEnumerable<(T min, T max)> intervalSequence)
                {
                    TTC tc = default;
                    (T min, T max) pending = default;
                    bool anySeen = false;
                    foreach (var p in intervalSequence)
                    {
                        if (!anySeen)
                        {
                            pending = p;
                            anySeen = true;
                            continue;
                        }

                        if (tc.Related(Equal, tc.Next(pending.max), p.min))
                        {
                            pending.max = p.max;
                        }
                        else
                        {
                            yield return pending;
                            pending = p;
                        }
                    }

                    if (anySeen)
                        yield return pending;
                }
            }

            public override bool Equals(object obj) => obj is NumericValueSet<T, TTC> other && this.rootInterval.Equals(other.rootInterval);

            public override int GetHashCode() => this.rootInterval.GetHashCode();
        }
    }
}
