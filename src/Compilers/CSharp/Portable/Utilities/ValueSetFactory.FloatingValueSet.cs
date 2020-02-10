// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Roslyn.Utilities;

#nullable enable

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
        private class FloatingValueSet<TFloating, TFloatingTC> : IValueSet<TFloating> where TFloatingTC : struct, FloatingTC<TFloating>
        {
            private bool _hasNaN, _hasMinusInf, _hasPlusInf;
            private IValueSet<TFloating> _numbers;

            private FloatingValueSet(bool hasNaN, bool hasMinusInf, bool hasPlusInf, IValueSet<TFloating> numbers)
            {
                RoslynDebug.Assert(numbers is NumericValueSet<TFloating, TFloatingTC>);
                (_hasNaN, _hasMinusInf, _hasPlusInf, _numbers) = (hasNaN, hasMinusInf, hasPlusInf, numbers);
            }

            public static readonly IValueSet<TFloating> AllValues = new FloatingValueSet<TFloating, TFloatingTC>(
                hasNaN: true, hasMinusInf: true, hasPlusInf: true, numbers: NumericValueSetFactory<TFloating, TFloatingTC>.Instance.All);

            internal static IValueSet<TFloating> Random(int expectedSize, Random random)
            {
                bool hasNan = random.Next(2) < 1;
                bool hasMinusInf = random.Next(2) < 1;
                bool hasPlusInf = random.Next(2) < 1;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    hasNaN: hasNan, hasMinusInf: hasMinusInf, hasPlusInf: hasPlusInf, numbers: NumericValueSetFactory<TFloating, TFloatingTC>.Instance.Random(expectedSize, random));
            }

            public static readonly IValueSet<TFloating> None = new FloatingValueSet<TFloating, TFloatingTC>(
                hasNaN: false, hasMinusInf: false, hasPlusInf: false, numbers: NumericValueSetFactory<TFloating, TFloatingTC>.Instance.None);

            bool IValueSet.IsEmpty => !_hasNaN && !_hasMinusInf && !_hasPlusInf && _numbers.IsEmpty;

            IValueSetFactory<TFloating> IValueSet<TFloating>.Factory => FloatingValueSetFactory<TFloating, TFloatingTC>.Instance;

            IValueSetFactory IValueSet.Factory => FloatingValueSetFactory<TFloating, TFloatingTC>.Instance;

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
                                hasMinusInf: false,
                                hasPlusInf: false,
                                numbers: NumericValueSetFactory<TFloating, TFloatingTC>.Instance.None
                                );
                        case BinaryOperatorKind.LessThan:
                        case BinaryOperatorKind.GreaterThan:
                            return None;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(relation);
                    }
                }

                return new FloatingValueSet<TFloating, TFloatingTC>(
                    hasNaN: false,
                    hasMinusInf: tc.Related(relation, tc.MinusInf, value),
                    hasPlusInf: tc.Related(relation, tc.PlusInf, value),
                    numbers: NumericValueSetFactory<TFloating, TFloatingTC>.Instance.Related(relation, value)
                    );
            }

            public IValueSet<TFloating> Intersect(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating, TFloatingTC>)o;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    hasNaN: this._hasNaN & other._hasNaN,
                    hasMinusInf: this._hasMinusInf & other._hasMinusInf,
                    hasPlusInf: this._hasPlusInf & other._hasPlusInf,
                    numbers: this._numbers.Intersect(other._numbers));
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Union(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating, TFloatingTC>)o;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    hasNaN: this._hasNaN | other._hasNaN,
                    hasMinusInf: this._hasMinusInf | other._hasMinusInf,
                    hasPlusInf: this._hasPlusInf | other._hasPlusInf,
                    numbers: this._numbers.Union(other._numbers));
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Complement()
            {
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    hasNaN: !this._hasNaN,
                    hasMinusInf: !this._hasMinusInf,
                    hasPlusInf: !this._hasPlusInf,
                    numbers: this._numbers.Complement());
            }

            IValueSet IValueSet.Complement() => this.Complement();

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad || this.Any(relation, default(TFloatingTC).FromConstantValue(value));

            public bool Any(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                return
                    _hasNaN && tc.Related(relation, tc.NaN, value) ||
                    _hasMinusInf && tc.Related(relation, tc.MinusInf, value) ||
                    _hasPlusInf && tc.Related(relation, tc.PlusInf, value) ||
                    _numbers.Any(relation, value);
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TFloatingTC).FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                return
                    (!_hasNaN || tc.Related(relation, tc.NaN, value)) &&
                    (!_hasMinusInf || tc.Related(relation, tc.MinusInf, value)) &&
                    (!_hasPlusInf || tc.Related(relation, tc.PlusInf, value)) &&
                    _numbers.All(relation, value);
            }

            public override int GetHashCode() => this._numbers.GetHashCode();

            public override bool Equals(object obj) => this == obj ||
                obj is FloatingValueSet<TFloating, TFloatingTC> other &&
                this._hasNaN == other._hasNaN &&
                this._hasMinusInf == other._hasMinusInf &&
                this._hasPlusInf == other._hasPlusInf &&
                this._numbers.Equals(other._numbers);

            public override string ToString()
            {
                var b = new StringBuilder();
                if (_hasNaN)
                    b.Append("NaN");
                if (_hasMinusInf)
                    b.Append($"{(b.Length > 0 ? "," : "")}-Inf");
                if (_hasPlusInf)
                    b.Append($"{(b.Length > 0 ? "," : "")}Inf");
                string more = this._numbers.ToString();
                if (b.Length > 1 && more.Length > 1)
                    b.Append(",");
                b.Append(more);
                return b.ToString();
            }
        }
    }
}
