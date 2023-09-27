// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set that only supports equality and works by including or excluding specific values.
        /// This is used for value set of <see cref="System.String"/> because the language defines no
        /// relational operators for it; such a set can be formed only by including explicitly mentioned
        /// members (or the inverse, excluding them, by complementing the set).
        /// </summary>
        private sealed class EnumeratedValueSet<T, TTC> : IValueSet<T>
            where TTC : struct, IEquatableValueTC<T>
            where T : notnull
        {
            /// <summary>
            /// In <see cref="_included"/>, then members are listed by inclusion.  Otherwise all members
            /// are assumed to be contained in the set unless excluded.
            /// </summary>
            private readonly bool _included;

            private readonly ImmutableHashSet<T> _membersIncludedOrExcluded;

            private EnumeratedValueSet(bool included, ImmutableHashSet<T> membersIncludedOrExcluded) =>
                (this._included, this._membersIncludedOrExcluded) = (included, membersIncludedOrExcluded);

            public static readonly EnumeratedValueSet<T, TTC> AllValues = new EnumeratedValueSet<T, TTC>(included: false, ImmutableHashSet<T>.Empty);

            public static readonly EnumeratedValueSet<T, TTC> NoValues = new EnumeratedValueSet<T, TTC>(included: true, ImmutableHashSet<T>.Empty);

            internal static EnumeratedValueSet<T, TTC> Including(T value) => new EnumeratedValueSet<T, TTC>(included: true, ImmutableHashSet<T>.Empty.Add(value));

            public bool IsEmpty => _included && _membersIncludedOrExcluded.IsEmpty;

            ConstantValue IValueSet.Sample
            {
                get
                {
                    if (IsEmpty) throw new ArgumentException();
                    var tc = default(TTC);
                    if (_included)
                        return tc.ToConstantValue(_membersIncludedOrExcluded.OrderBy(k => k).First());
                    if (typeof(T) == typeof(string))
                    {
                        // try some simple strings.
                        if (this.Any(BinaryOperatorKind.Equal, (T)(object)""))
                            return tc.ToConstantValue((T)(object)"");
                        for (char c = 'A'; c <= 'z'; c++)
                            if (this.Any(BinaryOperatorKind.Equal, (T)(object)c.ToString()))
                                return tc.ToConstantValue((T)(object)c.ToString());
                    }
                    // If that doesn't work, choose from a sufficiently large random selection of values.
                    // Since this is an excluded set, they cannot all be excluded
                    var candidates = tc.RandomValues(_membersIncludedOrExcluded.Count + 1, new Random(0), _membersIncludedOrExcluded.Count + 1);
                    foreach (var value in candidates)
                    {
                        if (this.Any(BinaryOperatorKind.Equal, value))
                            return tc.ToConstantValue(value);
                    }

                    throw ExceptionUtilities.Unreachable();
                }
            }

            public bool Any(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case BinaryOperatorKind.Equal:
                        return _included == _membersIncludedOrExcluded.Contains(value);
                    default:
                        return true; // supported for error recovery
                }
            }

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, default(TTC).FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case BinaryOperatorKind.Equal:
                        if (!_included)
                            return false;
                        switch (_membersIncludedOrExcluded.Count)
                        {
                            case 0:
                                return true;
                            case 1:
                                return _membersIncludedOrExcluded.Contains(value);
                            default:
                                return false;
                        }
                    default:
                        return false; // supported for error recovery
                }
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TTC).FromConstantValue(value));

            public IValueSet<T> Complement() => new EnumeratedValueSet<T, TTC>(!_included, _membersIncludedOrExcluded);

            IValueSet IValueSet.Complement() => this.Complement();

            public IValueSet<T> Intersect(IValueSet<T> o)
            {
                if (this == o)
                    return this;
                var other = (EnumeratedValueSet<T, TTC>)o;
                var (larger, smaller) = (this._membersIncludedOrExcluded.Count > other._membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                switch (larger._included, smaller._included)
                {
                    case (true, true):
                        return new EnumeratedValueSet<T, TTC>(true, larger._membersIncludedOrExcluded.Intersect(smaller._membersIncludedOrExcluded));
                    case (true, false):
                        return new EnumeratedValueSet<T, TTC>(true, larger._membersIncludedOrExcluded.Except(smaller._membersIncludedOrExcluded));
                    case (false, false):
                        return new EnumeratedValueSet<T, TTC>(false, larger._membersIncludedOrExcluded.Union(smaller._membersIncludedOrExcluded));
                    case (false, true):
                        return new EnumeratedValueSet<T, TTC>(true, smaller._membersIncludedOrExcluded.Except(larger._membersIncludedOrExcluded));
                }
            }

            IValueSet IValueSet.Intersect(IValueSet other) => Intersect((IValueSet<T>)other);

            public IValueSet<T> Union(IValueSet<T> o)
            {
                if (this == o)
                    return this;
                var other = (EnumeratedValueSet<T, TTC>)o;
                var (larger, smaller) = (this._membersIncludedOrExcluded.Count > other._membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                switch (larger._included, smaller._included)
                {
                    case (false, false):
                        return new EnumeratedValueSet<T, TTC>(false, larger._membersIncludedOrExcluded.Intersect(smaller._membersIncludedOrExcluded));
                    case (false, true):
                        return new EnumeratedValueSet<T, TTC>(false, larger._membersIncludedOrExcluded.Except(smaller._membersIncludedOrExcluded));
                    case (true, true):
                        return new EnumeratedValueSet<T, TTC>(true, larger._membersIncludedOrExcluded.Union(smaller._membersIncludedOrExcluded));
                    case (true, false):
                        return new EnumeratedValueSet<T, TTC>(false, smaller._membersIncludedOrExcluded.Except(larger._membersIncludedOrExcluded));
                }
            }

            IValueSet IValueSet.Union(IValueSet other) => Union((IValueSet<T>)other);

            public override bool Equals(object? obj)
            {
                if (obj is not EnumeratedValueSet<T, TTC> other)
                    return false;

                return this._included == other._included
                    && this._membersIncludedOrExcluded.SetEqualsWithoutIntermediateHashSet(other._membersIncludedOrExcluded);
            }

            public override int GetHashCode() => Hash.Combine(this._included.GetHashCode(), this._membersIncludedOrExcluded.GetHashCode());

            public override string ToString() => $"{(this._included ? "" : "~")}{{{string.Join(",", _membersIncludedOrExcluded.Select(o => o.ToString()))}{"}"}";
        }
    }
}
