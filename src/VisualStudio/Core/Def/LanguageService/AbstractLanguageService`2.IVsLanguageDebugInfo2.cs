// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageDebugInfo2
{
    int IVsLanguageDebugInfo2.QueryCatchLineSpan(IVsTextBuffer pBuffer, int iLine, int iCol, out int pfIsInCatch, TextSpan[] ptsCatchLine)
        => throw new NotImplementedException();

    int IVsLanguageDebugInfo2.QueryCommonLanguageBlock(IVsTextBuffer pBuffer, int iLine, int iCol, uint dwFlag, out int pfInBlock)
        => throw new NotImplementedException();

    int IVsLanguageDebugInfo2.ValidateInstructionpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
        => VSConstants.E_NOTIMPL;
}
