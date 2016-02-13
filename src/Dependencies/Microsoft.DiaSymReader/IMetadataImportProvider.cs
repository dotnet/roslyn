// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    [ComImport]
    [Guid("EDF3A293-A10D-4F4A-A609-38D5EDE35F89")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(false)]
    public interface IMetadataImportProvider
    {
        /// <summary>
        /// Gets an instance of IMetadataImport.
        /// </summary>
        /// <remarks>
        /// The implementer is responsible for managing the lifetime of the resulting object.
        /// </remarks>
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetMetadataImport();
    }
}
