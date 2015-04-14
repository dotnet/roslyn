// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [Guid("AA544D42-28CB-11d3-BD22-0000F80849BD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface ISymUnmanagedBinder
    {
        /// <summary>
        /// Given a metadata interface and a file name, returns the
        /// correct <see cref="ISymUnmanagedReader"/> that will read the debugging symbols
        /// associated with the module.
        /// </summary>
        [PreserveSig]
        int GetReaderForFile(
            [MarshalAs(UnmanagedType.Interface)]object metadataImporter,
            [MarshalAs(UnmanagedType.LPWStr)]string fileName,
            [MarshalAs(UnmanagedType.LPWStr)]string searchPath,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedReader reader);

        /// <summary>
        /// Given a metadata interface and a stream that contains
        /// the symbol store, returns the <see cref="ISymUnmanagedReader"/>
        /// that will read the debugging symbols from the given
        /// symbol store.
        /// </summary>
        [PreserveSig]
        int GetReaderFromStream(
            [MarshalAs(UnmanagedType.Interface)]object metadataImporter,
            [MarshalAs(UnmanagedType.Interface)]object stream,
            [MarshalAs(UnmanagedType.Interface)]out ISymUnmanagedReader reader);
    };
}