// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            internal static EnumeratedValueSet<T, TTC> Excluding(T value) => new EnumeratedValueSet<T, TTC>(included: true, ImmutableHashSet<T>.Empty.Add(value));

            IValueSetFactory<T> IValueSet<T>.Factory => EnumeratedValueSetFactory<T, TTC>.Instance;
            IValueSetFactory IValueSet.Factory => EnumeratedValueSetFactory<T, TTC>.Instance;
            bool IValueSet.IsEmpty => included && membersIncludedOrExcluded.IsEmpty;

            public bool Any(BinaryOperatorKind relation, T value) => relation switch
            {
                BinaryOperatorKind.Equal => included == membersIncludedOrExcluded.Contains(value),
                BinaryOperatorKind.NotEqual =>
                    !included || membersIncludedOrExcluded.Count > 1 ||
                    membersIncludedOrExcluded.Count == 1 && !membersIncludedOrExcluded.Contains(value),
                _ => true, // supported for error recovery
            };
            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, default(TTC).FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, T value) => relation switch
            {
                BinaryOperatorKind.Equal => included && membersIncludedOrExcluded.Count switch { 0 => true, 1 => membersIncludedOrExcluded.Contains(value), _ => false },
                BinaryOperatorKind.NotEqual => included != membersIncludedOrExcluded.Contains(value),
                _ => false, // supported for error recovery
            };
            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TTC).FromConstantValue(value));

            IValueSet<T> IValueSet<T>.Complement() => new EnumeratedValueSet<T, TTC>(!included, membersIncludedOrExcluded);
            IValueSet IValueSet.Complement() => new EnumeratedValueSet<T, TTC>(!included, membersIncludedOrExcluded);

            public IValueSet<T> Intersect(IValueSet<T> o)
            {
                var other = (EnumeratedValueSet<T, TTC>)o;
                var (larger, smaller) = (this.membersIncludedOrExcluded.Count > other.membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                return (larger.included, smaller.included) switch
                {
                    (true, true) => new EnumeratedValueSet<T, TTC>(true, this.membersIncludedOrExcluded.Intersect(other.membersIncludedOrExcluded)),
                    (true, false) => new EnumeratedValueSet<T, TTC>(true, this.membersIncludedOrExcluded.Except(other.membersIncludedOrExcluded)),
                    (false, false) => new EnumeratedValueSet<T, TTC>(false, this.membersIncludedOrExcluded.Union(other.membersIncludedOrExcluded)),
                    (false, true) => new EnumeratedValueSet<T, TTC>(true, other.membersIncludedOrExcluded.Except(this.membersIncludedOrExcluded)),
                };
            }
            IValueSet IValueSet.Intersect(IValueSet other) => Intersect((IValueSet<T>)other);

            public IValueSet<T> Union(IValueSet<T> o)
            {
                var other = (EnumeratedValueSet<T, TTC>)o;
                var (larger, smaller) = (this.membersIncludedOrExcluded.Count > other.membersIncludedOrExcluded.Count) ? (this, other) : (other, this);
                return (larger.included, smaller.included) switch
                {
                    (false, false) => new EnumeratedValueSet<T, TTC>(false, this.membersIncludedOrExcluded.Intersect(other.membersIncludedOrExcluded)),
                    (false, true) => new EnumeratedValueSet<T, TTC>(false, this.membersIncludedOrExcluded.Except(other.membersIncludedOrExcluded)),
                    (true, true) => new EnumeratedValueSet<T, TTC>(true, this.membersIncludedOrExcluded.Union(other.membersIncludedOrExcluded)),
                    (true, false) => new EnumeratedValueSet<T, TTC>(false, other.membersIncludedOrExcluded.Except(this.membersIncludedOrExcluded)),
                };
            }
            IValueSet IValueSet.Union(IValueSet other) => Union((IValueSet<T>)other);

            public override bool Equals(object obj) => obj is EnumeratedValueSet<T, TTC> other &&
                this.included == other.included && this.membersIncludedOrExcluded.Equals(other.membersIncludedOrExcluded);
            public override int GetHashCode() => Hash.Combine(this.included.GetHashCode(), this.membersIncludedOrExcluded.GetHashCode());
            public override string ToString() => $"{(this.included ? "" : "~")}{{{string.Join(",", membersIncludedOrExcluded.Select(o => o!.ToString()))}{"}"}";
        }
    }
}
