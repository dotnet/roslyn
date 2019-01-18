// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal unsafe sealed class ComMemoryStream : IUnsafeComStream
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
        private unsafe static void ZeroMemory(byte* dest, int count)
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
}

