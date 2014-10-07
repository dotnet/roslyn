// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class TemporaryStorageServiceFactory
    {
        /// <summary>
        /// Rather than creating a separate MemoryMappedFile for every string/stream we need to persist,
        /// use several large MemoryMappedFile 'arenas' that contain multiple buffers each.
        /// 
        /// Group buffers by size to simplify fragmentation and complexity of this memory management code.
        /// Currently, anything larger than 256KB will get its own MemoryMappedFile.
        /// 
        /// When opening Roslyn.sln and doing a full initialization through Solution Navigator, we get
        /// 8000+ requests.  Since the minimum OS allocation for a memory mapped file is rounded up to 
        /// 64KB, this would map ~500MB alone, not counting the additional space needed for files > 64KB.
        /// Using the strategy below for the same scenario, we create only 90 4MB arenas which hold all but
        /// 104 buffers that are larger than 256KB.  So we avoid creating ~7500 MemoryMappedFile objects
        /// and save roughly 400MB of fragmentation when compared to the simple one MemoryMappedFile per
        /// request approach.
        /// 
        /// The MemoryMappedFileManager, MemoryMappedFileArena, and MemoryMappedInfo classes all
        /// have very tight coupling in the current implementation.
        /// </summary>
        internal class MemoryMappedFileManager
        {
            private const int SmallFileMaxBytes = 1024 * 2;
            private const int MediumFileMaxBytes = 1024 * 8;
            private const int LargeFileMaxBytes = 1024 * 32;
            private const int HugeFileMaxBytes = 1024 * 256;

            private readonly List<MemoryMappedFileArena> smallFileStorage = new List<MemoryMappedFileArena>();
            private readonly List<MemoryMappedFileArena> mediumFileStorage = new List<MemoryMappedFileArena>();
            private readonly List<MemoryMappedFileArena> largeFileStorage = new List<MemoryMappedFileArena>();
            private readonly List<MemoryMappedFileArena> hugeFileStorage = new List<MemoryMappedFileArena>();

            // Ugh.  We need to keep a strong reference to our allocated arenas.  Otherwise, when we remove instances
            // from the storage lists above, the only references left are MemoryMappedInfo instances.  If 
            // these all happen to end up on the finalizer thread, the underlying MemoryMappedFile SafeHandle will also
            // be finalized.  This invalidates the handle and when we try to reuse this arena by putting it back on a 
            // storage list, we'll hit an exception trying to access an invalid handle.
            private readonly List<MemoryMappedFileArena> allocatedArenas = new List<MemoryMappedFileArena>();

            public MemoryMappedFileManager()
            {
                Contract.Assert(MemoryMappedFileArena.MemoryMappedFileArenaSize % SmallFileMaxBytes == 0);
                Contract.Assert(MemoryMappedFileArena.MemoryMappedFileArenaSize % MediumFileMaxBytes == 0);
                Contract.Assert(MemoryMappedFileArena.MemoryMappedFileArenaSize % LargeFileMaxBytes == 0);
                Contract.Assert(MemoryMappedFileArena.MemoryMappedFileArenaSize % HugeFileMaxBytes == 0);
            }

            public MemoryMappedInfo CreateViewInfo(long size)
            {
                List<MemoryMappedFileArena> storage = null;
                int allocationSize;

                if (size <= SmallFileMaxBytes)
                {
                    storage = smallFileStorage;
                    allocationSize = SmallFileMaxBytes;
                }
                else if (size <= MediumFileMaxBytes)
                {
                    storage = mediumFileStorage;
                    allocationSize = MediumFileMaxBytes;
                }
                else if (size <= LargeFileMaxBytes)
                {
                    storage = largeFileStorage;
                    allocationSize = LargeFileMaxBytes;
                }
                else if (size <= HugeFileMaxBytes)
                {
                    storage = hugeFileStorage;
                    allocationSize = HugeFileMaxBytes;
                }
                else
                {
                    // The requested size is larger than HugeFileMaxBytes, give it its own MemoryMappedFile
                    return new MemoryMappedInfo(MemoryMappedFile.CreateNew(CreateUniqueName(size), size), size);
                }

                // Lock this list since we may be inserting a new element.  MemoryMappedFileArena may also
                // add/remove items from the list.  To avoid deadlocks, lock on this List<MemoryMappedFileArena>
                // first, then MemoryMappedFileArena.gate, and lock allocatedArenas last of all.
                lock (storage)
                {
                    if (storage.Count == 0)
                    {
                        var arena = new MemoryMappedFileArena(this, storage, allocationSize);
                        storage.Add(arena);
                        lock (allocatedArenas)
                        {
                            allocatedArenas.Add(arena);
                        }
                    }

                    return storage[0].CreateMemoryMappedViewInfo(size);
                }
            }

            public static string CreateUniqueName(long size)
            {
                return "Roslyn Temp Storage " + size.ToString() + " " + Guid.NewGuid().ToString("N");
            }

            public void FreeArena(MemoryMappedFileArena memoryMappedFileArena)
            {
                lock (allocatedArenas)
                {
                    allocatedArenas.Remove(memoryMappedFileArena);
                }
            }
        }

        internal sealed class MemoryMappedInfo : IDisposable
        {
            private readonly MemoryMappedFile memoryMappedFile;
            private readonly long offset;
            private readonly long size;
            private readonly MemoryMappedFileArena containingArena;

            /// <summary>
            /// ref count of stream given out
            /// </summary>
            private int streamCount;

            /// <summary>
            /// actual memory accessor that owns the VM
            /// </summary>
            private MemoryMappedViewAccessor accessor;

            public MemoryMappedInfo(MemoryMappedFile memoryMappedFile, long size) : this(memoryMappedFile, 0, size, null) { }

            public MemoryMappedInfo(MemoryMappedFile memoryMappedFile, long offset, long size, MemoryMappedFileArena containingArena)
            {
                this.memoryMappedFile = memoryMappedFile;
                this.offset = offset;
                this.size = size;
                this.containingArena = containingArena;

                this.streamCount = 0;
                this.accessor = null;
            }

            /// <summary>
            /// Caller is responsible for disposing the returned stream.
            /// multiple call of this will not increase VM.
            /// </summary>
            public Stream CreateReadableStream()
            {
                // CreateViewStream is not guaranteed to be thread-safe
                lock (this.memoryMappedFile)
                {
                    if (this.streamCount == 0)
                    {
                        this.accessor = memoryMappedFile.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read);
                    }

                    this.streamCount++;
                    return new SharedReadableStream(this, this.accessor, this.size);
                }
            }

            /// <summary>
            /// Caller is responsible for disposing the returned stream.
            /// multiple call of this will increase VM.
            /// </summary>
            public Stream CreateWritableStream()
            {
                // CreateViewStream is not guaranteed to be thread-safe
                lock (this.memoryMappedFile)
                {
                    return memoryMappedFile.CreateViewStream(offset, size, MemoryMappedFileAccess.Write);
                }
            }

            private void StreamDisposed()
            {
                lock (this.memoryMappedFile)
                {
                    this.streamCount--;
                    if (this.streamCount == 0 && this.accessor != null)
                    {
                        this.accessor.Dispose();
                        this.accessor = null;
                    }
                }
            }

            ~MemoryMappedInfo()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (this.accessor != null)
                {
                    // dispose accessor it owns.
                    // if someone explicitly called Dispose when streams given out are not
                    // disposed yet, the accessor each stream has will simply stop working.
                    //
                    // it is caller's responsibility to make sure all streams it got from
                    // the temporary storage are disposed before calling dispose on the stroage.
                    //
                    // otherwise, finalizer will take care of disposing stuff as we used to be.
                    this.accessor.Dispose();
                    this.accessor = null;
                }

                // Dispose the memoryMappedFile if we own it, otherwise 
                // notify our containingArena that this offset is available
                // for someone else 
                if (this.containingArena == null)
                {
                    this.memoryMappedFile.Dispose();
                }
                else
                {
                    this.containingArena.FreeSegment(this.offset);
                }
            }

            private class SharedReadableStream : Stream, ISupportDirectMemoryAccess
            {
                private readonly MemoryMappedViewAccessor accessor;
                private readonly long length;

                private MemoryMappedInfo owner;
                private IntPtr lazyPointer;
                private long position;

                public SharedReadableStream(MemoryMappedInfo owner, MemoryMappedViewAccessor accessor, long length)
                {
                    this.owner = owner;
                    this.accessor = accessor;
                    this.length = length;

                    this.lazyPointer = IntPtr.Zero;
                    this.position = 0;
                }

                ~SharedReadableStream()
                {
                    // we don't have control on stream we give out to others such as
                    // compiler (ImageOnlyMetdataReferece), make sure we dispose resource 
                    // at the end if Disposed is not called explicitly.
                    Dispose(false);
                }

                public override bool CanRead
                {
                    get
                    {
                        return this.accessor.CanRead;
                    }
                }

                public override bool CanSeek
                {
                    get
                    {
                        return true;
                    }
                }

                public override bool CanWrite
                {
                    get
                    {
                        return false;
                    }
                }

                public override long Length
                {
                    get
                    {
                        return this.length;
                    }
                }

                public override long Position
                {
                    get
                    {
                        return this.position;
                    }

                    set
                    {
                        if (value < 0 || value >= length)
                        {
                            throw new ArgumentOutOfRangeException("value");
                        }

                        this.position = value;
                    }
                }

                public override int ReadByte()
                {
                    if (position >= length)
                    {
                        return -1;
                    }

                    var result = accessor.ReadByte(position);
                    position++;

                    return result;
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (position >= length)
                    {
                        return 0;
                    }

                    var adjustedCount = Math.Min((long)count, length - position);
                    var result = accessor.ReadArray(position, buffer, offset, (int)adjustedCount);

                    position += result;
                    return result;
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    long target;
                    try
                    {
                        switch (origin)
                        {
                            case SeekOrigin.Begin:
                                target = offset;
                                break;

                            case SeekOrigin.Current:
                                target = checked(offset + position);
                                break;

                            case SeekOrigin.End:
                                target = checked(offset + length);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException("origin");
                        }
                    }
                    catch (OverflowException)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }

                    if (target < 0 || target >= length)
                    {
                        throw new ArgumentOutOfRangeException("offset");
                    }

                    position = target;
                    return target;
                }

                public override void Flush()
                {
                    throw new NotSupportedException();
                }

                public override void SetLength(long value)
                {
                    throw new NotSupportedException();
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new NotSupportedException();
                }

                protected override void Dispose(bool disposing)
                {
                    base.Dispose(disposing);

                    if (this.lazyPointer != IntPtr.Zero)
                    {
                        this.accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        this.lazyPointer = IntPtr.Zero;
                    }

                    if (this.owner != null)
                    {
                        this.owner.StreamDisposed();
                        this.owner = null;
                    }
                }

                /// <summary>
                /// get underlying native memory directly
                /// </summary>
                public IntPtr GetPointer()
                {
                    // if we already have pointer, just return it
                    if (this.lazyPointer == IntPtr.Zero)
                    {
                        this.lazyPointer = AcquirePointer();
                    }

                    return this.lazyPointer;
                }

                private unsafe IntPtr AcquirePointer()
                {
                    byte* ptr = null;
                    this.accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    ptr += GetPrivateOffset(this.accessor);

                    return (IntPtr)ptr;
                }

                // this is a copy from compiler's MemoryMappedFileBlock. see MemoryMappedFileBlock for more information on why this is needed.
                private static long GetPrivateOffset(MemoryMappedViewAccessor stream)
                {
                    System.Reflection.FieldInfo unmanagedMemoryStreamOffset = typeof(MemoryMappedViewAccessor).GetField("m_view", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField);
                    object memoryMappedView = unmanagedMemoryStreamOffset.GetValue(stream);
                    System.Reflection.PropertyInfo memoryMappedViewPointerOffset = memoryMappedView.GetType().GetProperty("PointerOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty);
                    return (long)memoryMappedViewPointerOffset.GetValue(memoryMappedView);
                }
            }
        }

        internal class MemoryMappedFileArena
        {
            // Use a 4 MB default arena size, we'll only have a fraction of that
            // mapped into our address space at any given time.
            public const int MemoryMappedFileArenaSize = 4 * 1024 * 1024;

            private readonly MemoryMappedFileManager manager;
            private readonly List<MemoryMappedFileArena> containingList;
            private readonly MemoryMappedFile memoryMappedFile;

            // The byte offsets into memoryMappedFile that are available for allocations
            private readonly Stack<long> freeSegmentOffsets;
            private readonly int segmentCount;
            private readonly object gate = new object();

            public MemoryMappedFileArena(MemoryMappedFileManager manager, List<MemoryMappedFileArena> containingList, int allocationSize)
            {
                Contract.Assert(containingList.Count == 0, "should only create a new arena when the containing list is empty");
                this.manager = manager;
                this.containingList = containingList;
                this.memoryMappedFile = MemoryMappedFile.CreateNew(MemoryMappedFileManager.CreateUniqueName(allocationSize), MemoryMappedFileArenaSize, MemoryMappedFileAccess.ReadWrite);
                this.freeSegmentOffsets = new Stack<long>(Enumerable.Range(0, MemoryMappedFileArenaSize / allocationSize).Select(x => (long)x * allocationSize));
                this.segmentCount = freeSegmentOffsets.Count;
            }

            public void FreeSegment(long offset)
            {
                int count;
                lock (gate)
                {
                    freeSegmentOffsets.Push(offset);
                    count = freeSegmentOffsets.Count;
                }

                if (count == 1)
                {
                    // This arena has room for allocations now, so add it back to the list.
                    lock (containingList)
                    {
                        containingList.Add(this);
                    }
                }
                else if (count == segmentCount)
                {
                    // this arena is no longer in use.
                    lock (containingList)
                    {
                        lock (gate)
                        {
                            // re-check to make sure no one allocated after we released the lock
                            if (freeSegmentOffsets.Count == segmentCount)
                            {
                                containingList.Remove(this);
                                manager.FreeArena(this);
                            }
                        }
                    }
                }
            }

            internal MemoryMappedInfo CreateMemoryMappedViewInfo(long size)
            {
                lock (gate)
                {
                    var result = new MemoryMappedInfo(this.memoryMappedFile, freeSegmentOffsets.Pop(), size, this);
                    if (freeSegmentOffsets.IsEmpty())
                    {
                        // containingList should already be locked by our caller. 
                        this.containingList.Remove(this);
                    }

                    return result;
                }
            }
        }
    }
}
