// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
#if NET9_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

#if !NET9_0_OR_GREATER
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;
#endif

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
        void Stat(out STATSTG pstatstg, int grfStatFlag);
        void Clone(out IntPtr ppstm);
    }

#if NET9_0_OR_GREATER
    [NativeMarshalling(typeof(STATSTGMarshaller))]
    public struct STATSTG
    {
        public string pwcsName;
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

    [CustomMarshaller(typeof(STATSTG), MarshalMode.ManagedToUnmanagedOut, typeof(STATSTGMarshaller))]
    [CustomMarshaller(typeof(STATSTG), MarshalMode.UnmanagedToManagedOut, typeof(STATSTGMarshaller))]
    public static unsafe class STATSTGMarshaller
    {
        public struct Native
        {
            public ushort* pwcsName;
            public int type;
            public long cbSize;
            public FILETIME mtime;
            public FILETIME ctime;
            public FILETIME atime;
            public int grfMode;
            public Guid clsid;
            public int grfLocksSupported;
            public int grfStateBits;
            public int reserved;
        }

        public static STATSTG ConvertToManaged(Native n)
        {
            string name = null;
            if (n.pwcsName != null)
            {
                name = Utf16StringMarshaller.ConvertToManaged(n.pwcsName);
                Marshal.FreeCoTaskMem((IntPtr)n.pwcsName);
            }

            return new()
            {
                pwcsName = name,
                type = n.type,
                cbSize = n.cbSize,
                mtime = n.mtime,
                ctime = n.ctime,
                atime = n.atime,
                grfMode = n.grfMode,
                clsid = n.clsid,
                grfLocksSupported = n.grfLocksSupported,
                grfStateBits = n.grfStateBits,
                reserved = n.reserved
            };
        }

        public static Native ConvertToUnmanaged(STATSTG n) => new()
        {
            pwcsName = n.pwcsName is null ? null : Utf16StringMarshaller.ConvertToUnmanaged(n.pwcsName),
            type = n.type,
            cbSize = n.cbSize,
            mtime = n.mtime,
            ctime = n.ctime,
            atime = n.atime,
            grfMode = n.grfMode,
            clsid = n.clsid,
            grfLocksSupported = n.grfLocksSupported,
            grfStateBits = n.grfStateBits,
            reserved = n.reserved
        };
    }
#endif
}
