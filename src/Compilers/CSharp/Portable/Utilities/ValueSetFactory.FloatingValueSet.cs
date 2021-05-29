// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A value set implementation for <see cref="System.Single"/> and <see cref="System.Double"/>.
        /// </summary>
        /// <typeparam name="TFloating">A floating-point type.</typeparam>
        /// <typeparam name="TFloatingTC">A typeclass supporting that floating-point type.</typeparam>
        private sealed class FloatingValueSet<TFloating, TFloatingTC> : IValueSet<TFloating> where TFloatingTC : struct, FloatingTC<TFloating>
        {
            private readonly IValueSet<TFloating> _numbers;
            private readonly bool _hasNaN;

            private FloatingValueSet(IValueSet<TFloating> numbers, bool hasNaN)
            {
                RoslynDebug.Assert(numbers is NumericValueSet<TFloating, TFloatingTC>);
                (_numbers, _hasNaN) = (numbers, hasNaN);
            }

            internal static readonly IValueSet<TFloating> AllValues = new FloatingValueSet<TFloating, TFloatingTC>(
                numbers: NumericValueSet<TFloating, TFloatingTC>.AllValues, hasNaN: true);

            internal static readonly IValueSet<TFloating> NoValues = new FloatingValueSet<TFloating, TFloatingTC>(
                numbers: NumericValueSet<TFloating, TFloatingTC>.NoValues, hasNaN: false);

            internal static IValueSet<TFloating> Random(int expectedSize, Random random)
            {
                bool hasNan = random.NextDouble() < 0.5;
                if (hasNan)
                    expectedSize--;
                if (expectedSize < 1)
                    expectedSize = 2;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    numbers: (IValueSet<TFloating>)NumericValueSetFactory<TFloating, TFloatingTC>.Instance.Random(expectedSize, random), hasNaN: hasNan);
            }

            public bool IsEmpty => !_hasNaN && _numbers.IsEmpty;

            ConstantValue IValueSet.Sample
            {
                get
                {
                    if (IsEmpty)
                        throw new ArgumentException();

                    if (!_numbers.IsEmpty)
                    {
                        var sample = _numbers.Sample;
                        Debug.Assert(sample is { });
                        return sample;
                    }

                    Debug.Assert(_hasNaN);
                    var tc = default(TFloatingTC);
                    return tc.ToConstantValue(tc.NaN);
                }
            }

            public static IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                if (tc.Related(Equal, tc.NaN, value))
                {
                    switch (relation)
                    {
                        case BinaryOperatorKind.Equal:
                        case BinaryOperatorKind.LessThanOrEqual:
                        case BinaryOperatorKind.GreaterThanOrEqual:
                            return new FloatingValueSet<TFloating, TFloatingTC>(
                                hasNaN: true,
                                numbers: NumericValueSet<TFloating, TFloatingTC>.NoValues
                                );
                        case BinaryOperatorKind.LessThan:
                        case BinaryOperatorKind.GreaterThan:
                            return NoValues;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(relation);
                    }
                }
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    numbers: NumericValueSetFactory<TFloating, TFloatingTC>.Instance.Related(relation, value),
                    hasNaN: false
                    );
            }

            public IValueSet<TFloating> Intersect(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating, TFloatingTC>)o;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    numbers: this._numbers.Intersect(other._numbers),
                    hasNaN: this._hasNaN & other._hasNaN);
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Union(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating, TFloatingTC>)o;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    numbers: this._numbers.Union(other._numbers),
                    hasNaN: this._hasNaN | other._hasNaN);
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Complement()
            {
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    numbers: this._numbers.Complement(),
                    hasNaN: !this._hasNaN);
            }

            IValueSet IValueSet.Complement() => this.Complement();

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad || this.Any(relation, default(TFloatingTC).FromConstantValue(value));

            public bool Any(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                return
                    _hasNaN && tc.Related(relation, tc.NaN, value) ||
                    _numbers.Any(relation, value);
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TFloatingTC).FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                return
                    (!_hasNaN || tc.Related(relation, tc.NaN, value)) &&
                    _numbers.All(relation, value);
            }

            public override int GetHashCode() => this._numbers.GetHashCode();

            public override bool Equals(object? obj) => this == obj ||
                obj is FloatingValueSet<TFloating, TFloatingTC> other &&
                this._hasNaN == other._hasNaN &&
                this._numbers.Equals(other._numbers);

            public override string ToString()
            {
                var b = new StringBuilder();
                if (_hasNaN)
                    b.Append("NaN");
                string more = this._numbers.ToString()!;
                if (b.Length > 1 && more.Length > 1)
                    b.Append(",");
                b.Append(more);
                return b.ToString();
            }
        }
    }
}
