// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal struct RefKindVector : IEquatable<RefKindVector>
    {
        private BitVector _bits;

        internal static RefKindVector Create(int capacity)
        {
            return new RefKindVector(capacity);
        }

        private RefKindVector(int capacity)
        {
            _bits = BitVector.Create(capacity * 2);
        }

        internal bool IsNull => _bits.IsNull;

        internal int Capacity => _bits.Capacity / 2;

        internal IEnumerable<ulong> Words() => _bits.Words();

        internal RefKind this[int index]
        {
            get
            {
                index *= 2;
                return (_bits[index + 1], _bits[index]) switch
                {
                    (false, false) => RefKind.None,
                    (false, true) => RefKind.Ref,
                    (true, false) => RefKind.Out,
                    (true, true) => RefKind.RefReadOnly,
                };
            }
            set
            {
                index *= 2;
                (_bits[index + 1], _bits[index]) = value switch
                {
                    RefKind.None => (false, false),
                    RefKind.Ref => (false, true),
                    RefKind.Out => (true, false),
                    RefKind.RefReadOnly => (true, true),
                    _ => throw ExceptionUtilities.UnexpectedValue(value)
                };
            }
        }

        public bool Equals(RefKindVector other)
        {
            return _bits.Equals(other._bits);
        }

        public override bool Equals(object? obj)
        {
            return obj is RefKindVector other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return _bits.GetHashCode();
        }
    }
}
