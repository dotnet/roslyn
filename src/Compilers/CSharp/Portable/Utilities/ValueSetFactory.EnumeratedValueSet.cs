// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set that only supports equality and works by including or excluding specific values.
        /// </summary>
        private class EnumeratedValueSet<T, TTC> : IValueSet<T> where TTC : struct, EqualableValueTC<T>
        {
            /// <summary>
            /// In <see cref="included"/>, then members are listed by inclusion.  Otherwise all members
            /// are assumed to be contained in the set unless excluded.
            /// </summary>
            private bool included;

            private ImmutableHashSet<T> membersIncludedOrExcluded;

            private EnumeratedValueSet(bool included, ImmutableHashSet<T> membersIncludedOrExcluded) =>
                (this.included, this.membersIncludedOrExcluded) = (included, membersIncludedOrExcluded);

            public static EnumeratedValueSet<T, TTC> AllValues = new EnumeratedValueSet<T, TTC>(included: false, ImmutableHashSet<T>.Empty);

            public static EnumeratedValueSet<T, TTC> None = new EnumeratedValueSet<T, TTC>(included: true, ImmutableHashSet<T>.Empty);

            internal static EnumeratedValueSet<T, TTC> Including(T value) => new EnumeratedValueSet<T, TTC>(included: true, ImmutableHashSet<T>.Empty.Add(value));

            IValueSetFactory<T> IValueSet<T>.Factory => EnumeratedValueSetFactory<T, TTC>.Instance;

            IValueSetFactory IValueSet.Factory => EnumeratedValueSetFactory<T, TTC>.Instance;

            bool IValueSet.IsEmpty => included && membersIncludedOrExcluded.IsEmpty;

            public bool Any(BinaryOperatorKind relation, T value)
            {
                switch (relation)
                {
                    case BinaryOperatorKind.Equal:
                        return included == membersIncludedOrExcluded.Contains(value);
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
                        if (!included)
                            return false;
                        switch (membersIncludedOrExcluded.Count)
                        {
                            case 0:
                                return true;
                            case 1:
                                return membersIncludedOrExcluded.Contains(value);
                            default:
                                return false;
                        }
                    default:
                        return false; // supported for error recovery
                }
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TTC).FromConstantValue(value));

            IValueSet<T> IValueSet<T>.Complement() => new EnumeratedValueSet<T, TTC>(!included, membersIncludedOrExcluded);

            IValueSet IValueSet.Complement() => new EnumeratedValueSet<T, TTC>(!included, membersIncludedOrExcluded);

            public IValueSet<T> Intersect(IValueSet<T> o)
            {
                if (this == o)
                    return this;
                var other = (EnumeratedValueSet<T, TTC>)o;
                var (larger, smaller) = (this.membersIncludedOrExcluded.Count > other.membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                switch (larger.included, smaller.included)
                {
                    case (true, true):
                        return new EnumeratedValueSet<T, TTC>(true, this.membersIncludedOrExcluded.Intersect(other.membersIncludedOrExcluded));
                    case (true, false):
                        return new EnumeratedValueSet<T, TTC>(true, this.membersIncludedOrExcluded.Except(other.membersIncludedOrExcluded));
                    case (false, false):
                        return new EnumeratedValueSet<T, TTC>(false, this.membersIncludedOrExcluded.Union(other.membersIncludedOrExcluded));
                    case (false, true):
                        return new EnumeratedValueSet<T, TTC>(true, other.membersIncludedOrExcluded.Except(this.membersIncludedOrExcluded));
                }
            }

            IValueSet IValueSet.Intersect(IValueSet other) => Intersect((IValueSet<T>)other);

            public IValueSet<T> Union(IValueSet<T> o)
            {
                if (this == o)
                    return this;
                var other = (EnumeratedValueSet<T, TTC>)o;
                var (larger, smaller) = (this.membersIncludedOrExcluded.Count > other.membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                switch (larger.included, smaller.included)
                {
                    case (false, false):
                        return new EnumeratedValueSet<T, TTC>(false, this.membersIncludedOrExcluded.Intersect(other.membersIncludedOrExcluded));
                    case (false, true):
                        return new EnumeratedValueSet<T, TTC>(false, this.membersIncludedOrExcluded.Except(other.membersIncludedOrExcluded));
                    case (true, true):
                        return new EnumeratedValueSet<T, TTC>(true, this.membersIncludedOrExcluded.Union(other.membersIncludedOrExcluded));
                    case (true, false):
                        return new EnumeratedValueSet<T, TTC>(false, other.membersIncludedOrExcluded.Except(this.membersIncludedOrExcluded));
                }
            }

            IValueSet IValueSet.Union(IValueSet other) => Union((IValueSet<T>)other);

            public override bool Equals(object obj) => obj is EnumeratedValueSet<T, TTC> other &&
                this.included == other.included && this.membersIncludedOrExcluded.SetEquals(other.membersIncludedOrExcluded);

            public override int GetHashCode() => Hash.Combine(this.included.GetHashCode(), this.membersIncludedOrExcluded.GetHashCode());

            public override string ToString() => $"{(this.included ? "" : "~")}{{{string.Join(",", membersIncludedOrExcluded.Select(o => o!.ToString()))}{"}"}";
        }
    }
}
