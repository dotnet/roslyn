// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// This is a re-definition of COM's IStream interface. The important change is that
    /// the Read and Write methods take a pointer instead of a byte[] to avoid the
    /// allocation cost when called from native code.
    /// </summary>
    [Guid("0000000c-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [GeneratedWhenPossibleComInterface]
    internal unsafe partial interface IUnsafeComStream
    {
        // ISequentialStream portion
        void Read(byte* pv, int cb, int* pcbRead);
        void Write(byte* pv, int cb, int* pcbWritten);

        // IStream portion
        void Seek(long dlibMove, int dwOrigin, long* plibNewPosition);
        void SetSize(long libNewSize);
        void CopyTo(IntPtr pstm, long cb, int* pcbRead, int* pcbWritten);
        void Commit(int grfCommitFlags);
        void Revert();
        void LockRegion(long libOffset, long cb, int dwLockType);
        void UnlockRegion(long libOffset, long cb, int dwLockType);
        void Stat(ref NativeSTATSTG pstatstg, int grfStatFlag);
        void Clone(out IntPtr ppstm);
    }

    /// <summary>
    /// Native definition of `STATSTG`. Needed because the implementation of <see cref="IUnsafeComStream.Stat" /> in mscordbi
    /// (see https://github.com/dotnet/runtime/blob/87523393fdb14746ceb529ab308f11047819fd01/src/coreclr/inc/memorystreams.h#L115) doesn't
    /// zero-initialize the structure, so normal marshaling would corrupt the heap trying to process unitialized memory for (e.g. <see cref="NativeSTATSTG.pwcsName"/>).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal struct NativeSTATSTG
    {
        public IntPtr pwcsName;
        public int type;
        public long cbSize;
        public FILETIME mtime;
        public FILETIME ctime;
        public FILETIME atime;
        public int grfMode;
        public int grfLocksSupported;
        public Guid clsid;
        public int grfStateBits;
        public int reserved;
    }
}
