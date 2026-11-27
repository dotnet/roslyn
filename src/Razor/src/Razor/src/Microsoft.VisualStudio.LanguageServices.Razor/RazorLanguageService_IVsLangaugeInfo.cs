// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Razor;

[Guid(RazorConstants.RazorLanguageServiceString)]
internal partial class RazorLanguageService : IVsLanguageInfo
{
    public int GetLanguageName(out string bstrName)
    {
        bstrName = "Razor";
        return VSConstants.S_OK;
    }

    public int GetFileExtensions(out string? pbstrExtensions)
    {
        pbstrExtensions = default;
        return VSConstants.E_NOTIMPL;
    }

    public int GetColorizer(IVsTextLines pBuffer, out IVsColorizer? ppColorizer)
    {
        ppColorizer = default;
        return VSConstants.E_NOTIMPL;
    }

    public int GetCodeWindowManager(IVsCodeWindow pCodeWin, out IVsCodeWindowManager? ppCodeWinMgr)
    {
        ppCodeWinMgr = default;
        return VSConstants.E_NOTIMPL;
    }
}
