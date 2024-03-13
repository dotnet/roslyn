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
        private sealed class FloatingValueSet<TFloating> : IValueSet<TFloating>
        {
            private readonly IValueSet<TFloating> _numbers;
            private readonly bool _hasNaN;
            private readonly FloatingTC<TFloating> _tc;

            private FloatingValueSet(IValueSet<TFloating> numbers, bool hasNaN, FloatingTC<TFloating> tc)
            {
                RoslynDebug.Assert(numbers is NumericValueSet<TFloating>);
                (_numbers, _hasNaN, _tc) = (numbers, hasNaN, tc);
            }

            internal static IValueSet<TFloating> AllValues(FloatingTC<TFloating> tc) => new FloatingValueSet<TFloating>(
                numbers: NumericValueSet<TFloating>.AllValues(tc), hasNaN: true, tc);

            internal static IValueSet<TFloating> NoValues(FloatingTC<TFloating> tc) => new FloatingValueSet<TFloating>(
                numbers: NumericValueSet<TFloating>.NoValues(tc), hasNaN: false, tc);

            internal static IValueSet<TFloating> Random(int expectedSize, Random random, FloatingTC<TFloating> tc)
            {
                bool hasNan = random.NextDouble() < 0.5;
                if (hasNan)
                    expectedSize--;
                if (expectedSize < 1)
                    expectedSize = 2;
                return new FloatingValueSet<TFloating>(
                    numbers: (IValueSet<TFloating>)new NumericValueSetFactory<TFloating>(tc).Random(expectedSize, random), hasNaN: hasNan, tc);
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
                    return _tc.ToConstantValue(_tc.NaN);
                }
            }

            public static IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value, FloatingTC<TFloating> tc)
            {
                if (tc.Related(Equal, tc.NaN, value))
                {
                    switch (relation)
                    {
                        case BinaryOperatorKind.Equal:
                        case BinaryOperatorKind.LessThanOrEqual:
                        case BinaryOperatorKind.GreaterThanOrEqual:
                            return new FloatingValueSet<TFloating>(
                                hasNaN: true,
                                numbers: NumericValueSet<TFloating>.NoValues(tc),
                                tc: tc
                                );
                        case BinaryOperatorKind.LessThan:
                        case BinaryOperatorKind.GreaterThan:
                            return NoValues(tc);
                        default:
                            throw ExceptionUtilities.UnexpectedValue(relation);
                    }
                }
                return new FloatingValueSet<TFloating>(
                    numbers: new NumericValueSetFactory<TFloating>(tc).Related(relation, value),
                    hasNaN: false,
                    tc: tc
                    );
            }

            public IValueSet<TFloating> Intersect(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating>)o;
                Debug.Assert(object.ReferenceEquals(this._tc, other._tc));

                return new FloatingValueSet<TFloating>(
                    numbers: this._numbers.Intersect(other._numbers),
                    hasNaN: this._hasNaN & other._hasNaN,
                    _tc);
            }

            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Union(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating>)o;
                Debug.Assert(object.ReferenceEquals(this._tc, other._tc));

                return new FloatingValueSet<TFloating>(
                    numbers: this._numbers.Union(other._numbers),
                    hasNaN: this._hasNaN | other._hasNaN,
                    _tc);
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Complement()
            {
                return new FloatingValueSet<TFloating>(
                    numbers: this._numbers.Complement(),
                    hasNaN: !this._hasNaN,
                    _tc);
            }

            IValueSet IValueSet.Complement() => this.Complement();

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) =>
                value.IsBad || this.Any(relation, _tc.FromConstantValue(value));

            public bool Any(BinaryOperatorKind relation, TFloating value)
            {
                return
                    _hasNaN && _tc.Related(relation, _tc.NaN, value) ||
                    _numbers.Any(relation, value);
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, _tc.FromConstantValue(value));

            public bool All(BinaryOperatorKind relation, TFloating value)
            {
                return
                    (!_hasNaN || _tc.Related(relation, _tc.NaN, value)) &&
                    _numbers.All(relation, value);
            }

            public override int GetHashCode() => this._numbers.GetHashCode();

            public override bool Equals(object? obj) => this == obj ||
                obj is FloatingValueSet<TFloating> other &&
                this._hasNaN == other._hasNaN &&
                this._numbers.Equals(other._numbers);

            public override string ToString()
            {
                var b = new StringBuilder();
                if (_hasNaN)
                    b.Append("NaN");
                string more = this._numbers.ToString()!;
                if (b.Length > 1 && more.Length > 1)
                    b.Append(',');
                b.Append(more);
                return b.ToString();
            }
        }
    }
}
