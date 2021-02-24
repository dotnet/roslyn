// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
