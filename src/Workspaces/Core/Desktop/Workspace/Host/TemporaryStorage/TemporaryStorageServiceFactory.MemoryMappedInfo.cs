// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime;
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

            /// <summary>
            /// The memory mapped file.
            /// </summary>
            /// <remarks>
            /// <para>It is possible for this accessor to be disposed prior to the view and/or the streams which use it.
            /// However, the operating system does not actually close the views which are in use until the view handles
            /// are closed as well, even if the <see cref="MemoryMappedFile"/> is disposed first.</para>
            /// </remarks>
            private readonly MemoryMappedFile _memoryMappedFile;

            /// <summary>
            /// actual memory accessor that owns the VM
            /// </summary>
            /// <remarks>
            /// <para>It is possible for this accessor to be disposed prior to the streams which use it. However, the
            /// streams interact directly with the underlying memory buffer, and keep a
            /// <see cref="CopiedMemoryMappedViewHandle"/> to prevent that buffer from being released while still in
            /// use. The <see cref="SafeHandle"/> used by this accessor is reference counted, and is not finally
            /// released until the reference count reaches zero.</para>
            /// </remarks>
            private MemoryMappedViewAccessor _accessor;

            public MemoryMappedInfo(long size)
            {
                _name = CreateUniqueName(size);
                _size = size;

                _memoryMappedFile = MemoryMappedFile.CreateNew(_name, size);
            }

            public MemoryMappedInfo(string name, long size)
            {
                _name = name;
                _size = size;

                _memoryMappedFile = MemoryMappedFile.OpenExisting(_name);
            }

            /// <summary>
            /// Name and Size of memory map file
            /// </summary>
            public string Name => _name;
            public long Size => _size;

            private void ForceCompactingGC()
            {
                // repeated GC.Collect / WaitForPendingFinalizers till memory freed delta is super small, ignore the return value
                GC.GetTotalMemory(forceFullCollection: true);

                // compact the LOH
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
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
                    if (_accessor == null)
                    {
                        try
                        {
                            _accessor = _memoryMappedFile.CreateViewAccessor(0, _size, MemoryMappedFileAccess.Read);
                        }
                        catch (IOException)
                        {
                            // CreateViewAccessor will use a native memory map - which can't trigger a GC.
                            // In this case, we'd otherwise crash with OOM, so we don't care about creating a UI delay with a full forced compacting GC.
                            // If it crashes the second try, it means we're legitimately out of resources.
                            this.ForceCompactingGC();
                            _accessor = _memoryMappedFile.CreateViewAccessor(0, _size, MemoryMappedFileAccess.Read);
                        }
                    }

                    Contract.Assert(_accessor.CanRead);
                    return new SharedReadableStream(this, new CopiedMemoryMappedViewHandle(_accessor.SafeMemoryMappedViewHandle), _accessor.PointerOffset, _size);
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

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    lock (_memoryMappedFile)
                    {
                        if (_accessor != null)
                        {
                            // (see remarks on accessor for relation between _accessor and the streams)
                            _accessor.Dispose();
                            _accessor = null;
                        }
                    }

                    // (see remarks on accessor for relation between _memoryMappedFile and the views/streams)
                    _memoryMappedFile.Dispose();
                }
            }

            public static string CreateUniqueName(long size)
            {
                return "Roslyn Temp Storage " + size.ToString() + " " + Guid.NewGuid().ToString("N");
            }

            private unsafe sealed class SharedReadableStream : Stream, ISupportDirectMemoryAccess
            {
                private readonly CopiedMemoryMappedViewHandle _handle;

                private byte* _start;
                private byte* _current;
                private readonly byte* _end;

                public SharedReadableStream(MemoryMappedInfo owner, CopiedMemoryMappedViewHandle handle, long offset, long length)
                {
                    _handle = handle;
                    _current = _start = handle.Pointer + offset;
                    _end = checked(_start + length);
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

                    if (disposing)
                    {
                        _handle.Dispose();
                    }

                    _start = null;
                }

                /// <summary>
                /// Get underlying native memory directly.
                /// </summary>
                public IntPtr GetPointer()
                {
                    return (IntPtr)_start;
                }
            }
        }
    }
}
