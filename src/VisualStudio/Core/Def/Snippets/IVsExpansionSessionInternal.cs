// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

/// <summary>
/// This allows us to get pNode as an IntPtr instead of a via a RCW. Otherwise, a second 
/// invocation of the same snippet may cause an AccessViolationException.
/// </summary>
[Guid("3DFA7603-3B51-4484-81CD-FF1470123C7C")]
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IVsExpansionSessionInternal
{
    void Reserved1();
    void Reserved2();
    void Reserved3();
    void Reserved4();
    void Reserved5();
    void Reserved6();
    void Reserved7();
    void Reserved8();

    /// <summary>
    /// WARNING: Marshal pNode with GetUniqueObjectForIUnknown and call ReleaseComObject on it
    /// before leaving the calling method.
    /// </summary>
    [PreserveSig]
    int GetSnippetNode([MarshalAs(UnmanagedType.BStr)] string bstrNode, out IntPtr pNode);

    void Reserved9();
    void Reserved10();
    void Reserved11();
}
