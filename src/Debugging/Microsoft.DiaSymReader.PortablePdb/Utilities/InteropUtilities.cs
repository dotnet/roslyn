// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal static class InteropUtilities
    {
        public static int StringToBuffer(
            string str,
            int bufferLength,
            out int count,
            char[] buffer)
        {
            // include NUL terminator:
            count = str.Length + 1;

            if (buffer == null)
            {
                return HResult.S_OK;
            }

            if (count > bufferLength)
            {
                count = 0;
                return HResult.E_OUTOFMEMORY;
            }

            str.CopyTo(0, buffer, 0, str.Length);
            buffer[str.Length] = '\0';

            return HResult.S_OK;
        }

        public static int BytesToBuffer(
            byte[] bytes,
            int bufferLength,
            out int count,
            byte[] buffer)
        {
            count = bytes.Length;

            if (buffer == null)
            {
                return HResult.S_OK;
            }

            if (count > bufferLength)
            {
                count = 0;
                return HResult.E_OUTOFMEMORY;
            }

            Buffer.BlockCopy(bytes, 0, buffer, 0, count);
            return HResult.S_OK;
        }

        // TODO: use IUnsafeComStream (ComMemoryStream in tests)?
        internal unsafe static void ReadAllBytes(this IStream stream, out byte[] bytes, out int size)
        {
            const int STREAM_SEEK_SET = 0;

            size = GetStreamSize(stream);

            stream.Seek(0, STREAM_SEEK_SET, IntPtr.Zero);

            bytes = new byte[size];

            int bytesRead = 0;
            stream.Read(bytes, size, (IntPtr)(&bytesRead));

            if (bytesRead != size)
            {
                // TODO:
                throw new NotSupportedException();
            }
        }

        private static int GetStreamSize(IStream stream)
        {
            const int STATFLAG_NONAME = 1;

            STATSTG stats;
            stream.Stat(out stats, STATFLAG_NONAME);
            long result = stats.cbSize;
            if (result < 0 || result > int.MaxValue)
            {
                throw new BadImageFormatException();
            }

            return (int)result;
        }

        internal static void TransferOwnershipOrRelease(ref object objectOpt, object newOwnerOpt)
        {
            if (newOwnerOpt != null)
            {
                if (objectOpt != null && Marshal.IsComObject(objectOpt))
                {
                    Marshal.ReleaseComObject(objectOpt);
                }

                objectOpt = null;
            }
        }
    }
}
