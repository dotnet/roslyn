// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal partial class AbstractLibraryManager : IVsLibrary
{
    int IVsLibrary.AddBrowseContainer(VSCOMPONENTSELECTORDATA[] pcdComponent, ref uint pgrfOptions, out string pbstrComponentAdded)
    {
        pbstrComponentAdded = null;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary.GetBrowseContainersForHierarchy(IVsHierarchy pHierarchy, uint celt, VSBROWSECONTAINER[] rgBrowseContainers, uint[] pcActual)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary.GetGuid(out Guid ppguidLib)
    {
        ppguidLib = Guid.Empty;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary.GetLibFlags(out uint pfFlags)
    {
        pfFlags = 0;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary.GetLibList(LIB_PERSISTTYPE lptType, out IVsLiteTreeList pplist)
    {
        pplist = null;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary.GetList(uint listType, uint flags, VSOBSEARCHCRITERIA[] pobSrch, out IVsObjectList pplist)
    {
        pplist = null;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary.GetSeparatorString(string[] pszSeparator)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary.GetSupportedCategoryFields(LIB_CATEGORY category, out uint pCatField)
    {
        pCatField = 0;
        return VSConstants.E_NOTIMPL;
    }

    int IVsLibrary.LoadState(Microsoft.VisualStudio.OLE.Interop.IStream pIStream, LIB_PERSISTTYPE lptType)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary.RemoveBrowseContainer(uint dwReserved, string pszLibName)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary.SaveState(Microsoft.VisualStudio.OLE.Interop.IStream pIStream, LIB_PERSISTTYPE lptType)
        => VSConstants.E_NOTIMPL;

    int IVsLibrary.UpdateCounter(out uint pCurUpdate)
    {
        pCurUpdate = 0;
        return VSConstants.E_NOTIMPL;
    }
}
