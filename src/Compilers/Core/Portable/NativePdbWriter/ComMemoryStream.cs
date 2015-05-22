// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A COM IStream implementation over memory. Supports just enough for DiaSymReader's PDB writing.
    /// Also tuned for performance:
    /// 1. SetSize (and Seek beyond the length) is very fast and doesn't re-allocate the underlying memory.
    /// 2. Read and Write are optimized to avoid copying (see <see cref="IUnsafeComStream"/>)
    /// 3. Allocates in chunks instead of a contiguous buffer to avoid re-alloc and copy costs when growing.
    /// </summary>
    internal class ComMemoryStream : IUnsafeComStream
    {
        private const int ChunkSize = 32768;
        private readonly List<byte[]> _chunks = new List<byte[]>();
        private int _position;
        private int _length;

        public void CopyTo(Stream stream)
        {
            // If the target stream allows seeking set its length upfront.
            // When writing to a large file, it helps to give a hint to the OS how big the file is going to be.
            if (stream.CanSeek)
            {
                stream.SetLength(stream.Position + _length);
            }

            int chunkIndex = 0;
            for (int remainingBytes = _length; remainingBytes > 0;)
            {
                int bytesToCopy = Math.Min(ChunkSize, remainingBytes);
                if (chunkIndex < _chunks.Count)
                {
                    stream.Write(_chunks[chunkIndex++], 0, bytesToCopy);
                }
                else
                {
                    // Fill remaining space with zero bytes
                    for (int i = 0; i < bytesToCopy; i++)
                    {
                        stream.WriteByte(0);
                    }
                }

                remainingBytes -= bytesToCopy;
            }
        }

        private unsafe static void ZeroMemory(IntPtr dest, int count)
        {
            var p = (byte*)dest;
            while (count-- > 0)
            {
                *p++ = 0;
            }
        }

        unsafe void IUnsafeComStream.Read(IntPtr pv, int cb, IntPtr pcbRead)
        {
            int chunkIndex = _position / ChunkSize;
            int chunkOffset = _position % ChunkSize;
            int destinationIndex = 0;
            int bytesRead = 0;

            while (true)
            {
                int bytesToCopy = Math.Min(_length - _position, Math.Min(cb, ChunkSize - chunkOffset));
                if (bytesToCopy == 0)
                {
                    break;
                }

                if (chunkIndex < _chunks.Count)
                {
                    Marshal.Copy(_chunks[chunkIndex], chunkOffset, pv + destinationIndex, bytesToCopy);
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

            if (pcbRead != IntPtr.Zero)
            {
                *(int*)pcbRead = bytesRead;
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

        unsafe void IUnsafeComStream.Seek(long dlibMove, int origin, IntPtr plibNewPosition)
        {
            int newPosition;

            switch (origin)
            {
                case 0: // STREAM_SEEK_SET
                    newPosition = SetPosition((int)dlibMove);
                    break;

                case 1: // STREAM_SEEK_CUR
                    newPosition = SetPosition(_position + (int)dlibMove);
                    break;

                case 2: // STREAM_SEEK_END
                    newPosition = SetPosition(_length + (int)dlibMove);
                    break;

                default:
                    throw new ArgumentException(nameof(origin));
            }

            if (plibNewPosition != IntPtr.Zero)
            {
                *(long*)plibNewPosition = newPosition;
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

        unsafe void IUnsafeComStream.Write(IntPtr pv, int cb, IntPtr pcbWritten)
        {
            int chunkIndex = _position / ChunkSize;
            int chunkOffset = _position % ChunkSize;
            int bytesWritten = 0;
            while (true)
            {
                int bytesToCopy = Math.Min(cb, ChunkSize - chunkOffset);
                if (bytesToCopy == 0)
                {
                    break;
                }

                while (chunkIndex >= _chunks.Count)
                {
                    _chunks.Add(new byte[ChunkSize]);
                }

                Marshal.Copy(pv + bytesWritten, _chunks[chunkIndex], chunkOffset, bytesToCopy);
                bytesWritten += bytesToCopy;
                cb -= bytesToCopy;
                chunkIndex++;
                chunkOffset = 0;
            }

            SetPosition(_position + bytesWritten);

            if (pcbWritten != IntPtr.Zero)
            {
                *(int*)pcbWritten = bytesWritten;
            }
        }

        void IUnsafeComStream.Commit(int grfCommitFlags)
        {
        }

        void IUnsafeComStream.Clone(out IStream ppstm)
        {
            throw new NotSupportedException();
        }

        void IUnsafeComStream.CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
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
}

