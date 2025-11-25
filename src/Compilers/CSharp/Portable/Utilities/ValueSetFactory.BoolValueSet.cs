// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private sealed class BoolValueSet : IConstantValueSet<bool>
        {
            private readonly bool _hasFalse, _hasTrue;

            internal static readonly BoolValueSet AllValues = new BoolValueSet(hasFalse: true, hasTrue: true);
            internal static readonly BoolValueSet None = new BoolValueSet(hasFalse: false, hasTrue: false);
            internal static readonly BoolValueSet OnlyTrue = new BoolValueSet(hasFalse: false, hasTrue: true);
            internal static readonly BoolValueSet OnlyFalse = new BoolValueSet(hasFalse: true, hasTrue: false);

            private BoolValueSet(bool hasFalse, bool hasTrue) => (_hasFalse, _hasTrue) = (hasFalse, hasTrue);

            public static BoolValueSet Create(bool hasFalse, bool hasTrue)
            {
                switch (hasFalse, hasTrue)
                {
                    case (false, false):
                        return None;
                    case (false, true):
                        return OnlyTrue;
                    case (true, false):
                        return OnlyFalse;
                    case (true, true):
                        return AllValues;
                }
            }

            bool IConstantValueSet.IsEmpty => !_hasFalse && !_hasTrue;

            ConstantValue IConstantValueSet.Sample => ConstantValue.Create(_hasTrue ? true : _hasFalse ? false : throw new ArgumentException());

            public bool Any(BinaryOperatorKind relation, bool value)
            {
                switch (relation, value)
                {
                    case (Equal, true):
                        return _hasTrue;
                    case (Equal, false):
                        return _hasFalse;
                    default:
                        return true;
                }
            }

            bool IConstantValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, value.BooleanValue);

            public bool All(BinaryOperatorKind relation, bool value)
            {
                switch (relation, value)
                {
                    case (Equal, true):
                        return !_hasFalse;
                    case (Equal, false):
                        return !_hasTrue;
                    default:
                        return true;
                }
            }

            bool IConstantValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, value.BooleanValue);

            public IConstantValueSet<bool> Complement() => Create(!_hasFalse, !_hasTrue);

            IValueSet IValueSet.Complement() => this.Complement();

            public IConstantValueSet<bool> Intersect(IConstantValueSet<bool> other)
            {
                if (this == other)
                    return this;
                BoolValueSet o = (BoolValueSet)other;
                return Create(hasFalse: this._hasFalse & o._hasFalse, hasTrue: this._hasTrue & o._hasTrue);
            }

            public IValueSet Intersect(IValueSet other) => this.Intersect((IConstantValueSet<bool>)other);

            public IConstantValueSet<bool> Union(IConstantValueSet<bool> other)
            {
                if (this == other)
                    return this;
                BoolValueSet o = (BoolValueSet)other;
                return Create(hasFalse: this._hasFalse | o._hasFalse, hasTrue: this._hasTrue | o._hasTrue);
            }

            IValueSet IValueSet.Union(IValueSet other) => this.Union((IConstantValueSet<bool>)other);

            // Since we cache all distinct boolean value sets, we can use reference equality.
            public override bool Equals(object? obj) => this == obj;

            public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

            public override string ToString() => (_hasFalse, _hasTrue) switch
            {
                (false, false) => "{}",
                (true, false) => "{false}",
                (false, true) => "{true}",
                (true, true) => "{false,true}",
            };
        }
    }
}
