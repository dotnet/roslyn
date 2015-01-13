// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace Roslyn.Utilities
{
    internal sealed class ComStreamWrapper : IStream
    {
        private readonly Stream stream;

        public ComStreamWrapper(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanSeek);

            this.stream = stream;
        }

        public void Commit(int grfCommitFlags)
        {
            stream.Flush();
        }

        public unsafe void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            int bytesRead = stream.Read(pv, 0, cb);

            if (pcbRead != IntPtr.Zero)
            {
                *(int*)pcbRead = bytesRead;
            }
        }

        public unsafe void Seek(long dlibMove, int origin, IntPtr plibNewPosition)
        {
            long newPosition = stream.Seek(dlibMove, (SeekOrigin)origin);
            if (plibNewPosition != IntPtr.Zero)
            {
                *(long*)plibNewPosition = newPosition;
            }
        }

        public void SetSize(long libNewSize)
        {
            stream.SetLength(libNewSize);
        }

        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG()
            {
                cbSize = stream.Length
            };
        }

        public unsafe void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            stream.Write(pv, 0, cb);
            if (pcbWritten != IntPtr.Zero)
            {
                *(int*)pcbWritten = cb;
            }
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