// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
