// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed partial class VisualStudioMetadataReferenceManager
    {
        [ComImport]
        [Guid("7998EA64-7F95-48B8-86FC-17CAF48BF5CB")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMetaDataInfo
        {
            // MetaData scope is opened (there's a reference to a MetaData interface for this scope).
            // Returns S_OK, COR_E_NOTSUPPORTED, or E_INVALIDARG (if NULL is passed).
            // STDMETHOD(GetFileMapping)(
            //    const void ** ppvData,        // [out] Pointer to the start of the mapped file.
            //    ULONG * pcbData,              // [out] Size of the mapped memory region.
            //    DWORD * pdwMappingType) PURE; // [out] Type of file mapping (code:CorFileMapping).
            [PreserveSig]
            int GetFileMapping(out IntPtr ppvData, out long pcbData, out CorFileMapping pdwMappingType);
        }

        // Flags returned from IMetaDataInfo.GetFileMapping
        internal enum CorFileMapping : uint
        {
            Flat = 0,    // Flat file mapping - file is mapped as data file (code:SEC_IMAGE flag was not 
            // passed to code:CreateFileMapping).
            ExecutableImage = 1     // Executable image file mapping - file is mapped for execution 
            // (either via code:LoadLibrary or code:CreateFileMapping with code:SEC_IMAGE flag).
        }

        internal enum CorOpenFlags : uint
        {
            Read = 0,
            Write = 1,
            ReadWriteMask = 1,

            CopyMemory = 2,

            ManifestMetadata = 8,
            ReadOnly = 16,
            TakeOwnership = 32,

            CacheImage = 4,
            NoTypeLib = 128
        }
    }
}
