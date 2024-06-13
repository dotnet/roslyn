// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageInfo
{
    public int GetCodeWindowManager(IVsCodeWindow pCodeWin, out IVsCodeWindowManager ppCodeWinMgr)
    {
        ppCodeWinMgr = new VsCodeWindowManager((TLanguageService)this, pCodeWin);

        return VSConstants.S_OK;
    }

    public int GetColorizer(IVsTextLines pBuffer, out IVsColorizer ppColorizer)
    {
        ppColorizer = null;

        return VSConstants.E_NOTIMPL;
    }

    public int GetFileExtensions(out string pbstrExtensions)
    {
        pbstrExtensions = null;

        return VSConstants.E_NOTIMPL;
    }

    public int GetLanguageName(out string bstrName)
    {
        bstrName = LanguageName;

        return VSConstants.S_OK;
    }
}
