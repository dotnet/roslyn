// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageDebugInfo2
    {
        int IVsLanguageDebugInfo2.QueryCatchLineSpan(IVsTextBuffer pBuffer, int iLine, int iCol, out int pfIsInCatch, TextSpan[] ptsCatchLine)
        {
            throw new NotImplementedException();
        }

        int IVsLanguageDebugInfo2.QueryCommonLanguageBlock(IVsTextBuffer pBuffer, int iLine, int iCol, uint dwFlag, out int pfInBlock)
        {
            throw new NotImplementedException();
        }

        int IVsLanguageDebugInfo2.ValidateInstructionpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}
