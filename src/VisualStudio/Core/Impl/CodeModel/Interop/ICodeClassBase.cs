// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("23BBD58A-7C59-449b-A93C-43E59EFC080C")]
    internal interface ICodeClassBase
    {
        [PreserveSig]
        int GetBaseName([MarshalAs(UnmanagedType.BStr)] out string pBaseName);
    }
}
