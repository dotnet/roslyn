﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;
using IVsEnumBSTR = Microsoft.VisualStudio.TextManager.Interop.IVsEnumBSTR;
using IVsTextBuffer = Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageDebugInfo
{
    int IVsLanguageDebugInfo.GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
    {
        try
        {
            return _languageDebugInfo.GetLanguageID(pBuffer, iLine, iCol, out pguidLanguageID);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    int IVsLanguageDebugInfo.GetLocationOfName(string pszName, out string pbstrMkDoc, out TextSpan pspanLocation)
    {
        try
        {
            return _languageDebugInfo.GetLocationOfName(pszName, out pbstrMkDoc, out pspanLocation);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    int IVsLanguageDebugInfo.GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string pbstrName, out int piLineOffset)
    {
        try
        {
            return _languageDebugInfo.GetNameOfLocation(pBuffer, iLine, iCol, out pbstrName, out piLineOffset);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    int IVsLanguageDebugInfo.GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
    {
        try
        {
            return _languageDebugInfo.GetProximityExpressions(pBuffer, iLine, iCol, cLines, out ppEnum);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    int IVsLanguageDebugInfo.IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
    {
        try
        {
            return _languageDebugInfo.IsMappedLocation(pBuffer, iLine, iCol);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    int IVsLanguageDebugInfo.ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName ppNames)
    {
        try
        {
            return _languageDebugInfo.ResolveName(pszName, dwFlags, out ppNames);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    int IVsLanguageDebugInfo.ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
    {
        try
        {
            return _languageDebugInfo.ValidateBreakpointLocation(pBuffer, iLine, iCol, pCodeSpan);
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
