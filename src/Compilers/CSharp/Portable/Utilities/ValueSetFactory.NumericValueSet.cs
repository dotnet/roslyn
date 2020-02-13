// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private sealed class NumericValueSet<T, TTC> : IValueSet<T> where TTC : struct, INumericTC<T>
        {
            private readonly Interval _rootInterval;

            internal NumericValueSet(Interval rootInterval) => this._rootInterval = rootInterval;

            bool IValueSet.IsEmpty => _rootInterval is Interval.Excluded;

            public bool Any(BinaryOperatorKind relation, T value)
            {
                TTC tc = default;
                return anyInterval(_rootInterval, relation, value, tc.MinValue, tc.MaxValue);

                // Compute the result of <see cref="IValueSet{T}.Any"/> for a given interval.
                static bool anyInterval(Interval interval, BinaryOperatorKind relation, T value, T minValue, T maxValue)
                {
                    TTC tc = default;
                    Debug.Assert(tc.Related(LessThanOrEqual, minValue, maxValue));
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
                                    throw new ArgumentException(nameof(relation));
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
                                        anyInterval(mixed.Left, relation, value, minValue, leftMax) ||
                                        anyInterval(mixed.Right, relation, value, rightMin, maxValue);
                            };
                        default:
                            throw new ArgumentException(nameof(interval));
                    };
                }
            }

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, default(TTC).FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, T value)
            {
                TTC tc = default;
                return allInterval(_rootInterval, relation, value, tc.MinValue, tc.MaxValue);

                // Compute the result of <see cref="IValueSet{T}.All"/> for a given interval.
                static bool allInterval(Interval interval, BinaryOperatorKind relation, T value, T minValue, T maxValue)
                {
                    TTC tc = default;
                    Debug.Assert(tc.Related(LessThanOrEqual, minValue, maxValue));
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
                                    throw new ArgumentException(nameof(relation));
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
                                        allInterval(mixed.Left, relation, value, minValue, leftMax) &&
                                        allInterval(mixed.Right, relation, value, rightMin, maxValue);
                            }
                        default:
                            throw new ArgumentException(nameof(interval));
                    };
                }
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TTC).FromConstantValue(value));

            public IValueSet<T> Complement()
            {
                return new NumericValueSet<T, TTC>(complementInterval(_rootInterval));

                static Interval complementInterval(Interval interval)
                {
                    switch (interval)
                    {
                        case Interval.Included _:
                            return Interval.Excluded.Instance;
                        case Interval.Excluded _:
                            return Interval.Included.Instance;
                        case Interval.Mixed(var left, var right):
                            return Interval.Mixed.Create(complementInterval(left), complementInterval(right));
                        default:
                            throw new ArgumentException(nameof(interval));
                    }
                }
            }

            IValueSet IValueSet.Complement() => this.Complement();

            public IValueSet<T> Intersect(IValueSet<T> o)
            {
                var other = ((NumericValueSet<T, TTC>)o);
                var newInterval = intersectInterval(_rootInterval, other._rootInterval);
                if (newInterval == _rootInterval)
                    return this;
                if (newInterval == other._rootInterval)
                    return other;
                return new NumericValueSet<T, TTC>(newInterval);

                static Interval intersectInterval(Interval left, Interval right)
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
                            return intersectMixed(m1, m2);
                        default:
                            throw new ArgumentException("(left, right)");
                    }
                }

                static Interval intersectMixed(Interval.Mixed m1, Interval.Mixed m2)
                {
                    if (m1 == m2)
                        return m1;
                    var newLeft = intersectInterval(m1.Left, m2.Left);
                    var newRight = intersectInterval(m1.Right, m2.Right);
                    if (newLeft == m1.Left && newRight == m1.Right)
                        return m1;
                    if (newLeft == m2.Left && newRight == m2.Right)
                        return m2;
                    return Interval.Mixed.Create(newLeft, newRight);
                }
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IValueSet<T>)other);

            public IValueSet<T> Union(IValueSet<T> o)
            {
                var other = ((NumericValueSet<T, TTC>)o);
                var newInterval = unionInterval(_rootInterval, other._rootInterval);
                if (newInterval == _rootInterval)
                    return this;
                if (newInterval == other._rootInterval)
                    return other;
                return new NumericValueSet<T, TTC>(newInterval);

                static Interval unionInterval(Interval left, Interval right)
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
                            return unionMixed(m1, m2);
                        default:
                            throw new ArgumentException("(left, right)");
                    }
                }

                static Interval unionMixed(Interval.Mixed m1, Interval.Mixed m2)
                {
                    var newLeft = unionInterval(m1.Left, m2.Left);
                    var newRight = unionInterval(m1.Right, m2.Right);
                    if (newLeft == m1.Left && newRight == m1.Right)
                        return m1;
                    if (newLeft == m2.Left && newRight == m2.Right)
                        return m2;
                    return Interval.Mixed.Create(newLeft, newRight);
                }
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<T>)other);

            internal static IValueSet<T> Random(int expectedSize, Random random)
            {
                TTC tc = default;

                // Since 2 out of 3 of the intervals will be merged with neighbors, triple the expected size
                return new NumericValueSet<T, TTC>(randomInterval(3 * expectedSize, tc.MinValue, tc.MaxValue));

                Interval randomInterval(int size, T minValue, T maxValue)
                {
                    if (size < 2 || tc.Related(Equal, minValue, maxValue))
                        return random.NextDouble() < 0.5 ? Interval.Included.Instance : Interval.Excluded.Instance;

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
                var intervalSequence = asIntervalSequence(_rootInterval, tc.MinValue, tc.MaxValue);
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

            public override bool Equals(object? obj) => obj is NumericValueSet<T, TTC> other && this._rootInterval.Equals(other._rootInterval);

            public override int GetHashCode() => this._rootInterval.GetHashCode();
        }
    }
}
