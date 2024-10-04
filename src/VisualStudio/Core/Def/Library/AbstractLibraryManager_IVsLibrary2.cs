// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal partial class AbstractLibraryManager : IVsLibrary2
{
    int IVsLibrary2.AddBrowseContainer(VSCOMPONENTSELECTORDATA[] pcdComponent, ref uint pgrfOptions, string[] pbstrComponentAdded)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary2.CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo)
    {
        ppNavInfo = null;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary2.GetBrowseContainersForHierarchy(IVsHierarchy pHierarchy, uint celt, VSBROWSECONTAINER[] rgBrowseContainers, uint[] pcActual)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary2.GetGuid(out IntPtr ppguidLib)
    {
        ppguidLib = IntPtr.Zero;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary2.GetLibFlags2(out uint pgrfFlags)
    {
        pgrfFlags = 0;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary2.GetLibList(LIB_PERSISTTYPE lptType, out IVsLiteTreeList ppList)
    {
        ppList = null;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary2.GetList2(uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsObjectList2 ppIVsObjectList2)
    {
        ppIVsObjectList2 = null;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary2.GetSeparatorString(IntPtr pszSeparator)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary2.GetSupportedCategoryFields2(int category, out uint pgrfCatField)
    {
        pgrfCatField = 0;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary2.LoadState(IStream pIStream, LIB_PERSISTTYPE lptType)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary2.RemoveBrowseContainer(uint dwReserved, string pszLibName)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary2.SaveState(IStream pIStream, LIB_PERSISTTYPE lptType)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary2.UpdateCounter(out uint pCurUpdate)
    {
        pCurUpdate = 0;
        return VSConstants.E_NOTIMPL;
    }
}
