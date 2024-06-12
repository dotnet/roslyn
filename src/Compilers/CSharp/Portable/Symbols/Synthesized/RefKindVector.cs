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
        private const int BitsPerRefKind = 3;
        private BitVector _bits;

        internal static RefKindVector Create(int capacity)
        {
            return new RefKindVector(capacity);
        }

        private RefKindVector(int capacity)
        {
            _bits = BitVector.Create(capacity * BitsPerRefKind);
        }

        private RefKindVector(BitVector bits)
        {
            _bits = bits;
        }

        internal bool IsNull => _bits.IsNull;

        internal int Capacity => _bits.Capacity / BitsPerRefKind;

        internal IEnumerable<ulong> Words() => _bits.Words();

        internal RefKind this[int index]
        {
            get
            {
                index *= BitsPerRefKind;
                return (_bits[index + 2], _bits[index + 1], _bits[index]) switch
                {
                    (false, false, false) => RefKind.None,
                    (false, false, true) => RefKind.Ref,
                    (false, true, false) => RefKind.Out,
                    (false, true, true) => RefKind.RefReadOnly,
                    (true, false, false) => RefKind.RefReadOnlyParameter,
                    var bits => throw ExceptionUtilities.UnexpectedValue(bits)
                };
            }
            set
            {
                index *= BitsPerRefKind;
                (_bits[index + 2], _bits[index + 1], _bits[index]) = value switch
                {
                    RefKind.None => (false, false, false),
                    RefKind.Ref => (false, false, true),
                    RefKind.Out => (false, true, false),
                    RefKind.RefReadOnly => (false, true, true),
                    RefKind.RefReadOnlyParameter => (true, false, false),
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
            foreach (var word in this.Words())
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.AppendFormat("{0:x8}", word);
                i++;
            }

            builder.Append("}");
            Debug.Assert(i > 0);

            return pooledBuilder.ToStringAndFree();
        }

        public static bool TryParse(string refKindString, int capacity, out RefKindVector result)
        {
            ulong? firstWord = null;
            ArrayBuilder<ulong>? otherWords = null;
            foreach (var word in refKindString.Split(','))
            {
                ulong value;
                try
                {
                    value = Convert.ToUInt64(word, 16);
                }
                catch (Exception)
                {
                    result = default;
                    return false;
                }

                if (firstWord is null)
                {
                    firstWord = value;
                }
                else
                {
                    otherWords ??= ArrayBuilder<ulong>.GetInstance();
                    otherWords.Add(value);
                }
            }

            Debug.Assert(firstWord is not null);

            var bitVector = BitVector.FromWords(firstWord.Value, otherWords?.ToArrayAndFree() ?? Array.Empty<ulong>(), capacity * BitsPerRefKind);
            result = new RefKindVector(bitVector);
            return true;
        }
    }
}
