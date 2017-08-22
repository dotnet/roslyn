// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using IVsEnumBSTR = Microsoft.VisualStudio.TextManager.Interop.IVsEnumBSTR;
using IVsTextBuffer = Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageDebugInfo
    {
        int IVsLanguageDebugInfo.GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
        {
            try
            {
                return LanguageDebugInfo.GetLanguageID(pBuffer, iLine, iCol, out pguidLanguageID);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                pguidLanguageID = default;
                return e.HResult;
            }
        }

        int IVsLanguageDebugInfo.GetLocationOfName(string pszName, out string pbstrMkDoc, out TextSpan pspanLocation)
        {
            try
            {
                return LanguageDebugInfo.GetLocationOfName(pszName, out pbstrMkDoc, out pspanLocation);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                pbstrMkDoc = null;
                pspanLocation = default;
                return e.HResult;
            }
        }

        int IVsLanguageDebugInfo.GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string pbstrName, out int piLineOffset)
        {
            try
            {
                return LanguageDebugInfo.GetNameOfLocation(pBuffer, iLine, iCol, out pbstrName, out piLineOffset);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                pbstrName = null;
                piLineOffset = 0;
                return e.HResult;
            }
        }

        int IVsLanguageDebugInfo.GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
        {
            try
            {
                return LanguageDebugInfo.GetProximityExpressions(pBuffer, iLine, iCol, cLines, out ppEnum);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                ppEnum = default;
                return e.HResult;
            }
        }

        int IVsLanguageDebugInfo.IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
        {
            try
            {
                return LanguageDebugInfo.IsMappedLocation(pBuffer, iLine, iCol);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                return e.HResult;
            }
        }

        int IVsLanguageDebugInfo.ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName ppNames)
        {
            try
            {
                return LanguageDebugInfo.ResolveName(pszName, dwFlags, out ppNames);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                ppNames = default;
                return e.HResult;
            }
        }

        int IVsLanguageDebugInfo.ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
        {
            try
            {
                return LanguageDebugInfo.ValidateBreakpointLocation(pBuffer, iLine, iCol, pCodeSpan);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                return e.HResult;
            }
        }
    }
}
