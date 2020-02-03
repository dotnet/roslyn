// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private class FloatingValueSet<TFloating, TFloatingTC> : IValueSet<TFloating> where TFloatingTC : struct, FloatingTC<TFloating>
        {
            private bool _hasNaN, _hasMinusInf, _hasPlusInf;
            private IValueSet<TFloating> _numbers;
            private FloatingValueSet(bool hasNaN, bool hasMinusInf, bool hasPlusInf, IValueSet<TFloating> numbers)
            {
                RoslynDebug.Assert(numbers is NumericValueSet<TFloating, TFloatingTC>);
                (_hasNaN, _hasMinusInf, _hasPlusInf, _numbers) = (hasNaN, hasMinusInf, hasPlusInf, numbers);
            }
            public static readonly IValueSet<TFloating> AllValues = new FloatingValueSet<TFloating, TFloatingTC>(true, true, true, new NumericValueSet<TFloating, TFloatingTC>(Interval.Included.Instance));
            public static readonly IValueSet<TFloating> None = new FloatingValueSet<TFloating, TFloatingTC>(false, false, false, new NumericValueSet<TFloating, TFloatingTC>(Interval.Excluded.Instance));

            bool IValueSet.IsEmpty => !_hasNaN && !_hasMinusInf && !_hasPlusInf && _numbers.IsEmpty;
            IValueSetFactory<TFloating> IValueSet<TFloating>.Factory => FloatingValueSetFactory<TFloating, TFloatingTC>.Instance;

            IValueSetFactory IValueSet.Factory => FloatingValueSetFactory<TFloating, TFloatingTC>.Instance;

            public static IValueSet<TFloating> Related(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                if (tc.Related(Equal, tc.NaN, value))
                {
                    return new FloatingValueSet<TFloating, TFloatingTC>(
                        true,
                        false,
                        false,
                        NumericValueSetFactory<TFloating, TFloatingTC>.Instance.None
                        );
                }

                return new FloatingValueSet<TFloating, TFloatingTC>(
                    false,
                    tc.Related(relation, tc.MinusInf, value),
                    tc.Related(relation, tc.PlusInf, value),
                    NumericValueSetFactory<TFloating, TFloatingTC>.Instance.Related(relation, value)
                    );
            }

            public IValueSet<TFloating> Intersect(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating, TFloatingTC>)o;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    this._hasNaN & other._hasNaN,
                    this._hasMinusInf & other._hasMinusInf,
                    this._hasPlusInf & other._hasPlusInf,
                    this._numbers.Intersect(other._numbers));
            }
            IValueSet IValueSet.Intersect(IValueSet other) => this.Intersect((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Union(IValueSet<TFloating> o)
            {
                if (this == o)
                    return this;
                var other = (FloatingValueSet<TFloating, TFloatingTC>)o;
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    this._hasNaN | other._hasNaN,
                    this._hasMinusInf | other._hasMinusInf,
                    this._hasPlusInf | other._hasPlusInf,
                    this._numbers.Union(other._numbers));
            }
            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<TFloating>)other);

            public IValueSet<TFloating> Complement()
            {
                return new FloatingValueSet<TFloating, TFloatingTC>(
                    !this._hasNaN,
                    !this._hasMinusInf,
                    !this._hasPlusInf,
                    this._numbers.Complement());
            }
            IValueSet IValueSet.Complement() => this.Complement();

            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || this.Any(relation, default(TFloatingTC).FromConstantValue(value));
            public bool Any(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                return
                    _hasNaN && tc.Related(relation, value, tc.NaN) ||
                    _hasMinusInf && tc.Related(relation, value, tc.MinusInf) ||
                    _hasPlusInf && tc.Related(relation, value, tc.PlusInf) ||
                    _numbers.Any(relation, value);
            }

            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, default(TFloatingTC).FromConstantValue(value));
            public bool All(BinaryOperatorKind relation, TFloating value)
            {
                TFloatingTC tc = default;
                return
                    (!_hasNaN || tc.Related(relation, value, tc.NaN)) &&
                    (!_hasMinusInf || tc.Related(relation, value, tc.MinusInf)) &&
                    (!_hasPlusInf || tc.Related(relation, value, tc.PlusInf)) &&
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
