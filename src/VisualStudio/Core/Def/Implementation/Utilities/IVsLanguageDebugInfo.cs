// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    [ComImport]
    [Guid("F30A6A07-5340-4C0E-B312-5772558B0E63")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsLanguageDebugInfo
    {
        [PreserveSig]
        int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum);

        [PreserveSig]
        int ValidateBreakpointLocation(
            IVsTextBuffer pBuffer,
            int iLine,
            int iCol,
            [In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct)] TextSpan[] pCodeSpan);

        [PreserveSig]
        int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, [MarshalAs(UnmanagedType.BStr)] out string pbstrName, out int piLineOffset);

        [PreserveSig]
        int GetLocationOfName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            [MarshalAs(UnmanagedType.BStr)] out string pbstrMkDoc,
            out TextSpan pspanLocation);

        [PreserveSig]
        int ResolveName([MarshalAs(UnmanagedType.LPWStr)] string pszName, uint dwFlags, out IVsEnumDebugName ppNames);

        [PreserveSig]
        int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID);

        [PreserveSig]
        int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol);
    }
}
