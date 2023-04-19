// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
    internal abstract class AbstractObjectList<TLibraryManager> : IVsCoTaskMemFreeMyStrings, IVsSimpleObjectList2, IVsBrowseContainersList
        where TLibraryManager : AbstractLibraryManager
    {
        protected readonly TLibraryManager LibraryManager;

        protected AbstractObjectList(TLibraryManager libraryManager)
            => this.LibraryManager = libraryManager;

        protected abstract bool CanGoToSource(uint index, VSOBJGOTOSRCTYPE srcType);
        protected abstract bool TryGetCategoryField(uint index, int category, out uint categoryField);
        protected abstract void GetDisplayData(uint index, ref VSTREEDISPLAYDATA data);
        protected abstract bool GetExpandable(uint index, uint listTypeExcluded);
        protected abstract uint GetItemCount();
        protected abstract IVsSimpleObjectList2 GetList(uint index, uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch);
        protected abstract string GetText(uint index, VSTREETEXTOPTIONS tto);
        protected abstract string GetTipText(uint index, VSTREETOOLTIPTYPE eTipType);
        protected abstract Task GoToSourceAsync(uint index, VSOBJGOTOSRCTYPE srcType);
        protected abstract uint GetUpdateCounter();

        protected virtual bool SupportsBrowseContainers
        {
            get { return false; }
        }

        protected virtual bool TryGetBrowseContainerData(uint index, ref VSCOMPONENTSELECTORDATA data)
            => false;

        protected virtual bool TryFindBrowseContainer(VSCOMPONENTSELECTORDATA data, out uint index)
        {
            index = 0;
            return false;
        }

        protected virtual bool TryGetCapabilities(out uint capabilities)
        {
            capabilities = 0;
            return false;
        }

        protected virtual bool TryGetContextMenu(uint index, out Guid menuGuid, out int menuId)
        {
            menuGuid = Guid.Empty;
            menuId = 0;
            return false;
        }

        protected virtual bool TryGetProperty(uint index, _VSOBJLISTELEMPROPID propertyId, out object pvar)
        {
            pvar = null;
            return false;
        }

        protected virtual bool TryCountSourceItems(uint index, out IVsHierarchy hierarchy, out uint itemid, out uint items)
        {
            hierarchy = null;
            itemid = 0;
            items = 0;
            return false;
        }

        protected virtual object GetBrowseObject(uint index)
            => null;

        protected virtual bool SupportsNavInfo
        {
            get { return false; }
        }

        protected virtual IVsNavInfo GetNavInfo(uint index)
            => null;

        protected virtual IVsNavInfoNode GetNavInfoNode(uint index)
            => null;

        protected virtual bool TryLocateNavInfoNode(IVsNavInfoNode pNavInfoNode, out uint index)
        {
            index = 0;
            return false;
        }

        protected virtual bool SupportsDescription
        {
            get { return false; }
        }

        protected virtual bool TryFillDescription(uint index, _VSOBJDESCOPTIONS options, IVsObjectBrowserDescription3 description)
            => false;

        int IVsSimpleObjectList2.CanDelete(uint index, out int pfOK)
        {
            pfOK = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.CanGoToSource(uint index, VSOBJGOTOSRCTYPE srcType, out int pfOK)
        {
            pfOK = CanGoToSource(index, srcType) ? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.CanRename(uint index, string pszNewName, out int pfOK)
        {
            pfOK = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.CountSourceItems(uint index, out IVsHierarchy ppHier, out uint pItemid, out uint pcItems)
        {
            if (TryCountSourceItems(index, out ppHier, out pItemid, out pcItems))
            {
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsSimpleObjectList2.DoDelete(uint index, uint grfFlags)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.DoDragDrop(uint index, IDataObject pDataObject, uint grfKeyState, ref uint pdwEffect)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.DoRename(uint index, string pszNewName, uint grfFlags)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.EnumClipboardFormats(uint index, uint grfFlags, uint celt, VSOBJCLIPFORMAT[] rgcfFormats, uint[] pcActual)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.FillDescription2(uint index, uint grfOptions, IVsObjectBrowserDescription3 pobDesc)
        {
            if (!SupportsDescription)
            {
                return VSConstants.E_NOTIMPL;
            }

            return TryFillDescription(index, (_VSOBJDESCOPTIONS)grfOptions, pobDesc)
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }

        int IVsSimpleObjectList2.GetBrowseObject(uint index, out object ppdispBrowseObj)
        {
            ppdispBrowseObj = GetBrowseObject(index);

            return ppdispBrowseObj != null
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetCapabilities2(out uint pgrfCapabilities)
        {
            return TryGetCapabilities(out pgrfCapabilities)
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetCategoryField2(uint index, int category, out uint pfCatField)
        {
            return TryGetCategoryField(index, category, out pfCatField)
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetClipboardFormat(uint index, uint grfFlags, FORMATETC[] pFormatetc, STGMEDIUM[] pMedium)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.GetContextMenu(uint index, out Guid pclsidActive, out int pnMenuId, out IOleCommandTarget ppCmdTrgtActive)
        {
            if (TryGetContextMenu(index, out pclsidActive, out pnMenuId))
            {
                ppCmdTrgtActive = this.LibraryManager;
                return VSConstants.S_OK;
            }

            ppCmdTrgtActive = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetDisplayData(uint index, VSTREEDISPLAYDATA[] pData)
        {
            if (index >= GetItemCount())
            {
                return VSConstants.E_INVALIDARG;
            }

            pData[0].hImageList = this.LibraryManager.ImageListPtr;
            GetDisplayData(index, ref pData[0]);

            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetExpandable3(uint index, uint listTypeExcluded, out int pfExpandable)
        {
            pfExpandable = GetExpandable(index, listTypeExcluded) ? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetExtendedClipboardVariant(uint index, uint grfFlags, VSOBJCLIPFORMAT[] pcfFormat, out object pvarFormat)
        {
            pvarFormat = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetFlags(out uint pFlags)
        {
            pFlags = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetItemCount(out uint pCount)
        {
            pCount = GetItemCount();
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetList2(uint index, uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2)
        {
            ppIVsSimpleObjectList2 = GetList(index, listType, flags, pobSrch);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetMultipleSourceItems(uint index, uint grfGSI, uint cItems, VSITEMSELECTION[] rgItemSel)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.GetNavInfo(uint index, out IVsNavInfo ppNavInfo)
        {
            if (!SupportsNavInfo)
            {
                ppNavInfo = null;
                return VSConstants.E_NOTIMPL;
            }

            ppNavInfo = GetNavInfo(index);
            return ppNavInfo != null
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }

        int IVsSimpleObjectList2.GetNavInfoNode(uint index, out IVsNavInfoNode ppNavInfoNode)
        {
            ppNavInfoNode = GetNavInfoNode(index);

            return ppNavInfoNode != null
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetProperty(uint index, int propid, out object pvar)
        {
            if (TryGetProperty(index, (_VSOBJLISTELEMPROPID)propid, out pvar))
            {
                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        int IVsSimpleObjectList2.GetSourceContextWithOwnership(uint index, out string pbstrFilename, out uint pulLineNum)
        {
            pbstrFilename = null;
            pulLineNum = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetTextWithOwnership(uint index, VSTREETEXTOPTIONS tto, out string pbstrText)
        {
            if (index >= GetItemCount())
            {
                pbstrText = null;
                return VSConstants.E_INVALIDARG;
            }

            pbstrText = GetText(index, tto);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetTipTextWithOwnership(uint index, VSTREETOOLTIPTYPE eTipType, out string pbstrText)
        {
            if (index >= GetItemCount())
            {
                pbstrText = null;
                return VSConstants.E_INVALIDARG;
            }

            pbstrText = GetTipText(index, eTipType);

            return pbstrText != null
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetUserContext(uint index, out object ppunkUserCtx)
        {
            ppunkUserCtx = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GoToSource(uint index, VSOBJGOTOSRCTYPE srcType)
        {
            if (index >= GetItemCount())
                return VSConstants.E_INVALIDARG;

            // Fire and forget
            _ = GoToSourceAsync(index, srcType);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.LocateNavInfoNode(IVsNavInfoNode pNavInfoNode, out uint pulIndex)
        {
            if (!SupportsNavInfo)
            {
                pulIndex = 0;
                return VSConstants.E_NOTIMPL;
            }

            return TryLocateNavInfoNode(pNavInfoNode, out pulIndex)
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }

        int IVsSimpleObjectList2.OnClose(VSTREECLOSEACTIONS[] ptca)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.QueryDragDrop(uint index, IDataObject pDataObject, uint grfKeyState, ref uint pdwEffect)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.ShowHelp(uint index)
            => VSConstants.E_NOTIMPL;

        int IVsSimpleObjectList2.UpdateCounter(out uint pCurUpdate)
        {
            pCurUpdate = GetUpdateCounter();
            return VSConstants.S_OK;
        }

        int IVsBrowseContainersList.GetContainerData(uint ulIndex, VSCOMPONENTSELECTORDATA[] pData)
        {
            if (!SupportsBrowseContainers)
            {
                return VSConstants.E_NOTIMPL;
            }

            if (ulIndex >= GetItemCount())
            {
                return VSConstants.E_INVALIDARG;
            }

            if (pData == null || pData.Length != 1)
            {
                return VSConstants.E_INVALIDARG;
            }

            pData[0] = new VSCOMPONENTSELECTORDATA();
            pData[0].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORDATA));

            return TryGetBrowseContainerData(ulIndex, ref pData[0])
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }

        int IVsBrowseContainersList.FindContainer(VSCOMPONENTSELECTORDATA[] pData, out uint pulIndex)
        {
            pulIndex = 0;

            if (!SupportsBrowseContainers)
            {
                return VSConstants.E_NOTIMPL;
            }

            if (pData == null || pData.Length != 1)
            {
                return VSConstants.E_INVALIDARG;
            }

            return TryFindBrowseContainer(pData[0], out pulIndex)
                ? VSConstants.S_OK
                : VSConstants.E_FAIL;
        }
    }
}
