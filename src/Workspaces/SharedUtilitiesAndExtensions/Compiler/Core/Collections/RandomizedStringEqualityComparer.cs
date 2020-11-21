// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// A randomized <see cref="EqualityComparer{String}"/> which uses a different seed on each
    /// construction as a general good hygiene + defense-in-depth mechanism. This implementation
    /// *does not* need to stay in sync with <see cref="string.GetHashCode()"/>, which for stability
    /// is required to use an app-global seed.
    /// </summary>
    internal abstract class RandomizedStringEqualityComparer : EqualityComparer<string?>, IInternalStringEqualityComparer
    {
        private readonly MarvinSeed _seed;
        private readonly IEqualityComparer<string?> _underlyingComparer;

        private RandomizedStringEqualityComparer(IEqualityComparer<string?> underlyingComparer)
        {
            _underlyingComparer = underlyingComparer;

            var guidAccessor = new ObjectWriter.GuidAccessor() { Guid = Guid.NewGuid() };
            _seed.p0 = (uint)guidAccessor.High64;
            _seed.p1 = (uint)guidAccessor.Low64;
        }

        internal static RandomizedStringEqualityComparer Create(IEqualityComparer<string?> underlyingComparer, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                return new OrdinalComparer(underlyingComparer);
            }
            else
            {
                return new OrdinalIgnoreCaseComparer(underlyingComparer);
            }
        }

        public IEqualityComparer<string?> GetUnderlyingEqualityComparer() => _underlyingComparer;

        private struct MarvinSeed
        {
            internal uint p0;
            internal uint p1;
        }

        private sealed class OrdinalComparer : RandomizedStringEqualityComparer
        {
            internal OrdinalComparer(IEqualityComparer<string?> wrappedComparer)
                : base(wrappedComparer)
            {
            }

            public override bool Equals(string? x, string? y) => string.Equals(x, y);

            public override int GetHashCode(string? obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                // The Ordinal version of Marvin32 operates over bytes.
                // The multiplication from # chars -> # bytes will never integer overflow.
                return Marvin.ComputeHash32(
                    ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(obj.AsSpan())),
                    (uint)obj.Length * 2,
                    _seed.p0, _seed.p1);
            }
        }

        private sealed class OrdinalIgnoreCaseComparer : RandomizedStringEqualityComparer
        {
            internal OrdinalIgnoreCaseComparer(IEqualityComparer<string?> wrappedComparer)
                : base(wrappedComparer)
            {
            }

            public override bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

            public override int GetHashCode(string? obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                // The Ordinal version of Marvin32 operates over bytes, so convert
                // char count -> byte count. Guaranteed not to integer overflow.
                return Marvin.ComputeHash32(
                    ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(obj.AsSpan())),
                    (uint)obj.Length * sizeof(char),
                    _seed.p0, _seed.p1);
            }
        }

        private sealed class RandomizedOrdinalIgnoreCaseComparer : RandomizedStringEqualityComparer
        {
            internal RandomizedOrdinalIgnoreCaseComparer(IEqualityComparer<string?> underlyingComparer)
                : base(underlyingComparer)
            {
            }

            public override bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

            public override int GetHashCode(string? obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                // The OrdinalIgnoreCase version of Marvin32 operates over chars,
                // so pass in the char count directly.
                return Marvin.ComputeHash32OrdinalIgnoreCase(
                    obj.AsSpan(),
                    _seed.p0, _seed.p1);
            }
        }
    }
}
