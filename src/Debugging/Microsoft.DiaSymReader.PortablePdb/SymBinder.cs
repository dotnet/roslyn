// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Roslyn.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

[assembly: Guid("CA89ACD1-A1D5-43DE-890A-5FDF50BC1F93")]

namespace Microsoft.DiaSymReader.PortablePdb
{
    [Guid("E4B18DEF-3B78-46AE-8F50-E67E421BDF70")]
    [ComVisible(true)]
    public sealed class SymBinder : ISymUnmanagedBinder
    {
        [PreserveSig]
        public unsafe int GetReaderForFile(
            [MarshalAs(UnmanagedType.Interface)]object importer,
            [MarshalAs(UnmanagedType.LPWStr)]string fileName,
            [MarshalAs(UnmanagedType.LPWStr)]string searchPath,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedReader reader)
        {
            if (importer == null || string.IsNullOrEmpty(fileName))
            {
                reader = null;
                return HResult.E_INVALIDARG;
            }

            var metadataImport = importer as IMetadataImport;
            if (metadataImport == null)
            {
                reader = null;
                return HResult.E_FAIL;
            }

            byte[] bytes;
            try
            {
                // TODO: use memory mapped files?
                bytes = PortableShim.File.ReadAllBytes(fileName);
            }
            catch
            {
                reader = null;
                return HResult.E_INVALIDARG;
            }

            reader = new SymReader(new PortablePdbReader(bytes, bytes.Length, metadataImport));
            return HResult.S_OK;
        }

        [PreserveSig]
        public int GetReaderFromStream(
            [MarshalAs(UnmanagedType.Interface)]object importer,
            [MarshalAs(UnmanagedType.Interface)]object stream,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedReader reader)
        {
            if (importer == null || stream == null)
            {
                reader = null;
                return HResult.E_INVALIDARG;
            }

            var metadataImport = importer as IMetadataImport;
            if (metadataImport == null)
            {
                reader = null;
                return HResult.E_FAIL;
            }

            byte[] bytes;
            int size;
            ReadAllBytes((IStream)stream, out bytes, out size);

            // TODO: use IUnsafeComStream (ComMemoryStream in tests)?

            reader = new SymReader(new PortablePdbReader(bytes, size, metadataImport));
            return HResult.S_OK;
        }

        private unsafe static void ReadAllBytes(IStream stream, out byte[] bytes, out int size)
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
    }
}

// regasm /codebase C:\R0\Binaries\Debug\Microsoft.DiaSymReader.PortablePdb.dll
// tlbexp C:\R0\Binaries\Debug\Microsoft.DiaSymReader.PortablePdb.dll
