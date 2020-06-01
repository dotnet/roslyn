﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private sealed class NintValueSet : IValueSet<int>, IValueSet
        {
            public readonly static NintValueSet AllValues = new NintValueSet(hasSmall: true, values: NumericValueSet<int, IntTC>.AllValues, hasLarge: true);

            private readonly IValueSet<int> _values;

            /// <summary>
            /// A value of type nint may, in a 64-bit runtime, take on values less than <see cref="System.Int32.MinValue"/>.
            /// A value set representing values of type nint groups them all together, so that it is not possible to
            /// distinguish one such value from another.  The flag <see cref="_hasSmall"/> is true when the set is considered
            /// to contain all values less than <see cref="System.Int32.MinValue"/> (if any).
            /// </summary>
            private readonly bool _hasSmall;

            /// <summary>
            /// A value of type nint may, in a 64-bit runtime, take on values greater than <see cref="System.Int32.MaxValue"/>.
            /// A value set representing values of type nint groups them all together, so that it is not possible to
            /// distinguish one such value from another.  The flag <see cref="_hasLarge"/> is true when the set is considered
            /// to contain all values greater than <see cref="System.Int32.MaxValue"/> (if any).
            /// </summary>
            private readonly bool _hasLarge;

            internal NintValueSet(bool hasSmall, IValueSet<int> values, bool hasLarge)
            {
                _hasSmall = hasSmall;
                _values = values;
                _hasLarge = hasLarge;
            }

            bool IValueSet.IsEmpty => !_hasSmall && !_hasLarge && _values.IsEmpty;

            public bool All(BinaryOperatorKind relation, int value)
            {
                if (_hasLarge && relation switch { LessThan => true, LessThanOrEqual => true, _ => false })
                    return false;
                if (_hasSmall && relation switch { GreaterThan => true, GreaterThanOrEqual => true, _ => false })
                    return false;
                return _values.All(relation, value);
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || All(relation, value.Int32Value);

            public bool Any(BinaryOperatorKind relation, int value)
            {
                if (_hasSmall && relation switch { LessThan => true, LessThanOrEqual => true, _ => false })
                    return true;
                if (_hasLarge && relation switch { GreaterThan => true, GreaterThanOrEqual => true, _ => false })
                    return true;
                return _values.Any(relation, value);
            }

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, value.Int32Value);

            public IValueSet<int> Complement()
            {
                return new NintValueSet(
                    hasSmall: !this._hasSmall,
                    values: this._values.Complement(),
                    hasLarge: !this._hasLarge
                    );
            }

            IValueSet IValueSet.Complement() => this.Complement();

            public IValueSet<int> Intersect(IValueSet<int> o)
            {
                var other = (NintValueSet)o;
                return new NintValueSet(
                    hasSmall: this._hasSmall && other._hasSmall,
                    values: this._values.Intersect(other._values),
                    hasLarge: this._hasLarge && other._hasLarge
                    );
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((NintValueSet)other);

            public IValueSet<int> Union(IValueSet<int> o)
            {
                var other = (NintValueSet)o;
                return new NintValueSet(
                    hasSmall: this._hasSmall || other._hasSmall,
                    values: this._values.Union(other._values),
                    hasLarge: this._hasLarge || other._hasLarge
                    );
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((NintValueSet)other);

            public override bool Equals(object? obj) => obj is NintValueSet other &&
                this._hasSmall == other._hasSmall &&
                this._hasLarge == other._hasLarge &&
                this._values.Equals(other._values);

            public override int GetHashCode() =>
                Hash.Combine(this._hasSmall.GetHashCode(), Hash.Combine(this._hasLarge.GetHashCode(), this._values.GetHashCode()));

            public override string ToString()
            {
                var psb = PooledStringBuilder.GetInstance();
                var builder = psb.Builder;
                if (_hasSmall)
                    builder.Append("Small");
                if (_hasSmall && !_values.IsEmpty)
                    builder.Append(",");
                builder.Append(_values.ToString());
                if (_hasLarge && builder.Length > 0)
                    builder.Append(",");
                if (_hasLarge)
                    builder.Append("Large");
                return psb.ToStringAndFree();
            }
        }
    }
}
