// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
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
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsLibraryMgr.ToggleCheckAt(uint nLibIndex)
        {
            if (nLibIndex != 0)
            {
                return VSConstants.E_INVALIDARG;
            }

            throw new NotImplementedException();
        }
    }
}
