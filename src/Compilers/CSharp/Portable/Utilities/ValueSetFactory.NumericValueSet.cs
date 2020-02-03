// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                return interval switch
                {
                    Interval.Excluded _ => false,
                    Interval.Included _ => relation switch
                    {
                        LessThan => tc.Related(LessThan, minValue, value),
                        LessThanOrEqual => tc.Related(LessThanOrEqual, minValue, value),
                        GreaterThan => tc.Related(GreaterThan, maxValue, value),
                        GreaterThanOrEqual => tc.Related(GreaterThanOrEqual, maxValue, value),
                        NotEqual => tc.Related(GreaterThan, minValue, value) || tc.Related(LessThan, maxValue, value),
                        Equal => tc.Related(LessThanOrEqual, minValue, value) && tc.Related(GreaterThanOrEqual, maxValue, value),
                        _ => throw new ArgumentException("relation"),
                    },
                    Interval.Mixed mixed => relation switch
                    {
                        LessThan when tc.Related(LessThan, value, minValue) => false,
                        LessThan when tc.Related(LessThan, maxValue, value) => true,
                        GreaterThan when tc.Related(GreaterThan, value, maxValue) => false,
                        GreaterThan when tc.Related(GreaterThan, minValue, value) => true,
                        LessThanOrEqual when tc.Related(LessThanOrEqual, value, minValue) => false,
                        LessThanOrEqual when tc.Related(LessThanOrEqual, maxValue, value) => true,
                        GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, value, maxValue) => false,
                        GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, minValue, value) => true,
                        NotEqual => true, // a mixed interval contains more than one value
                        Equal when tc.Related(LessThan, value, minValue) || tc.Related(LessThan, maxValue, value) => false,
                        _ when tc.Partition(minValue, maxValue) is var (leftMax, rightMin) =>
                            AnyInterval(mixed.Left, relation, value, minValue, leftMax) ||
                            AnyInterval(mixed.Right, relation, value, rightMin, maxValue),
                        _ => throw new ArgumentException("relation"),
                    },
                    _ => throw new ArgumentException("interval"),
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
                return interval switch
                {
                    Interval.Excluded _ => true,
                    Interval.Included _ => relation switch
                    {
                        LessThan => tc.Related(LessThan, maxValue, value),
                        LessThanOrEqual => tc.Related(LessThanOrEqual, maxValue, value),
                        GreaterThan => tc.Related(GreaterThan, minValue, value),
                        GreaterThanOrEqual => tc.Related(GreaterThanOrEqual, minValue, value),
                        NotEqual => tc.Related(GreaterThan, minValue, value) || tc.Related(LessThan, maxValue, value),
                        Equal => tc.Related(Equal, minValue, value) && tc.Related(Equal, maxValue, value),
                        _ => throw new ArgumentException("relation"),
                    },
                    Interval.Mixed mixed => relation switch
                    {
                        LessThan when tc.Related(LessThan, value, minValue) => false,
                        LessThan when tc.Related(LessThan, maxValue, value) => true,
                        GreaterThan when tc.Related(GreaterThan, value, maxValue) => false,
                        GreaterThan when tc.Related(GreaterThan, minValue, value) => true,
                        LessThanOrEqual when tc.Related(LessThanOrEqual, value, minValue) => false,
                        LessThanOrEqual when tc.Related(LessThanOrEqual, maxValue, value) => true,
                        GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, value, maxValue) => false,
                        GreaterThanOrEqual when tc.Related(GreaterThanOrEqual, minValue, value) => true,
                        NotEqual when tc.Related(LessThan, value, minValue) || tc.Related(LessThan, maxValue, value) => true,
                        _ when tc.Partition(minValue, maxValue) is var (leftMax, rightMin) =>
                            AllInterval(mixed.Left, relation, value, minValue, leftMax) &&
                            AllInterval(mixed.Right, relation, value, rightMin, maxValue),
                        _ => throw new ArgumentException("relation"),
                    },
                    _ => throw new ArgumentException("interval"),
                };
            }

            public IValueSet<T> Complement() => new NumericValueSet<T, TTC>(ComplementInterval(rootInterval));
            IValueSet IValueSet.Complement() => this.Complement();
            private static Interval ComplementInterval(Interval interval) => interval switch
            {
                Interval.Included _ => Interval.Excluded.Instance,
                Interval.Excluded _ => Interval.Included.Instance,
                Interval.Mixed(var left, var right) => Interval.Mixed.Create(ComplementInterval(left), ComplementInterval(right)),
                _ => throw new ArgumentException("interval"),
            };

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
            private Interval IntersectInterval(Interval left, Interval right) => (left, right) switch
            {
                (Interval.Excluded _, _) => left,
                (_, Interval.Excluded _) => right,
                (Interval.Included _, _) => right,
                (_, Interval.Included _) => left,
                (Interval.Mixed m1, Interval.Mixed m2) when m1 == m2 => m1,
                (Interval.Mixed m1, Interval.Mixed m2) => IntersectMixed(m1, m2),
                _ => throw new ArgumentException("(left, right)"),
            };
            private Interval IntersectMixed(Interval.Mixed m1, Interval.Mixed m2)
            {
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
            private Interval UnionInterval(Interval left, Interval right) => (left, right) switch
            {
                (Interval.Excluded _, _) => right,
                (_, Interval.Excluded _) => left,
                (Interval.Included _, _) => left,
                (_, Interval.Included _) => right,
                (Interval.Mixed m1, Interval.Mixed m2) when m1 == m2 => m1,
                (Interval.Mixed m1, Interval.Mixed m2) => UnionMixed(m1, m2),
                _ => throw new ArgumentException("(left, right)"),
            };
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

            /// <summary>
            /// An (inefficiently produced) string representation for testing purposes.
            /// </summary>
            public override string ToString()
            {
                TTC tc = default;
                var intervalSequence = AsIntervalSequence(rootInterval, tc.MinValue, tc.MaxValue);
                intervalSequence = CompressIntervalSequence(intervalSequence);
                return string.Join(",", intervalSequence.Select(p => $"[{p.min}..{p.max}]"));
            }
            private static IEnumerable<(T min, T max)> AsIntervalSequence(Interval interval, T minValue, T maxValue)
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
                        foreach (var p in AsIntervalSequence(left, minValue, leftMax))
                            yield return p;
                        foreach (var p in AsIntervalSequence(right, rightMin, maxValue))
                            yield return p;
                        break;
                }
            }
            private static IEnumerable<(T min, T max)> CompressIntervalSequence(IEnumerable<(T min, T max)> intervalSequence)
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

            public override bool Equals(object obj) => obj is NumericValueSet<T, TTC> other && this.rootInterval.Equals(other.rootInterval);
            public override int GetHashCode() => this.rootInterval.GetHashCode();
        }
    }
}
