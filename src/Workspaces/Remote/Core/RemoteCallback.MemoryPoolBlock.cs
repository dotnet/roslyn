// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.CodeAnalysis.Remote
{
    internal readonly partial struct RemoteCallback<T>
        where T : class
    {
        /// <summary>
        /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
        /// individual blocks are then treated as independent array segments.
        /// </summary>
        private sealed class MemoryPoolBlock : IMemoryOwner<byte>
        {
            private readonly int _offset;
            private readonly int _length;

            /// <summary>
            /// This object cannot be instantiated outside of the static Create method
            /// </summary>
            public MemoryPoolBlock(SlabMemoryPool pool, MemoryPoolSlab slab, int offset, int length)
            {
                _offset = offset;
                _length = length;

                Pool = pool;
                Slab = slab;

                Memory = MemoryMarshal.CreateFromPinnedArray(slab.Array, _offset, _length);
            }

            /// <summary>
            /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
            /// </summary>
            public SlabMemoryPool Pool { get; }

            /// <summary>
            /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
            /// </summary>
            public MemoryPoolSlab Slab { get; }

            public Memory<byte> Memory { get; }

            ~MemoryPoolBlock()
            {
                if (Slab != null && Slab.IsActive)
                {
                    // Need to make a new object because this one is being finalized
                    Pool.Return(new MemoryPoolBlock(Pool, Slab, _offset, _length));
                }
            }

            public void Dispose()
            {
                Pool.Return(this);
            }
        }
    }
}
