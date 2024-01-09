// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("1DD71F22-C880-46be-A462-A0A5542BC939")]
internal interface IVsMessageBoxService
{
    [PreserveSig]
    int ShowMessageBox(
        IntPtr hWndOwner,
        IntPtr hInstance,
        [MarshalAs(UnmanagedType.LPWStr)] string lpszText,
        [MarshalAs(UnmanagedType.LPWStr)] string lpszCaption,
        uint dwStyle,
        IntPtr lpszIcon,
        IntPtr dwContextHelpId,
        IntPtr pfnMessageBoxCallback,
        uint dwLangID,
        out int pidButton);
}
