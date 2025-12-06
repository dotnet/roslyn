// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private sealed class NuintValueSet : IConstantValueSet<uint>, IValueSet
        {
            public static readonly NuintValueSet AllValues = new NuintValueSet(values: NumericValueSet<uint>.AllValues(UIntTC.Instance), hasLarge: true);

            public static readonly NuintValueSet NoValues = new NuintValueSet(values: NumericValueSet<uint>.NoValues(UIntTC.Instance), hasLarge: false);

            private readonly IConstantValueSet<uint> _values;

            /// <summary>
            /// A value of type nuint may, in a 64-bit runtime, take on values greater than <see cref="System.UInt32.MaxValue"/>.
            /// A value set representing values of type nuint groups them all together, so that it is not possible to
            /// distinguish one such value from another.  The flag <see cref="_hasLarge"/> is true when the set is considered
            /// to contain all values greater than <see cref="System.UInt32.MaxValue"/> (if any).
            /// </summary>
            private readonly bool _hasLarge;

            internal NuintValueSet(IConstantValueSet<uint> values, bool hasLarge)
            {
                _values = values;
                _hasLarge = hasLarge;
            }

            public bool IsEmpty => !_hasLarge && _values.IsEmpty;

            ConstantValue? IConstantValueSet.Sample
            {
                get
                {
                    if (IsEmpty)
                        throw new ArgumentException();

                    if (!_values.IsEmpty)
                        return _values.Sample;

                    // We do not support sampling from a nuint value set without a specific value. The caller
                    // must arrange another way to get a sample, since we can return no specific value.  This
                    // occurs when the value set was constructed from a pattern like `> (nuint)uint.MaxValue`.
                    return null;
                }
            }

            public bool All(BinaryOperatorKind relation, uint value)
            {
                if (_hasLarge && relation switch { LessThan => true, LessThanOrEqual => true, _ => false })
                    return false;
                return _values.All(relation, value);
            }

            bool IConstantValueSet.All(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || All(relation, value.UInt32Value);

            public bool Any(BinaryOperatorKind relation, uint value)
            {
                if (_hasLarge && relation switch { GreaterThan => true, GreaterThanOrEqual => true, _ => false })
                    return true;
                return _values.Any(relation, value);
            }

            bool IConstantValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, value.UInt32Value);

            public IConstantValueSet<uint> Complement()
            {
                return new NuintValueSet(
                    values: this._values.Complement(),
                    hasLarge: !this._hasLarge
                    );
            }

            IValueSet IValueSet.Complement() => this.Complement();

            public IConstantValueSet<uint> Intersect(IConstantValueSet<uint> o)
            {
                var other = (NuintValueSet)o;
                return new NuintValueSet(
                    values: this._values.Intersect(other._values),
                    hasLarge: this._hasLarge && other._hasLarge
                    );
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((NuintValueSet)other);

            public IConstantValueSet<uint> Union(IConstantValueSet<uint> o)
            {
                var other = (NuintValueSet)o;
                return new NuintValueSet(
                    values: this._values.Union(other._values),
                    hasLarge: this._hasLarge || other._hasLarge
                    );
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((NuintValueSet)other);

            public override bool Equals(object? obj) => obj is NuintValueSet other &&
                this._hasLarge == other._hasLarge &&
                this._values.Equals(other._values);

            public override int GetHashCode() =>
                Hash.Combine(this._hasLarge.GetHashCode(), this._values.GetHashCode());

            public override string ToString()
            {
                var psb = PooledStringBuilder.GetInstance();
                var builder = psb.Builder;
                builder.Append(_values.ToString());
                if (_hasLarge && builder.Length > 0)
                    builder.Append(',');
                if (_hasLarge)
                    builder.Append("Large");
                return psb.ToStringAndFree();
            }
        }
    }
}
