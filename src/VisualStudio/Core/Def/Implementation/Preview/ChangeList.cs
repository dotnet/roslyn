// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal partial class ChangeList : IVsPreviewChangesList, IVsLiteTreeList
    {
        public readonly static ChangeList Empty = new ChangeList(Array.Empty<AbstractChange>());

        internal AbstractChange[] Changes { get; }

        public ChangeList(AbstractChange[] changes)
        {
            this.Changes = changes;
        }

        public int GetDisplayData(uint index, VSTREEDISPLAYDATA[] pData)
        {
            pData[0].Mask = (uint)_VSTREEDISPLAYMASK.TDM_STATE | (uint)_VSTREEDISPLAYMASK.TDM_IMAGE | (uint)_VSTREEDISPLAYMASK.TDM_SELECTEDIMAGE;

            // Set TDS_SELECTED and TDS_GRAYTEXT
            pData[0].State = Changes[index].GetDisplayState();

            Changes[index].GetDisplayData(pData);
            return VSConstants.S_OK;
        }

        public int GetExpandable(uint index, out int pfExpandable)
        {
            pfExpandable = Changes[index].IsExpandable;
            return VSConstants.S_OK;
        }

        public int GetExpandedList(uint index, out int pfCanRecurse, out IVsLiteTreeList pptlNode)
        {
            pfCanRecurse = Changes[index].CanRecurse;
            pptlNode = (IVsLiteTreeList)Changes[index].GetChildren();
            return VSConstants.S_OK;
        }

        public int GetFlags(out uint pFlags)
        {
            // The interface IVsSimplePreviewChangesList doesn't include this method.
            // Setting flags to 0 is necessary to make the underlying treeview draw
            // checkboxes and make them clickable.
            pFlags = 0;
            return VSConstants.S_OK;
        }

        public int GetItemCount(out uint pCount)
        {
            pCount = (uint)Changes.Length;
            return VSConstants.S_OK;
        }

        public int GetListChanges(ref uint pcChanges, VSTREELISTITEMCHANGE[] prgListChanges)
        {
            return VSConstants.E_FAIL;
        }

        public int GetText(uint index, VSTREETEXTOPTIONS tto, out string ppszText)
        {
            return Changes[index].GetText(out tto, out ppszText);
        }

        public int GetTipText(uint index, VSTREETOOLTIPTYPE eTipType, out string ppszText)
        {
            return Changes[index].GetTipText(out eTipType, out ppszText);
        }

        public int LocateExpandedList(IVsLiteTreeList child, out uint iIndex)
        {
            for (var i = 0; i < Changes.Length; i++)
            {
                if (Changes[i].GetChildren() == child)
                {
                    iIndex = (uint)i;
                    return VSConstants.S_OK;
                }
            }

            iIndex = 0;
            return VSConstants.S_FALSE;
        }

        public int OnClose(VSTREECLOSEACTIONS[] ptca)
        {
            return VSConstants.S_OK;
        }

        public int OnRequestSource(uint index, object pIUnknownTextView)
        {
            return Changes[index].OnRequestSource(pIUnknownTextView);
        }

        public int ToggleState(uint index, out uint ptscr)
        {
            Changes[index].Toggle();
            ptscr = (uint)_VSTREESTATECHANGEREFRESH.TSCR_ENTIRE;

            Changes[index].OnRequestSource(null);
            return VSConstants.S_OK;
        }

        public int UpdateCounter(out uint pCurUpdate, out uint pgrfChanges)
        {
            pCurUpdate = 0;
            pgrfChanges = 0;
            return VSConstants.S_OK;
        }
    }
}
