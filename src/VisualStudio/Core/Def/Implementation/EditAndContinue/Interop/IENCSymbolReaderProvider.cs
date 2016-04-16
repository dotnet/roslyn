// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B69910A9-4AD6-475F-859A-5DC0B1072A5D")]
    internal interface IENCSymbolReaderProvider
    {
        void GetSymbolReader([MarshalAs(UnmanagedType.IUnknown)] out object ppSymbolReader);
    }
}
