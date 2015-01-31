// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ErrorListDiagnosticsPackage
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:StatementMustNotUseUnnecessaryParenthesis", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    public class MyTaskProvider : IVsTaskProvider2, IVsTaskProvider3, IVsTaskProvider4
    {
        public readonly List<IVsTaskItem> Items = new List<IVsTaskItem>();

        public readonly Guid ProviderGuid = new Guid();

        public MyTaskProvider()
        {
        }

        public void ClearItems()
        {
            this.Items.Clear();
        }

        public void AddItems(IEnumerable<IVsTaskItem> items)
        {
            this.Items.AddRange(items);
        }

        public void AddItems(params IVsTaskItem[] items)
        {
            this.Items.AddRange(items);
        }

        #region IVsTaskProvider/IVsTaskProvider2 members
        public int EnumTaskItems(out IVsEnumTaskItems ppenum)
        {
            ppenum = new MyEnumTaskItems(this.Items);
            return VSConstants.S_OK;
        }

        public int ImageList(out IntPtr phImageList)
        {
            phImageList = IntPtr.Zero;
            return VSConstants.S_OK;
        }

        public int OnTaskListFinalRelease(IVsTaskList pTaskList)
        {
            return VSConstants.S_OK;
        }

        public int ReRegistrationKey(out string pbstrKey)
        {
            pbstrKey = "MyProvider";
            return VSConstants.S_OK;
        }

        public int SubcategoryList(uint cbstr, string[] rgbstr, out uint pcActual)
        {
            pcActual = 2;

            if ((rgbstr != null) && (rgbstr.Length > 2))
            {
                rgbstr[0] = "red";
                rgbstr[1] = "green";
            }

            return VSConstants.S_OK;
        }
        #endregion

        #region IVsTaskProvider2 members
        int IVsTaskProvider2.MaintainInitialTaskOrder(out int bMaintainOrder)
        {
            bMaintainOrder = 0;
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsTaskProvider3 members
        int IVsTaskProvider3.GetColumn(int iColumn, VSTASKCOLUMN[] pColumn)
        {
            if (iColumn == 0)
            {
                VSTASKCOLUMN c = new VSTASKCOLUMN();
                c.bstrCanonicalName = "Test";
                c.bstrHeading = "header";
                c.bstrLocalizedName = "localized name";
                c.bstrTip = "tip";
                c.cxDefaultWidth = 200;
                c.cxMinWidth = 100;
                c.fAllowHide = 1;
                c.fAllowUserSort = 1;
                c.fDescendingSort = 1;
                c.fDynamicSize = 1;
                c.fFitContent = 1;
                c.fMoveable = 1;
                c.fShowSortArrow = 1;
                c.fSizeable = 1;
                c.fVisibleByDefault = 1;
                c.iField = 0;
                c.iImage = 0;

                pColumn[0] = c;
                return VSConstants.S_OK;
            }
            else
            {
                return VSConstants.E_INVALIDARG;
            }
        }

        int IVsTaskProvider3.GetColumnCount(out int pnColumns)
        {
            pnColumns = 1;
            return VSConstants.S_OK;
        }

        int IVsTaskProvider3.GetProviderFlags(out uint tpfFlags)
        {
            tpfFlags = (uint)(__VSTASKPROVIDERFLAGS.TPF_ALWAYSVISIBLE);
            return VSConstants.S_OK;
        }

        int IVsTaskProvider3.GetProviderGuid(out Guid pguidProvider)
        {
            pguidProvider = this.ProviderGuid;
            return VSConstants.S_OK;
        }

        int IVsTaskProvider3.GetProviderName(out string pbstrName)
        {
            // TODO: testing
            pbstrName = "test123";
            return VSConstants.S_OK;
        }

        int IVsTaskProvider3.GetProviderToolbar(out Guid pguidGroup, out uint pdwID)
        {
            pguidGroup = Guid.Empty;
            pdwID = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskProvider3.GetSurrogateProviderGuid(out Guid pguidProvider)
        {
            pguidProvider = Guid.Empty;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskProvider3.OnBeginTaskEdit(IVsTaskItem pItem)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskProvider3.OnEndTaskEdit(IVsTaskItem pItem, int fCommitChanges, out int pfAllowChanges)
        {
            pfAllowChanges = 0;
            return VSConstants.E_NOTIMPL;
        }
        #endregion

        #region IVsTaskProvider3 members
        IntPtr IVsTaskProvider4.ThemedImageList
        {
            get
            {
                return IntPtr.Zero;
            }
        }
        #endregion
    }
}
