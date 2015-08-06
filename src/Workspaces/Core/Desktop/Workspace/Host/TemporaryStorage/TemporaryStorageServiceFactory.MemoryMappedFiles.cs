// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
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

            private readonly List<MemoryMappedFileArena> _smallFileStorage = new List<MemoryMappedFileArena>();
            private readonly List<MemoryMappedFileArena> _mediumFileStorage = new List<MemoryMappedFileArena>();
            private readonly List<MemoryMappedFileArena> _largeFileStorage = new List<MemoryMappedFileArena>();
            private readonly List<MemoryMappedFileArena> _hugeFileStorage = new List<MemoryMappedFileArena>();

            // Ugh.  We need to keep a strong reference to our allocated arenas.  Otherwise, when we remove instances
            // from the storage lists above, the only references left are MemoryMappedInfo instances.  If 
            // these all happen to end up on the finalizer thread, the underlying MemoryMappedFile SafeHandle will also
            // be finalized.  This invalidates the handle and when we try to reuse this arena by putting it back on a 
            // storage list, we'll hit an exception trying to access an invalid handle.
            private readonly List<MemoryMappedFileArena> _allocatedArenas = new List<MemoryMappedFileArena>();

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
                    storage = _smallFileStorage;
                    allocationSize = SmallFileMaxBytes;
                }
                else if (size <= MediumFileMaxBytes)
                {
                    storage = _mediumFileStorage;
                    allocationSize = MediumFileMaxBytes;
                }
                else if (size <= LargeFileMaxBytes)
                {
                    storage = _largeFileStorage;
                    allocationSize = LargeFileMaxBytes;
                }
                else if (size <= HugeFileMaxBytes)
                {
                    storage = _hugeFileStorage;
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
                        lock (_allocatedArenas)
                        {
                            _allocatedArenas.Add(arena);
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
                lock (_allocatedArenas)
                {
                    _allocatedArenas.Remove(memoryMappedFileArena);
                }
            }
        }

        internal sealed class MemoryMappedInfo : IDisposable
        {
            private readonly MemoryMappedFile _memoryMappedFile;
            private readonly long _offset;
            private readonly long _size;
            private readonly MemoryMappedFileArena _containingArena;

            /// <summary>
            /// ref count of stream given out
            /// </summary>
            private int _streamCount;

            /// <summary>
            /// actual memory accessor that owns the VM
            /// </summary>
            private MemoryMappedViewAccessor _accessor;

            public MemoryMappedInfo(MemoryMappedFile memoryMappedFile, long size) : this(memoryMappedFile, 0, size, null) { }

            public MemoryMappedInfo(MemoryMappedFile memoryMappedFile, long offset, long size, MemoryMappedFileArena containingArena)
            {
                _memoryMappedFile = memoryMappedFile;
                _offset = offset;
                _size = size;
                _containingArena = containingArena;

                _streamCount = 0;
                _accessor = null;
            }

            /// <summary>
            /// Caller is responsible for disposing the returned stream.
            /// multiple call of this will not increase VM.
            /// </summary>
            public Stream CreateReadableStream()
            {
                // CreateViewStream is not guaranteed to be thread-safe
                lock (_memoryMappedFile)
                {
                    if (_streamCount == 0)
                    {
                        _accessor = _memoryMappedFile.CreateViewAccessor(_offset, _size, MemoryMappedFileAccess.Read);
                    }

                    _streamCount++;
                    return new SharedReadableStream(this, _accessor, _size);
                }
            }

            /// <summary>
            /// Caller is responsible for disposing the returned stream.
            /// multiple call of this will increase VM.
            /// </summary>
            public Stream CreateWritableStream()
            {
                // CreateViewStream is not guaranteed to be thread-safe
                lock (_memoryMappedFile)
                {
                    return _memoryMappedFile.CreateViewStream(_offset, _size, MemoryMappedFileAccess.Write);
                }
            }

            private void StreamDisposed()
            {
                lock (_memoryMappedFile)
                {
                    _streamCount--;
                    if (_streamCount == 0 && _accessor != null)
                    {
                        _accessor.Dispose();
                        _accessor = null;
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
                if (_accessor != null)
                {
                    // dispose accessor it owns.
                    // if someone explicitly called Dispose when streams given out are not
                    // disposed yet, the accessor each stream has will simply stop working.
                    //
                    // it is caller's responsibility to make sure all streams it got from
                    // the temporary storage are disposed before calling dispose on the storage.
                    //
                    // otherwise, finalizer will take care of disposing stuff as we used to be.
                    _accessor.Dispose();
                    _accessor = null;
                }

                // Dispose the memoryMappedFile if we own it, otherwise 
                // notify our containingArena that this offset is available
                // for someone else 
                if (_containingArena == null)
                {
                    _memoryMappedFile.Dispose();
                }
                else
                {
                    _containingArena.FreeSegment(_offset);
                }
            }

            private unsafe sealed class SharedReadableStream : Stream, ISupportDirectMemoryAccess
            {
                private readonly MemoryMappedViewAccessor _accessor;

                private MemoryMappedInfo _owner;
                private byte* _start;
                private byte* _current;
                private readonly byte* _end;

                public SharedReadableStream(MemoryMappedInfo owner, MemoryMappedViewAccessor accessor, long length)
                {
                    Contract.Assert(accessor.CanRead);

                    _owner = owner;
                    _accessor = accessor;
                    _current = _start = AcquirePointer(accessor);
                    _end = checked(_start + length);
                }

                ~SharedReadableStream()
                {
                    // we don't have control on stream we give out to others such as
                    // compiler (ImageOnlyMetadataReference), make sure we dispose resource 
                    // at the end if Disposed is not called explicitly.
                    Dispose(false);
                }

                public override bool CanRead
                {
                    get
                    {
                        return true;
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
                        return _end - _start;
                    }
                }

                public override long Position
                {
                    get
                    {
                        return _current - _start;
                    }

                    set
                    {
                        var target = _start + value;
                        if (target < _start || target >= _end)
                        {
                            throw new ArgumentOutOfRangeException(nameof(value));
                        }

                        _current = target;
                    }
                }

                public override int ReadByte()
                {
                    // PERF: Keeping this as simple as possible since it's on the hot path
                    if (_current >= _end)
                    {
                        return -1;
                    }

                    return *_current++;
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (_current >= _end)
                    {
                        return 0;
                    }

                    int adjustedCount = Math.Min(count, (int)(_end - _current));
                    Marshal.Copy((IntPtr)_current, buffer, offset, adjustedCount);

                    _current += adjustedCount;
                    return adjustedCount;
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    byte* target;
                    try
                    {
                        switch (origin)
                        {
                            case SeekOrigin.Begin:
                                target = checked(_start + offset);
                                break;

                            case SeekOrigin.Current:
                                target = checked(_current + offset);
                                break;

                            case SeekOrigin.End:
                                target = checked(_end + offset);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(nameof(origin));
                        }
                    }
                    catch (OverflowException)
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    }

                    if (target < _start || target >= _end)
                    {
                        throw new ArgumentOutOfRangeException(nameof(offset));
                    }

                    _current = target;
                    return _current - _start;
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

                    if (_start != null)
                    {
                        _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        _start = null;
                    }

                    if (_owner != null)
                    {
                        _owner.StreamDisposed();
                        _owner = null;
                    }
                }

                /// <summary>
                /// Get underlying native memory directly.
                /// </summary>
                public IntPtr GetPointer()
                {
                    return (IntPtr)_start;
                }

                /// <summary>
                /// Acquire the fixed pointer to the start of the memory mapped view.
                /// The pointer will be released during <see cref="Dispose(bool)"/>
                /// </summary>
                /// <returns>The pointer to the start of the memory mapped view. The pointer is valid, and remains fixed for the lifetime of this object.</returns>
                private static byte* AcquirePointer(MemoryMappedViewAccessor accessor)
                {
                    byte* ptr = null;
                    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    ptr += accessor.PointerOffset;
                    return ptr;
                }
            }
        }

        internal class MemoryMappedFileArena
        {
            // Use a 4 MB default arena size, we'll only have a fraction of that
            // mapped into our address space at any given time.
            public const int MemoryMappedFileArenaSize = 4 * 1024 * 1024;

            private readonly MemoryMappedFileManager _manager;
            private readonly List<MemoryMappedFileArena> _containingList;
            private readonly MemoryMappedFile _memoryMappedFile;

            // The byte offsets into memoryMappedFile that are available for allocations
            private readonly Stack<long> _freeSegmentOffsets;
            private readonly int _segmentCount;
            private readonly object _gate = new object();

            public MemoryMappedFileArena(MemoryMappedFileManager manager, List<MemoryMappedFileArena> containingList, int allocationSize)
            {
                Contract.Assert(containingList.Count == 0, "should only create a new arena when the containing list is empty");
                _manager = manager;
                _containingList = containingList;
                _memoryMappedFile = MemoryMappedFile.CreateNew(MemoryMappedFileManager.CreateUniqueName(allocationSize), MemoryMappedFileArenaSize, MemoryMappedFileAccess.ReadWrite);
                _freeSegmentOffsets = new Stack<long>(Enumerable.Range(0, MemoryMappedFileArenaSize / allocationSize).Select(x => (long)x * allocationSize));
                _segmentCount = _freeSegmentOffsets.Count;
            }

            public void FreeSegment(long offset)
            {
                int count;
                lock (_gate)
                {
                    _freeSegmentOffsets.Push(offset);
                    count = _freeSegmentOffsets.Count;
                }

                if (count == 1)
                {
                    // This arena has room for allocations now, so add it back to the list.
                    lock (_containingList)
                    {
                        _containingList.Add(this);
                    }
                }
                else if (count == _segmentCount)
                {
                    // this arena is no longer in use.
                    lock (_containingList)
                    {
                        lock (_gate)
                        {
                            // re-check to make sure no-one allocated after we released the lock
                            if (_freeSegmentOffsets.Count == _segmentCount)
                            {
                                _containingList.Remove(this);
                                _manager.FreeArena(this);
                            }
                        }
                    }
                }
            }

            internal MemoryMappedInfo CreateMemoryMappedViewInfo(long size)
            {
                lock (_gate)
                {
                    var result = new MemoryMappedInfo(_memoryMappedFile, _freeSegmentOffsets.Pop(), size, this);
                    if (_freeSegmentOffsets.IsEmpty())
                    {
                        // containingList should already be locked by our caller. 
                        _containingList.Remove(this);
                    }

                    return result;
                }
            }
        }
    }
}
