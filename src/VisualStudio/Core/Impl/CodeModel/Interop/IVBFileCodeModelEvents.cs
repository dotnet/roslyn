// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComImport]
    [Guid("EA1A87AD-7BC5-4349-B3BE-CADC301F17A3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    internal interface IVBFileCodeModelEvents
    {
        [PreserveSig]
        int StartEdit();

        [PreserveSig]
        int EndEdit();
    }
}
