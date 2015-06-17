// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

[assembly: Guid("CA89ACD1-A1D5-43DE-890A-5FDF50BC1F93")]

namespace Microsoft.DiaSymReader.PortablePdb
{
    [Guid("E4B18DEF-3B78-46AE-8F50-E67E421BDF70")]
    [ComVisible(true)]
    public class SymBinder : ISymUnmanagedBinder
    {
        [PreserveSig]
        public int GetReaderForFile(
            [MarshalAs(UnmanagedType.Interface)]object importer, 
            [MarshalAs(UnmanagedType.LPWStr)]string fileName,
            [MarshalAs(UnmanagedType.LPWStr)]string searchPath,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedReader reader)
        {
            var pdbReader = new PortablePdbReader(File.OpenRead(fileName), importer);
            reader = new SymReader(pdbReader);
            return HResult.S_OK;
        }

        [PreserveSig]
        public int GetReaderFromStream(
            [MarshalAs(UnmanagedType.Interface)]object importer,
            [MarshalAs(UnmanagedType.Interface)]object pstream,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedReader reader)
        {
            // TODO:
            throw new NotImplementedException();
        }
    }
}

// regasm /codebase C:\R0\Binaries\Debug\Microsoft.DiaSymReader.PortablePdb.dll
// tlbexp C:\R0\Binaries\Debug\Microsoft.DiaSymReader.PortablePdb.dll
