// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal abstract class AbstractObjectList<TLibraryManager> : IVsCoTaskMemFreeMyStrings, IVsSimpleObjectList2, IVsBrowseContainersList
    where TLibraryManager : AbstractLibraryManager
{
    protected readonly TLibraryManager LibraryManager;

    protected AbstractObjectList(TLibraryManager libraryManager)
        => this.LibraryManager = libraryManager;

    protected abstract bool CanGoToSource(uint index, VSOBJGOTOSRCTYPE srcType);
    protected abstract bool TryGetCategoryField(uint index, int category, out uint categoryField);
    protected abstract void GetDisplayData(uint index, ref VSTREEDISPLAYDATA data);
    protected abstract Task<bool> GetExpandableAsync(uint index, uint listTypeExcluded, CancellationToken cancellationToken);
    protected abstract uint GetItemCount();
    protected abstract Task<IVsSimpleObjectList2> GetListAsync(
        uint index, uint listType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, CancellationToken cancellationToken);
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

    protected virtual async Task<(bool success, object pvar)> TryGetPropertyAsync(uint index, _VSOBJLISTELEMPROPID propertyId, CancellationToken cancellationToken)
        => default((bool success, object pvar));

    protected virtual bool TryCountSourceItems(uint index, out IVsHierarchy hierarchy, out uint itemid, out uint items)
    {
        hierarchy = null;
        itemid = 0;
        items = 0;
        return false;
    }

    protected virtual async Task<object> GetBrowseObjectAsync(uint index, CancellationToken cancellationToken)
        => null;

    protected virtual bool SupportsNavInfo
    {
        get { return false; }
    }

    protected virtual async Task<IVsNavInfo> GetNavInfoAsync(uint index, CancellationToken cancellationToken)
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

    protected virtual async Task<bool> TryFillDescriptionAsync(uint index, _VSOBJDESCOPTIONS options, IVsObjectBrowserDescription3 description, CancellationToken cancellationToken)
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
            return VSConstants.E_NOTIMPL;

        var result = this.LibraryManager.ThreadingContext.JoinableTaskFactory.Run(
                () => TryFillDescriptionAsync(index, (_VSOBJDESCOPTIONS)grfOptions, pobDesc, CancellationToken.None));
        return result
            ? VSConstants.S_OK
            : VSConstants.E_FAIL;
    }

    int IVsSimpleObjectList2.GetBrowseObject(uint index, out object ppdispBrowseObj)
    {
        ppdispBrowseObj = this.LibraryManager.ThreadingContext.JoinableTaskFactory.Run(() =>
            GetBrowseObjectAsync(index, CancellationToken.None));

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

        // Just return zero for the hImageList, which allows the object browser to use the default image list with no DPI issues.
        pData[0].hImageList = IntPtr.Zero;
        GetDisplayData(index, ref pData[0]);

        return VSConstants.S_OK;
    }

    int IVsSimpleObjectList2.GetExpandable3(uint index, uint listTypeExcluded, out int pfExpandable)
    {
        pfExpandable = this.LibraryManager.ThreadingContext.JoinableTaskFactory.Run(
            () => GetExpandableAsync(index, listTypeExcluded, CancellationToken.None)) ? 1 : 0;
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
        ppIVsSimpleObjectList2 = this.LibraryManager.ThreadingContext.JoinableTaskFactory.Run(
            () => GetListAsync(index, listType, flags, pobSrch, CancellationToken.None));
        return VSConstants.S_OK;
    }

    int IVsSimpleObjectList2.GetMultipleSourceItems(uint index, uint grfGSI, uint cItems, VSITEMSELECTION[] rgItemSel)
        => VSConstants.E_NOTIMPL;

    int IVsSimpleObjectList2.GetNavInfo(uint index, out IVsNavInfo ppNavInfo)
    {
        (ppNavInfo, var result) = this.LibraryManager.ThreadingContext.JoinableTaskFactory.Run(async () =>
        {
            if (!SupportsNavInfo)
                return (null, VSConstants.E_NOTIMPL);

            var ppNavInfo = await GetNavInfoAsync(index, CancellationToken.None).ConfigureAwait(true);
            return (ppNavInfo, ppNavInfo != null
                ? VSConstants.S_OK
                : VSConstants.E_FAIL);
        });

        return result;
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
        var (success, obj) = this.LibraryManager.ThreadingContext.JoinableTaskFactory.Run(() =>
            TryGetPropertyAsync(index, (_VSOBJLISTELEMPROPID)propid, CancellationToken.None));
        if (success)
        {
            pvar = obj;
            return VSConstants.S_OK;
        }

        pvar = null;
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
        var asynchronousOperationListener = LibraryManager.ComponentModel.GetService<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.LibraryManager);
        var asyncToken = asynchronousOperationListener.BeginAsyncOperation(nameof(IVsSimpleObjectList2) + "." + nameof(IVsSimpleObjectList2.GoToSource));

        GoToSourceAsync(index, srcType).CompletesAsyncOperation(asyncToken);
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
