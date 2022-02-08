// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
