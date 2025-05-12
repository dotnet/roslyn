// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

/// <summary>
/// This interface is redefined by copy/paste from Reflector, so that we can tweak the
/// definitions of GetStaticEventBindingsForObject, because they take optional out params, and
/// the marshalling was wrong in the PIA.
/// </summary>
[ComImport, Guid("22FF7776-2C9A-48C4-809F-39E5184CC32D"), ComConversionLoss, InterfaceType(1)]
internal interface IVsContainedLanguageStaticEventBinding
{
    [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    int GetStaticEventBindingsForObject(
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectName,
        out int pcMembers,
        IntPtr ppbstrEventNames,
        IntPtr ppbstrDisplayNames,
        IntPtr ppbstrMemberIDs);

    [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    int RemoveStaticEventBinding(
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszUniqueMemberID,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent);

    [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    int AddStaticEventBinding(
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszUniqueMemberID,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent);

    [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    int EnsureStaticEventHandler(
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectTypeName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectName,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent,
        [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszEventHandlerName,
        [In, ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")] uint itemidInsertionPoint,
        [MarshalAs(UnmanagedType.BStr)] out string pbstrUniqueMemberID,
        [MarshalAs(UnmanagedType.BStr)] out string pbstrEventBody,
        [Out, ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan"), MarshalAs(UnmanagedType.LPArray)] TextSpan[] pSpanInsertionPoint);
}
