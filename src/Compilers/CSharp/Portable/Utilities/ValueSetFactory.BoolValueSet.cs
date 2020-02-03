// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    using static BinaryOperatorKind;

    internal static partial class ValueSetFactory
    {
        private class BoolValueSet : IValueSet<bool>
        {
            internal static BoolValueSet AllValues = new BoolValueSet(true, true);
            internal static BoolValueSet None = new BoolValueSet(false, false);
            internal static BoolValueSet OnlyTrue = new BoolValueSet(false, true);
            internal static BoolValueSet OnlyFalse = new BoolValueSet(true, false);
            public static BoolValueSet Create(bool hasFalse, bool hasTrue) => (hasFalse, hasTrue) switch
            {
                (false, false) => None,
                (false, true) => OnlyTrue,
                (true, false) => OnlyFalse,
                (true, true) => AllValues,
            };

            private readonly bool _hasFalse, _hasTrue;
            private BoolValueSet(bool hasFalse, bool hasTrue) => (_hasFalse, _hasTrue) = (hasFalse, hasTrue);
            bool IValueSet.IsEmpty => !_hasFalse && !_hasTrue;

            IValueSetFactory<bool> IValueSet<bool>.Factory => BoolValueSetFactory.Instance;

            IValueSetFactory IValueSet.Factory => BoolValueSetFactory.Instance;

            public bool Any(BinaryOperatorKind relation, bool value) => (relation, value) switch
            {
                (Equal, true) => _hasTrue,
                (Equal, false) => _hasFalse,
                (NotEqual, true) => _hasFalse,
                (NotEqual, false) => _hasTrue,
                var _ => throw new ArgumentException("relation"),
            };
            bool IValueSet.Any(BinaryOperatorKind relation, ConstantValue value) => value.IsBad || Any(relation, value.BooleanValue);
            public bool All(BinaryOperatorKind relation, bool value) => (relation, value) switch
            {
                (Equal, true) => !_hasFalse,
                (Equal, false) => !_hasTrue,
                (NotEqual, true) => !_hasTrue,
                (NotEqual, false) => !_hasFalse,
                var _ => throw new ArgumentException("relation"),
            };
            bool IValueSet.All(BinaryOperatorKind relation, ConstantValue value) => !value.IsBad && All(relation, value.BooleanValue);
            public IValueSet<bool> Complement() => Create(!_hasFalse, !_hasTrue);
            IValueSet IValueSet.Complement() => this.Complement();
            public IValueSet<bool> Intersect(IValueSet<bool> other)
            {
                if (this == other)
                    return this;
                BoolValueSet o = (BoolValueSet)other;
                return Create(this._hasFalse & o._hasFalse, this._hasTrue & o._hasTrue);
            }
            public IValueSet Intersect(IValueSet other) => this.Intersect((IValueSet<bool>)other);
            public IValueSet<bool> Union(IValueSet<bool> other)
            {
                if (this == other)
                    return this;
                BoolValueSet o = (BoolValueSet)other;
                return Create(this._hasFalse | o._hasFalse, this._hasTrue | o._hasTrue);
            }
            IValueSet IValueSet.Union(IValueSet other) => this.Union((IValueSet<bool>)other);
            // Since we cache all distinct boolean value sets, we can use reference equality.
            public override bool Equals(object obj) => this == obj;
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
