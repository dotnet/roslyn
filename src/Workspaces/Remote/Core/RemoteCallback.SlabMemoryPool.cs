// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Remote
{
    internal readonly partial struct RemoteCallback<T>
        where T : class
    {
        // Copied from: https://github.com/dotnet/aspnetcore/blob/release/2.1/src/Servers/Kestrel/Transport.Abstractions/src/Internal/SlabMemoryPool.cs
        /// <summary>
        /// Used to allocate and distribute re-usable blocks of memory.
        /// </summary>
        private sealed class SlabMemoryPool : MemoryPool<byte>
        {
            /// <summary>
            /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
            /// </summary>
            private const int _blockSize = 4096;

            /// <summary>
            /// Allocating 32 contiguous blocks per slab makes the slab size 128k. This is larger than the 85k size which will place the memory
            /// in the large object heap. This means the GC will not try to relocate this array, so the fact it remains pinned does not negatively
            /// affect memory management's compactification.
            /// </summary>
            private const int _blockCount = 32;

            /// <summary>
            /// Max allocation block size for pooled blocks,
            /// larger values can be leased but they will be disposed after use rather than returned to the pool.
            /// </summary>
            public override int MaxBufferSize { get; } = _blockSize;

            /// <summary>
            /// 4096 * 32 gives you a slabLength of 128k contiguous bytes allocated per slab
            /// </summary>
            private const int _slabLength = _blockSize * _blockCount;

            /// <summary>
            /// Thread-safe collection of blocks which are currently in the pool. A slab will pre-allocate all of the block tracking objects
            /// and add them to this collection. When memory is requested it is taken from here first, and when it is returned it is re-added.
            /// </summary>
            private readonly ConcurrentQueue<MemoryPoolBlock> _blocks = new ConcurrentQueue<MemoryPoolBlock>();

            /// <summary>
            /// Thread-safe collection of slabs which have been allocated by this pool. As long as a slab is in this collection and slab.IsActive,
            /// the blocks will be added to _blocks when returned.
            /// </summary>
            private readonly ConcurrentStack<MemoryPoolSlab> _slabs = new ConcurrentStack<MemoryPoolSlab>();

            /// <summary>
            /// This is part of implementing the IDisposable pattern.
            /// </summary>
            private bool _disposedValue = false; // To detect redundant calls

            /// <summary>
            /// This default value passed in to Rent to use the default value for the pool.
            /// </summary>
            private const int AnySize = -1;

            public override IMemoryOwner<byte> Rent(int size = AnySize)
            {
                if (size > _blockSize)
                    throw new ArgumentOutOfRangeException(nameof(size));

                var block = Lease();
                return block;
            }

            /// <summary>
            /// Called to take a block from the pool.
            /// </summary>
            /// <returns>The block that is reserved for the called. It must be passed to Return when it is no longer being used.</returns>
            private MemoryPoolBlock Lease()
            {
                Debug.Assert(!_disposedValue, "Block being leased from disposed pool!");

                if (_blocks.TryDequeue(out var block))
                {
                    // block successfully taken from the stack - return it
                    return block;
                }

                // no blocks available - grow the pool
                block = AllocateSlab();
                return block;
            }

            /// <summary>
            /// Internal method called when a block is requested and the pool is empty. It allocates one additional slab, creates all of the
            /// block tracking objects, and adds them all to the pool.
            /// </summary>
            private MemoryPoolBlock AllocateSlab()
            {
                var slab = MemoryPoolSlab.Create(_slabLength);
                _slabs.Push(slab);

                var basePtr = slab.NativePointer;
                // Page align the blocks
                var firstOffset = (int)((((ulong)basePtr + (uint)_blockSize - 1) & ~((uint)_blockSize - 1)) - (ulong)basePtr);
                // Ensure page aligned
                Debug.Assert((((ulong)basePtr + (uint)firstOffset) & (uint)(_blockSize - 1)) == 0);

                var blockAllocationLength = ((_slabLength - firstOffset) & ~(_blockSize - 1));
                var offset = firstOffset;
                for (;
                    offset + _blockSize < blockAllocationLength;
                    offset += _blockSize)
                {
                    var block = new MemoryPoolBlock(
                        this,
                        slab,
                        offset,
                        _blockSize);
                    Return(block);
                }

                Debug.Assert(offset + _blockSize - firstOffset == blockAllocationLength);
                // return last block rather than adding to pool
                var newBlock = new MemoryPoolBlock(
                        this,
                        slab,
                        offset,
                        _blockSize);

                return newBlock;
            }

            /// <summary>
            /// Called to return a block to the pool. Once Return has been called the memory no longer belongs to the caller, and
            /// Very Bad Things will happen if the memory is read of modified subsequently. If a caller fails to call Return and the
            /// block tracking object is garbage collected, the block tracking object's finalizer will automatically re-create and return
            /// a new tracking object into the pool. This will only happen if there is a bug in the server, however it is necessary to avoid
            /// leaving "dead zones" in the slab due to lost block tracking objects.
            /// </summary>
            /// <param name="block">The block to return. It must have been acquired by calling Lease on the same memory pool instance.</param>
            internal void Return(MemoryPoolBlock block)
            {
                if (block.Slab != null && block.Slab.IsActive)
                {
                    _blocks.Enqueue(block);
                }
                else
                {
                    GC.SuppressFinalize(block);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    _disposedValue = true;
                    if (disposing)
                    {
                        while (_slabs.TryPop(out var slab))
                        {
                            // dispose managed state (managed objects).
                            slab.Dispose();
                        }
                    }

                    // Discard blocks in pool
                    while (_blocks.TryDequeue(out var block))
                    {
                        GC.SuppressFinalize(block);
                    }

                    // N/A: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // N/A: set large fields to null.
                }
            }
        }
    }
}
