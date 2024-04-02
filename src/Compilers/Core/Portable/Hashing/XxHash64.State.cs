// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static System.IO.Hashing.XxHashShared;

// Copied from https://github.com/dotnet/runtime/blob/f1d463f46268382a8287d71aea929dadaa5dfef5/src/libraries/System.IO.Hashing/src/System/IO/Hashing/XxHash64.State.cs#L1C13-L1C13
// Remove once we can actually add a reference to System.IO.Hashing v8.0.0

// Implemented from the specification at
// https://github.com/Cyan4973/xxHash/blob/f9155bd4c57e2270a4ffbb176485e5d713de1c9b/doc/xxhash_spec.md

namespace System.IO.Hashing
{
    internal sealed partial class XxHash64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Avalanche(ulong hash)
        {
            hash ^= hash >> 33;
            hash *= Prime64_2;
            hash ^= hash >> 29;
            hash *= Prime64_3;
            hash ^= hash >> 32;
            return hash;
        }

#if false
        private struct State
        {
            private ulong _acc1;
            private ulong _acc2;
            private ulong _acc3;
            private ulong _acc4;
            private readonly ulong _smallAcc;
            private bool _hadFullStripe;

            internal State(ulong seed)
            {
                _acc1 = seed + unchecked(Prime64_1 + Prime64_2);
                _acc2 = seed + Prime64_2;
                _acc3 = seed;
                _acc4 = seed - Prime64_1;

                _smallAcc = seed + Prime64_5;
                _hadFullStripe = false;
            }

            internal void ProcessStripe(ReadOnlySpan<byte> source)
            {
                Debug.Assert(source.Length >= StripeSize);
                source = source.Slice(0, StripeSize);

                _acc1 = ApplyRound(_acc1, source);
                _acc2 = ApplyRound(_acc2, source.Slice(sizeof(ulong)));
                _acc3 = ApplyRound(_acc3, source.Slice(2 * sizeof(ulong)));
                _acc4 = ApplyRound(_acc4, source.Slice(3 * sizeof(ulong)));

                _hadFullStripe = true;
            }

            private static ulong MergeAccumulator(ulong acc, ulong accN)
            {
                acc ^= ApplyRound(0, accN);
                acc *= Prime64_1;
                acc += Prime64_4;

                return acc;
            }

            private readonly ulong Converge()
            {
                ulong acc =
                    BitOperations.RotateLeft(_acc1, 1) +
                    BitOperations.RotateLeft(_acc2, 7) +
                    BitOperations.RotateLeft(_acc3, 12) +
                    BitOperations.RotateLeft(_acc4, 18);

                acc = MergeAccumulator(acc, _acc1);
                acc = MergeAccumulator(acc, _acc2);
                acc = MergeAccumulator(acc, _acc3);
                acc = MergeAccumulator(acc, _acc4);

                return acc;
            }

            private static ulong ApplyRound(ulong acc, ReadOnlySpan<byte> lane)
            {
                return ApplyRound(acc, BinaryPrimitives.ReadUInt64LittleEndian(lane));
            }

            private static ulong ApplyRound(ulong acc, ulong lane)
            {
                acc += lane * Prime64_2;
                acc = BitOperations.RotateLeft(acc, 31);
                acc *= Prime64_1;

                return acc;
            }

            // Inliner may decide to inline this method into HashToUInt64() with help of PGO and
            // can run out of "time budget" producing non-inlined simple calls such as Span.Slice.
            // TODO: Remove NoInlining when https://github.com/dotnet/runtime/issues/85531 is fixed.
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal readonly ulong Complete(long length, ReadOnlySpan<byte> remaining)
            {
                ulong acc = _hadFullStripe ? Converge() : _smallAcc;

                acc += (ulong)length;

                while (remaining.Length >= sizeof(ulong))
                {
                    ulong lane = BinaryPrimitives.ReadUInt64LittleEndian(remaining);
                    acc ^= ApplyRound(0, lane);
                    acc = BitOperations.RotateLeft(acc, 27);
                    acc *= Prime64_1;
                    acc += Prime64_4;

                    remaining = remaining.Slice(sizeof(ulong));
                }

                // Doesn't need to be a while since it can occur at most once.
                if (remaining.Length >= sizeof(uint))
                {
                    ulong lane = BinaryPrimitives.ReadUInt32LittleEndian(remaining);
                    acc ^= lane * Prime64_1;
                    acc = BitOperations.RotateLeft(acc, 23);
                    acc *= Prime64_2;
                    acc += Prime64_3;

                    remaining = remaining.Slice(sizeof(uint));
                }

                for (int i = 0; i < remaining.Length; i++)
                {
                    ulong lane = remaining[i];
                    acc ^= lane * Prime64_5;
                    acc = BitOperations.RotateLeft(acc, 11);
                    acc *= Prime64_1;
                }

                return Avalanche(acc);
            }
        }
#endif
    }
}
