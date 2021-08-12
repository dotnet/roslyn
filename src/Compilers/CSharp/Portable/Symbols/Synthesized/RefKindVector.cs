// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
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

        private RefKindVector(BitVector bits)
        {
            _bits = bits;
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

        public string ToRefKindString()
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            builder.Append("{");

            int i = 0;
            foreach (int byRefIndex in this.Words())
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.AppendFormat("{0:x8}", byRefIndex);
                i++;
            }

            builder.Append("}");
            Debug.Assert(i > 0);

            return pooledBuilder.ToStringAndFree();
        }

        public static RefKindVector Parse(string refKindString, int capacity)
        {
            int index = 0;
            // To avoid having to map ref kinds again we can just deserialize the BitVector which is what
            // would have produced the string in the method above
            var bitVector = BitVector.Create(capacity * 2);
            foreach (var word in refKindString.Split(','))
            {
                var value = Convert.ToUInt64(word, 16);
                var valueBytes = BitConverter.GetBytes(value);
                var bits = new System.Collections.BitArray(valueBytes);

                // BitVector will happily grow, and GetBytes will always return 64 bytes, so we need to limit
                for (int i = 0; i < bits.Length && index < bitVector.Capacity; i++)
                {
                    bitVector[index++] = bits.Get(i);
                }

                if (index == bitVector.Capacity)
                {
                    break;
                }
            }

            return new RefKindVector(bitVector);
        }
    }
}
