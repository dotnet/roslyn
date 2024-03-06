// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal partial class AbstractLibraryManager : IVsLibraryMgr
{
    int IVsLibraryMgr.GetCheckAt(uint nLibIndex, LIB_CHECKSTATE[] pstate)
    {
        if (nLibIndex != 0)
        {
            return VSConstants.E_INVALIDARG;
        }

        throw new NotImplementedException();
    }

    int IVsLibraryMgr.GetCount(out uint pnCount)
    {
        pnCount = 1;
        return VSConstants.S_OK;
    }

    int IVsLibraryMgr.GetLibraryAt(uint nLibIndex, out IVsLibrary ppLibrary)
    {
        if (nLibIndex != 0)
        {
            ppLibrary = null;
            return VSConstants.E_INVALIDARG;
        }

        ppLibrary = this;
        return VSConstants.S_OK;
    }

    int IVsLibraryMgr.GetNameAt(uint nLibIndex, IntPtr pszName)
    {
        if (nLibIndex != 0)
        {
            return VSConstants.E_INVALIDARG;
        }

        throw new NotImplementedException();
    }

    int IVsLibraryMgr.SetLibraryGroupEnabled(LIB_PERSISTTYPE lpt, int fEnable)
        => VSConstants.E_NOTIMPL;

    int IVsLibraryMgr.ToggleCheckAt(uint nLibIndex)
    {
        if (nLibIndex != 0)
        {
            return VSConstants.E_INVALIDARG;
        }

        throw new NotImplementedException();
    }
}
