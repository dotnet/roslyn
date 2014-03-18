// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal sealed class ByteSequenceComparer : IEqualityComparer<byte[]>, IEqualityComparer<IEnumerable<byte>>, IEqualityComparer<ImmutableArray<byte>>
    {
        internal static readonly ByteSequenceComparer Instance = new ByteSequenceComparer();

        private ByteSequenceComparer()
        {
        }

        public bool Equals(IEnumerable<byte> x, IEnumerable<byte> y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            var ax = x as byte[];
            var ay = y as byte[];
            return (ax != null && ay != null)
                ? Equals(ax, ay)
                : x.SequenceEqual(y);
        }

        public bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            return ValueEquals(x, y);
        }

        internal static bool ValueEquals(ImmutableArray<byte> x, ImmutableArray<byte> y)
        {
            return x.SequenceEqual(y);
        }

        public bool Equals(byte[] x, byte[] y)
        {
            return ValueEquals(x, y);
        }

        internal static bool ValueEquals(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null || x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IEnumerable<byte> x)
        {
            if (x == null)
            {
                return 0;
            }

            var ax = x as byte[];
            if (ax != null)
            {
                return GetHashCode(ax);
            }

            var result = 7;
            foreach (var b in x)
            {
                result = (result << 5) ^ b;
            }

            return result;
        }

        public int GetHashCode(byte[] x)
        {
            return GetValueHashCode(x);
        }

        /// <summary>
        /// Compute the FNV-1a hash code for a sequence of bytes.
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <param name="x">The sequence of bytes</param>
        /// <returns>The FNV-1a hash code for the input sequence</returns>
        /// <exception cref="System.NullReferenceException">The input sequence was null (IsDefault)</exception>
        public int GetHashCode(ImmutableArray<byte> x)
        {
            return Hash.GetFNVHashCode(x);
        }

        // This uses FNV1a as a string hash
        internal static int GetValueHashCode(byte[] x)
        {
            if (x == null)
            {
                return 0;
            }

            return Hash.GetFNVHashCode(x);
        }
    }
}