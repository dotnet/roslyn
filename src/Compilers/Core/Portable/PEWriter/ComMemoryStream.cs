using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace Roslyn.Utilities
{
    internal class ComMemoryStream : IStream
    {
#if DEBUG
        private const int ChunkSize = 509; // Small prime number for debugging chunking
#else
        private const int ChunkSize = 32768;
#endif
        private List<byte[]> _chunks = new List<byte[]>();
        private int _position;
        private int _length;

        public unsafe void Read(byte[] pv, int cb, IntPtr pcbRead)
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
                    Array.Copy(_chunks[chunkIndex], chunkOffset, pv, destinationIndex, bytesToCopy);
                }
                else
                {
                    Array.Clear(pv, destinationIndex, bytesToCopy);
                }

                bytesRead += bytesToCopy;
                _position += bytesToCopy;
                cb -= bytesToCopy;
                if (cb == 0 || (_length - _position) == 0)
                {
                    break;
                }

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

        public unsafe void Seek(long dlibMove, int origin, IntPtr plibNewPosition)
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

        public void SetSize(long libNewSize)
        {
            _length = (int)libNewSize;
        }

        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG()
            {
                cbSize = _length
            };
        }

        public unsafe void Write(byte[] pv, int cb, IntPtr pcbWritten)
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

                Array.Copy(pv, bytesWritten, _chunks[chunkIndex], chunkOffset, bytesToCopy);
                bytesWritten += bytesToCopy;
                _position += bytesToCopy;
                cb -= bytesToCopy;
                if (cb == 0)
                {
                    break;
                }

                chunkIndex++;
                chunkOffset = 0;
            }

            if (_position > _length)
            {
                _length = _position;
            }

            if (pcbWritten != IntPtr.Zero)
            {
                *(int*)pcbWritten = bytesWritten;
            }
        }

        public void CopyTo(Stream stream)
        {
            int size = _length;

            // If the target stream allows seeking set its length upfront.
            // When writing to a large file, it helps to give a hint to the OS how big the file is going to be.
            if (stream.CanSeek)
            {
                stream.SetLength(stream.Position + size);
            }

            int chunkIndex = 0;
            for (int cb = size; cb > 0;)
            {
                int bytesToCopy = Math.Min(ChunkSize, cb);
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

                cb -= bytesToCopy;
            }
        }

        public void Commit(int grfCommitFlags)
        {
        }

        public void Clone(out IStream ppstm)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            throw new NotSupportedException();
        }

        public void LockRegion(long libOffset, long cb, int lockType)
        {
            throw new NotSupportedException();
        }

        public void Revert()
        {
            throw new NotSupportedException();
        }

        public void UnlockRegion(long libOffset, long cb, int lockType)
        {
            throw new NotSupportedException();
        }
    }
}
