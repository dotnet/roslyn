// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private sealed class EnumeratedValueSet<T> : IConstantValueSet<T>
            where T : notnull
        {
            /// <summary>
            /// In <see cref="_included"/>, then members are listed by inclusion.  Otherwise all members
            /// are assumed to be contained in the set unless excluded.
            /// </summary>
            private readonly bool _included;

            private readonly ImmutableHashSet<T> _membersIncludedOrExcluded;

            private readonly IEquatableValueTC<T> _tc;

            private EnumeratedValueSet(bool included, ImmutableHashSet<T> membersIncludedOrExcluded, IEquatableValueTC<T> tc) =>
                (this._included, this._membersIncludedOrExcluded, this._tc) = (included, membersIncludedOrExcluded, tc);

            public static EnumeratedValueSet<T> AllValues(IEquatableValueTC<T> tc)
                => new EnumeratedValueSet<T>(included: false, ImmutableHashSet<T>.Empty, tc);

            public static EnumeratedValueSet<T> NoValues(IEquatableValueTC<T> tc)
                => new EnumeratedValueSet<T>(included: true, ImmutableHashSet<T>.Empty, tc);

            internal static EnumeratedValueSet<T> Including(T value, IEquatableValueTC<T> tc)
                => new EnumeratedValueSet<T>(included: true, ImmutableHashSet<T>.Empty.Add(value), tc);

            public bool IsEmpty => _included && _membersIncludedOrExcluded.IsEmpty;

            ConstantValue IConstantValueSet.Sample
            {
                get
                {
                    if (IsEmpty) throw new ArgumentException();
                    if (_included)
                        return _tc.ToConstantValue(_membersIncludedOrExcluded.OrderBy(k => k).First());
                    if (typeof(T) == typeof(string))
                    {
                        // try some simple strings.
                        if (this.Any(BinaryOperatorKind.Equal, (T)(object)""))
                            return _tc.ToConstantValue((T)(object)"");
                        for (char c = 'A'; c <= 'z'; c++)
                            if (this.Any(BinaryOperatorKind.Equal, (T)(object)c.ToString()))
                                return _tc.ToConstantValue((T)(object)c.ToString());
                    }
                    // If that doesn't work, choose from a sufficiently large random selection of values.
                    // Since this is an excluded set, they cannot all be excluded
                    var candidates = _tc.RandomValues(_membersIncludedOrExcluded.Count + 1, new Random(0), _membersIncludedOrExcluded.Count + 1);
                    foreach (var value in candidates)
                    {
                        if (this.Any(BinaryOperatorKind.Equal, value))
                            return _tc.ToConstantValue(value);
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

            bool IConstantValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, _tc.FromConstantValue(value));

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

            bool IConstantValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, _tc.FromConstantValue(value));

            public IConstantValueSet<T> Complement() => new EnumeratedValueSet<T>(!_included, _membersIncludedOrExcluded, _tc);

            IValueSet IValueSet.Complement() => this.Complement();

            public IConstantValueSet<T> Intersect(IConstantValueSet<T> o)
            {
                if (this == o)
                    return this;
                var other = (EnumeratedValueSet<T>)o;
                Debug.Assert(object.ReferenceEquals(this._tc, other._tc));

                var (larger, smaller) = (this._membersIncludedOrExcluded.Count > other._membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                switch (larger._included, smaller._included)
                {
                    case (true, true):
                        return new EnumeratedValueSet<T>(true, larger._membersIncludedOrExcluded.Intersect(smaller._membersIncludedOrExcluded), _tc);
                    case (true, false):
                        return new EnumeratedValueSet<T>(true, larger._membersIncludedOrExcluded.Except(smaller._membersIncludedOrExcluded), _tc);
                    case (false, false):
                        return new EnumeratedValueSet<T>(false, larger._membersIncludedOrExcluded.Union(smaller._membersIncludedOrExcluded), _tc);
                    case (false, true):
                        return new EnumeratedValueSet<T>(true, smaller._membersIncludedOrExcluded.Except(larger._membersIncludedOrExcluded), _tc);
                }
            }

            IValueSet IValueSet.Intersect(IValueSet other) => Intersect((IConstantValueSet<T>)other);

            public IConstantValueSet<T> Union(IConstantValueSet<T> o)
            {
                if (this == o)
                    return this;
                var other = (EnumeratedValueSet<T>)o;
                Debug.Assert(object.ReferenceEquals(this._tc, other._tc));

                var (larger, smaller) = (this._membersIncludedOrExcluded.Count > other._membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                switch (larger._included, smaller._included)
                {
                    case (false, false):
                        return new EnumeratedValueSet<T>(false, larger._membersIncludedOrExcluded.Intersect(smaller._membersIncludedOrExcluded), _tc);
                    case (false, true):
                        return new EnumeratedValueSet<T>(false, larger._membersIncludedOrExcluded.Except(smaller._membersIncludedOrExcluded), _tc);
                    case (true, true):
                        return new EnumeratedValueSet<T>(true, larger._membersIncludedOrExcluded.Union(smaller._membersIncludedOrExcluded), _tc);
                    case (true, false):
                        return new EnumeratedValueSet<T>(false, smaller._membersIncludedOrExcluded.Except(larger._membersIncludedOrExcluded), _tc);
                }
            }

            IValueSet IValueSet.Union(IValueSet other) => Union((IConstantValueSet<T>)other);

            public override bool Equals(object? obj)
            {
                if (obj is not EnumeratedValueSet<T> other)
                    return false;

                Debug.Assert(object.ReferenceEquals(this._tc, other._tc));
                return this._included == other._included
                    && this._membersIncludedOrExcluded.SetEqualsWithoutIntermediateHashSet(other._membersIncludedOrExcluded);
            }

            public override int GetHashCode() => Hash.Combine(this._included.GetHashCode(), this._membersIncludedOrExcluded.GetHashCode());

            public override string ToString() => $"{(this._included ? "" : "~")}{{{string.Join(",", _membersIncludedOrExcluded.Select(o => o.ToString()))}{"}"}";
        }
    }
}
