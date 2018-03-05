// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3E596484-D2E4-461a-A876-254C4F097EBB")]
    internal interface IMethodXML
    {
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetXML();

        [PreserveSig]
        int SetXML([MarshalAs(UnmanagedType.BStr)] string bstrXML);

        /// <param name="ppUnk">Really a TextPoint.</param>
        [PreserveSig]
        int GetBodyPoint([MarshalAs(UnmanagedType.IUnknown)] out object ppUnk);
    }
}
