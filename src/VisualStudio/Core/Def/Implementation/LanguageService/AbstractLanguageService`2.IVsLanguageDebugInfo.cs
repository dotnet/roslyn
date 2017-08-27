// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using IVsEnumBSTR = Microsoft.VisualStudio.TextManager.Interop.IVsEnumBSTR;
using IVsTextBuffer = Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageDebugInfo
    {
        public int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
        {
            return this.LanguageDebugInfo.GetLanguageID(pBuffer, iLine, iCol, out pguidLanguageID);
        }

        public int GetLocationOfName(string pszName, out string pbstrMkDoc, out TextSpan pspanLocation)
        {
            return this.LanguageDebugInfo.GetLocationOfName(pszName, out pbstrMkDoc, out pspanLocation);
        }

        public int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string pbstrName, out int piLineOffset)
        {
            return this.LanguageDebugInfo.GetNameOfLocation(pBuffer, iLine, iCol, out pbstrName, out piLineOffset);
        }

        public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
        {
            return this.LanguageDebugInfo.GetProximityExpressions(pBuffer, iLine, iCol, cLines, out ppEnum);
        }

        public int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
        {
            return this.LanguageDebugInfo.IsMappedLocation(pBuffer, iLine, iCol);
        }

        public int ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName ppNames)
        {
            return this.LanguageDebugInfo.ResolveName(pszName, dwFlags, out ppNames);
        }

        public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
        {
            return this.LanguageDebugInfo.ValidateBreakpointLocation(pBuffer, iLine, iCol, pCodeSpan);
        }
    }
}
