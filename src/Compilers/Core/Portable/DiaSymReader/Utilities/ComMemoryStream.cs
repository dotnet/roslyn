// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// A COM IStream implementation over memory. Supports just enough for DiaSymReader's PDB writing.
    /// Also tuned for performance:
    /// 1. SetSize (and Seek beyond the length) is very fast and doesn't re-allocate the underlying memory.
    /// 2. Read and Write are optimized to avoid copying (see <see cref="IUnsafeComStream"/>)
    /// 3. Allocates in chunks instead of a contiguous buffer to avoid re-alloc and copy costs when growing.
    /// </summary>
    internal sealed unsafe class ComMemoryStream : IUnsafeComStream
    {
        // internal for testing
        internal const int STREAM_SEEK_SET = 0;
        internal const int STREAM_SEEK_CUR = 1;
        internal const int STREAM_SEEK_END = 2;

        private readonly int _chunkSize;
        private readonly List<byte[]> _chunks = new List<byte[]>();
        private int _position;
        private int _length;

        public ComMemoryStream(int chunkSize = 32768)
        {
            _chunkSize = chunkSize;
        }

        public void CopyTo(Stream stream)
        {
            // If the target stream allows seeking set its length upfront.
            // When writing to a large file, it helps to give a hint to the OS how big the file is going to be.
            if (stream.CanSeek)
            {
                stream.SetLength(stream.Position + _length);
            }

            int chunkIndex = 0;
            int remainingBytes = _length;
            while (remainingBytes > 0)
            {
                int bytesToCopy;
                if (chunkIndex < _chunks.Count)
                {
                    var chunk = _chunks[chunkIndex];
                    bytesToCopy = Math.Min(chunk.Length, remainingBytes);
                    stream.Write(chunk, 0, bytesToCopy);
                    chunkIndex++;
                }
                else
                {
                    // Fill remaining space with zero bytes
                    bytesToCopy = remainingBytes;
                    for (int i = 0; i < bytesToCopy; i++)
                    {
                        stream.WriteByte(0);
                    }
                }

                remainingBytes -= bytesToCopy;
            }
        }

        public IEnumerable<ArraySegment<byte>> GetChunks()
        {
            int chunkIndex = 0;
            int remainingBytes = _length;
            while (remainingBytes > 0)
            {
                int bytesToCopy;

                byte[] chunk;
                if (chunkIndex < _chunks.Count)
                {
                    chunk = _chunks[chunkIndex];
                    bytesToCopy = Math.Min(chunk.Length, remainingBytes);
                    chunkIndex++;
                }
                else
                {
                    // The caller seeked behind the end of the stream and didn't write there.
                    // The allocated array is not big in practice. 
                    chunk = new byte[remainingBytes];
                    bytesToCopy = remainingBytes;
                }

                yield return new ArraySegment<byte>(chunk, 0, bytesToCopy);

                remainingBytes -= bytesToCopy;
            }
        }
        private static unsafe void ZeroMemory(byte* dest, int count)
        {
            var p = dest;
            while (count-- > 0)
            {
                *p++ = 0;
            }
        }

        unsafe void IUnsafeComStream.Read(byte* pv, int cb, int* pcbRead)
        {
            int chunkIndex = _position / _chunkSize;
            int chunkOffset = _position % _chunkSize;
            int destinationIndex = 0;
            int bytesRead = 0;

            while (true)
            {
                int bytesToCopy = Math.Min(_length - _position, Math.Min(cb, _chunkSize - chunkOffset));
                if (bytesToCopy == 0)
                {
                    break;
                }

                if (chunkIndex < _chunks.Count)
                {
                    Marshal.Copy(_chunks[chunkIndex], chunkOffset, (IntPtr)(pv + destinationIndex), bytesToCopy);
                }
                else
                {
                    ZeroMemory(pv + destinationIndex, bytesToCopy);
                }

                bytesRead += bytesToCopy;
                _position += bytesToCopy;
                cb -= bytesToCopy;
                destinationIndex += bytesToCopy;
                chunkIndex++;
                chunkOffset = 0;
            }

            if (pcbRead != null)
            {
                *pcbRead = bytesRead;
            }
        }

        private int SetPosition(int newPos)
        {
            if (newPos < 0)
            {
                newPos = 0;
            }

            _position = newPos;

            if (newPos > _length)
            {
                _length = newPos;
            }

            return newPos;
        }

        unsafe void IUnsafeComStream.Seek(long dlibMove, int origin, long* plibNewPosition)
        {
            int newPosition;

            switch (origin)
            {
                case STREAM_SEEK_SET:
                    newPosition = SetPosition((int)dlibMove);
                    break;

                case STREAM_SEEK_CUR:
                    newPosition = SetPosition(_position + (int)dlibMove);
                    break;

                case STREAM_SEEK_END:
                    newPosition = SetPosition(_length + (int)dlibMove);
                    break;

                default:
                    throw new ArgumentException($"{nameof(origin)} ({origin}) is invalid.", nameof(origin));
            }

            if (plibNewPosition != null)
            {
                *plibNewPosition = newPosition;
            }
        }

        void IUnsafeComStream.SetSize(long libNewSize)
        {
            _length = (int)libNewSize;
        }

        void IUnsafeComStream.Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG()
            {
                cbSize = _length
            };
        }

        unsafe void IUnsafeComStream.Write(byte* pv, int cb, int* pcbWritten)
        {
            int chunkIndex = _position / _chunkSize;
            int chunkOffset = _position % _chunkSize;
            int bytesWritten = 0;
            while (true)
            {
                int bytesToCopy = Math.Min(cb, _chunkSize - chunkOffset);
                if (bytesToCopy == 0)
                {
                    break;
                }

                while (chunkIndex >= _chunks.Count)
                {
                    _chunks.Add(new byte[_chunkSize]);
                }

                Marshal.Copy((IntPtr)(pv + bytesWritten), _chunks[chunkIndex], chunkOffset, bytesToCopy);
                bytesWritten += bytesToCopy;
                cb -= bytesToCopy;
                chunkIndex++;
                chunkOffset = 0;
            }

            SetPosition(_position + bytesWritten);

            if (pcbWritten != null)
            {
                *pcbWritten = bytesWritten;
            }
        }

        void IUnsafeComStream.Commit(int grfCommitFlags)
        {
        }

        void IUnsafeComStream.Clone(out IStream ppstm)
        {
            throw new NotSupportedException();
        }

        void IUnsafeComStream.CopyTo(IStream pstm, long cb, int* pcbRead, int* pcbWritten)
        {
            throw new NotSupportedException();
        }

        void IUnsafeComStream.LockRegion(long libOffset, long cb, int lockType)
        {
            throw new NotSupportedException();
        }

        void IUnsafeComStream.Revert()
        {
            throw new NotSupportedException();
        }

        void IUnsafeComStream.UnlockRegion(long libOffset, long cb, int lockType)
        {
            throw new NotSupportedException();
        }
    }

#if NET
    internal unsafe sealed class ComMemoryStreamWrapperCache : ComWrappers
    {
        public static readonly ComMemoryStreamWrapperCache Instance = new ComMemoryStreamWrapperCache();
        private ComMemoryStreamWrapperCache() { }

        private static readonly IntPtr s_vtable;
        private static readonly (IntPtr Entries, int Count) s_definition;

        static ComMemoryStreamWrapperCache()
        {
            GetIUnknownImpl(out IntPtr queryIfacePtr, out IntPtr addRefPtr, out IntPtr releasePtr);

            int tableCount = 14;
            int i = 0;
            var vtable = (IntPtr*)System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(ComMemoryStream),
                IntPtr.Size * tableCount);
            vtable[i++] = queryIfacePtr;
            vtable[i++] = addRefPtr;
            vtable[i++] = releasePtr;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, int, int*, int>)&Ccw.Read;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, byte*, int, int*, int>)&Ccw.Write;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, long, int, long*, int>)&Ccw.Seek;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, long, int>)&Ccw.SetSize;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, long, int*, int*, int>)&Ccw.CopyTo;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, int, int>)&Ccw.Commit;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&Ccw.Revert;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, long, long, int, int>)&Ccw.LockRegion;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, long, long, int, int>)&Ccw.UnlockRegion;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, Ccw.STATSTG*, int, int>)&Ccw.Stat;
            vtable[i++] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr*, int>)&Ccw.Clone;
            System.Diagnostics.Debug.Assert(tableCount == i);
            s_vtable = (IntPtr)vtable;

            int definitionLen = 1;
            i = 0;
            var entries = (ComInterfaceEntry*)System.Runtime.CompilerServices.RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(ComMemoryStream),
                sizeof(ComInterfaceEntry) * definitionLen);
            entries[i++] = new ComInterfaceEntry() { IID = IUnsafeComStream.IID, Vtable = s_vtable };
            System.Diagnostics.Debug.Assert(i == definitionLen);
            s_definition = ((IntPtr)entries, definitionLen);
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            System.Diagnostics.Debug.Assert(flags == CreateComInterfaceFlags.None);

            if (obj is not ComMemoryStream)
                throw new NotSupportedException();

            count = s_definition.Count;
            return (ComInterfaceEntry*)s_definition.Entries;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            throw new NotImplementedException();
        }

        protected override void ReleaseObjects(System.Collections.IEnumerable objects)
        {
            throw new NotImplementedException();
        }

        private static unsafe class Ccw
        {
            [UnmanagedCallersOnly]
            public static int Read(IntPtr @this, byte* pv, int cb, int* pcbRead)
            {
                try
                {
                    ComInterfaceDispatch.GetInstance<IUnsafeComStream>((ComInterfaceDispatch*)@this).Read(pv, cb, pcbRead);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
                return HResult.S_OK;
            }

            [UnmanagedCallersOnly]
            public static int Write(IntPtr @this, byte* pv, int cb, int* pcbWritten)
            {
                try
                {
                    ComInterfaceDispatch.GetInstance<IUnsafeComStream>((ComInterfaceDispatch*)@this).Write(pv, cb, pcbWritten);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
                return HResult.S_OK;
            }

            [UnmanagedCallersOnly]
            public static int Seek(IntPtr @this, long dlibMove, int dwOrigin, long* plibNewPosition)
            {
                try
                {
                    ComInterfaceDispatch.GetInstance<IUnsafeComStream>((ComInterfaceDispatch*)@this).Seek(dlibMove, dwOrigin, plibNewPosition);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
                return HResult.S_OK;
            }

            [UnmanagedCallersOnly]
            public static int SetSize(IntPtr @this, long libNewSize)
            {
                try
                {
                    ComInterfaceDispatch.GetInstance<IUnsafeComStream>((ComInterfaceDispatch*)@this).SetSize(libNewSize);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
                return HResult.S_OK;
            }

            [UnmanagedCallersOnly]
            public static int CopyTo(IntPtr @this, IntPtr pstm, long cb, int* pcbRead, int* pcbWritten)
            {
                // CopyTo is not used by the sym writer
                return HResult.E_NOTIMPL;
            }

            [UnmanagedCallersOnly]
            public static int Commit(IntPtr @this, int grfCommitFlags)
            {
                try
                {
                    ComInterfaceDispatch.GetInstance<IUnsafeComStream>((ComInterfaceDispatch*)@this).Commit(grfCommitFlags);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
                return HResult.S_OK;
            }

            [UnmanagedCallersOnly]
            public static int Revert(IntPtr @this)
            {
                return HResult.E_NOTIMPL;
            }

            [UnmanagedCallersOnly]
            public static int LockRegion(IntPtr @this, long libOffset, long cb, int dwLockType)
            {
                return HResult.E_NOTIMPL;
            }

            [UnmanagedCallersOnly]
            public static int UnlockRegion(IntPtr @this, long libOffset, long cb, int dwLockType)
            {
                return HResult.E_NOTIMPL;
            }

            [StructLayout(LayoutKind.Sequential)]
            public readonly unsafe struct STATSTG
            {
                public readonly char* pwcsName;
                public readonly int type;
                public readonly long cbSize;
                public readonly System.Runtime.InteropServices.ComTypes.FILETIME mtime;
                public readonly System.Runtime.InteropServices.ComTypes.FILETIME ctime;
                public readonly System.Runtime.InteropServices.ComTypes.FILETIME atime;
                public readonly int grfMode;
                public readonly int grfLocksSupported;
                public readonly Guid clsid;
                public readonly int grfStateBits;
                public readonly int reserved;
            }

            [UnmanagedCallersOnly]
            public static int Stat(IntPtr @this, STATSTG* pstatstg, int grfStatFlag)
            {
                return HResult.E_NOTIMPL;
            }

            [UnmanagedCallersOnly]
            public static int Clone(IntPtr @this, IntPtr* ppstm)
            {
                return HResult.E_NOTIMPL;
            }
        }
    }
#endif
}

