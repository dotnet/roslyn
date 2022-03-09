// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    /// <summary>
    /// This interface is redefined by copy/paste from Reflector, so that we can tweak the
    /// definitions of GetMembers and GetCompatibleEventMembers, because they take optional out
    /// params, and the marshalling was wrong in the PIA.
    /// </summary>
    [ComImport, ComConversionLoss, InterfaceType(1), Guid("F386BE91-0E80-43AF-8EB6-8B829FA06282")]
    internal interface IVsContainedLanguageCodeSupport
    {
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int CreateUniqueEventName(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrEventHandlerName);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int EnsureEventHandler(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectTypeName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszEventHandlerName,
            [In, ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")] uint itemidInsertionPoint, [MarshalAs(UnmanagedType.BStr)] out string pbstrUniqueMemberID,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrEventBody, [Out, ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan"),
            MarshalAs(UnmanagedType.LPArray)] TextSpan[] pSpanInsertionPoint);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetMemberNavigationPoint(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszUniqueMemberID,
            [Out, ComAliasName("Microsoft.VisualStudio.TextManager.Interop.TextSpan"), MarshalAs(UnmanagedType.LPArray)] TextSpan[] pSpanNavPoint,
            [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")] out uint pItemID);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetMembers(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwFlags,
            out int pcMembers,
            IntPtr ppbstrDisplayNames,
            IntPtr ppbstrMemberIDs);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int OnRenamed(
            [In, ComAliasName("Microsoft.VisualStudio.TextManager.Interop.ContainedLanguageRenameType")] ContainedLanguageRenameType clrt,
            [In, MarshalAs(UnmanagedType.BStr)] string bstrOldID,
            [In, MarshalAs(UnmanagedType.BStr)] string bstrNewID);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int IsValidID(
            [In, MarshalAs(UnmanagedType.BStr)] string bstrID,
            out bool pfIsValidID);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetBaseClassName(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrBaseClassName);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetEventHandlerMemberID(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectTypeName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszEventHandlerName,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrUniqueMemberID);

        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int GetCompatibleEventHandlers(
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszClassName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszObjectTypeName,
            [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.LPCWSTR"), MarshalAs(UnmanagedType.LPWStr)] string pszNameOfEvent,
            out int pcMembers,
            IntPtr ppbstrEventHandlerNames,
            IntPtr ppbstrMemberIDs);
    }
}
