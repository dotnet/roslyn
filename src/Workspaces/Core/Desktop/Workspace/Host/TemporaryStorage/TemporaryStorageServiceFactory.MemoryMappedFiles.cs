// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class TemporaryStorageServiceFactory
    {
        /// <summary>
        /// Our own abstraction on top of memory map file so that we can have shared views over mmf files. 
        /// Otherwise, each view has minimum size of 64K due to requirement forced by windows.
        /// 
        /// most of our view will have short lifetime, but there are cases where view might live a bit longer such as
        /// metadata dll shadow copy. shared view will help those cases.
        /// </summary>
        internal sealed class MemoryMappedInfo : IDisposable
        {
            private readonly string _name;
            private readonly long _size;
            private readonly MemoryMappedFile _memoryMappedFile;

            /// <summary>
            /// ref count of stream given out
            /// </summary>
            private int _streamCount;

            /// <summary>
            /// actual memory accessor that owns the VM
            /// </summary>
            private MemoryMappedViewAccessor _accessor;

            public MemoryMappedInfo(long size)
            {
                _name = CreateUniqueName(size);
                _size = size;

                _memoryMappedFile = MemoryMappedFile.CreateNew(_name, size);

                _streamCount = 0;
                _accessor = null;
            }

            public MemoryMappedInfo(string name, long size)
            {
                _name = name;
                _size = size;

                _memoryMappedFile = MemoryMappedFile.OpenExisting(_name);

                _streamCount = 0;
                _accessor = null;
            }

            /// <summary>
            /// Name and Size of memory map file
            /// </summary>
            public string Name => _name;
            public long Size => _size;

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
                        _accessor = _memoryMappedFile.CreateViewAccessor(0, _size, MemoryMappedFileAccess.Read);
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
                    return _memoryMappedFile.CreateViewStream(0, _size, MemoryMappedFileAccess.Write);
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

                // Dispose the memoryMappedFile
                _memoryMappedFile.Dispose();
            }

            public static string CreateUniqueName(long size)
            {
                return "Roslyn Temp Storage " + size.ToString() + " " + Guid.NewGuid().ToString("N");
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
    }
}
