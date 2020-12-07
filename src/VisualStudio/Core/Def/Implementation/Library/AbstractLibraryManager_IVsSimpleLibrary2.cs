// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
    internal partial class AbstractLibraryManager : IVsSimpleLibrary2
    {
        public abstract uint GetLibraryFlags();
        protected abstract uint GetSupportedCategoryFields(uint category);
        protected abstract IVsSimpleObjectList2 GetList(uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch);
        protected abstract uint GetUpdateCounter();

        protected virtual int CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo)
        {
            ppNavInfo = null;

            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleLibrary2.AddBrowseContainer(VSCOMPONENTSELECTORDATA[] pcdComponent, ref uint pgrfOptions, out string pbstrComponentAdded)
        {
            pbstrComponentAdded = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleLibrary2.CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo)
            => CreateNavInfo(rgSymbolNodes, ulcNodes, out ppNavInfo);

        int IVsSimpleLibrary2.GetBrowseContainersForHierarchy(IVsHierarchy pHierarchy, uint celt, VSBROWSECONTAINER[] rgBrowseContainers, uint[] pcActual)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleLibrary2.GetGuid(out Guid pguidLib)
        {
            pguidLib = this.LibraryGuid;
            return VSConstants.S_OK;
        }

        int IVsSimpleLibrary2.GetLibFlags2(out uint pgrfFlags)
        {
            pgrfFlags = GetLibraryFlags();
            return VSConstants.S_OK;
        }

        int IVsSimpleLibrary2.GetList2(uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2)
        {
            ppIVsSimpleObjectList2 = GetList(listType, flags, pobSrch);

            return ppIVsSimpleObjectList2 != null
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }

        int IVsSimpleLibrary2.GetSeparatorStringWithOwnership(out string pbstrSeparator)
        {
            pbstrSeparator = ".";
            return VSConstants.S_OK;
        }

        int IVsSimpleLibrary2.GetSupportedCategoryFields2(int category, out uint pgrfCatField)
        {
            pgrfCatField = GetSupportedCategoryFields((uint)category);
            return VSConstants.S_OK;
        }

        int IVsSimpleLibrary2.LoadState(IStream pIStream, LIB_PERSISTTYPE lptType)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleLibrary2.RemoveBrowseContainer(uint dwReserved, string pszLibName)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleLibrary2.SaveState(IStream pIStream, LIB_PERSISTTYPE lptType)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleLibrary2.UpdateCounter(out uint pCurUpdate)
        {
            pCurUpdate = GetUpdateCounter();
            return VSConstants.E_NOTIMPL;
        }
    }
}
