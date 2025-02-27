// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Interop;

[Guid("1EAA526A-0898-11d3-B868-00C04F79F802"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IVsAppId
{
    [PreserveSig]
    int SetSite(IOleServiceProvider pSP);

    [PreserveSig]
    int GetProperty(int propid, // VSAPROPID
        [MarshalAs(UnmanagedType.Struct)] out object pvar);

    [PreserveSig]
    int SetProperty(int propid, //[in] VSAPROPID
        [MarshalAs(UnmanagedType.Struct)] object var);

    [PreserveSig]
    int GetGuidProperty(int propid, // VSAPROPID
        out Guid guid);

    [PreserveSig]
    int SetGuidProperty(int propid, // [in] VSAPROPID
        ref Guid rguid);

    [PreserveSig]
    int Initialize();  // called after main initialization and before command executing and entering main loop
}
