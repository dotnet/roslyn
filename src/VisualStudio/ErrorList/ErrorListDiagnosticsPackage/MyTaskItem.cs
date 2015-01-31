// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ErrorListDiagnosticsPackage
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:StatementMustNotUseUnnecessaryParenthesis", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Required by interface")]
    public class MyTaskItem : IVsTaskItem2, IVsTaskItem3, IVsErrorItem, IVsErrorItem2
    {
        private string _document;
        private int _line;
        private int _column;
        private string _text;
        private VSTASKCATEGORY _category;
        private __VSERRORCATEGORY _errorCategory;
        private Dictionary<uint, string> _customColumnText = new Dictionary<uint, string>();
        private bool _canDelete;
        private bool _isChecked;
        private bool _isReadOnly;
        private bool _hasHelp;
        private bool _customColumnsReadOnly;
        private int _imageListIndex;
        private int _subcategoryIndex;
        private VSTASKPRIORITY _priority;

        public readonly IVsTaskProvider3 Provider;

        public MyTaskItem(IVsTaskProvider3 provider,
                          string document = "", int line = 0, int column = 0, string text = "", VSTASKCATEGORY category = VSTASKCATEGORY.CAT_USER, __VSERRORCATEGORY errorCategory = __VSERRORCATEGORY.EC_ERROR,
                          IEnumerable<string> customColumnText = null,
                          bool canDelete = false, bool isChecked = false, bool isReadOnly = false, bool hasHelp = false, bool customColumnsReadOnly = false,
                          int imageListIndex = 0, int subcategoryIndex = 0,
                          VSTASKPRIORITY priority = VSTASKPRIORITY.TP_NORMAL)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            this.Provider = provider;
            _document = document;
            _line = line;
            _column = column;
            _text = text;
            _category = category;
            _errorCategory = errorCategory;

            if (customColumnText != null)
            {
                uint index = 0;
                foreach (var s in customColumnText)
                {
                    _customColumnText.Add(index++, s);
                }
            }

            _canDelete = canDelete;
            _isChecked = isChecked;
            _isReadOnly = isReadOnly;
            _hasHelp = hasHelp;
            _customColumnsReadOnly = customColumnsReadOnly;

            _imageListIndex = imageListIndex;
            _subcategoryIndex = subcategoryIndex;

            _priority = priority;
        }

        #region IVsTaskItem/IVsTaskItem2 members
        public int CanDelete([ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfCanDelete)
        {
            pfCanDelete = _canDelete ? 1 : 0;
            return VSConstants.S_OK;
        }

        public int Category([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSTASKCATEGORY")]VSTASKCATEGORY[] pCat)
        {
            pCat[0] = _category;
            return VSConstants.S_OK;
        }

        public int Column(out int piCol)
        {
            piCol = _column;
            return VSConstants.S_OK;
        }

        public int Document(out string pbstrMkDocument)
        {
            pbstrMkDocument = _document;
            return VSConstants.S_OK;
        }

        public int get_Checked([ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfChecked)
        {
            pfChecked = _isChecked ? 1 : 0;
            return VSConstants.S_OK;
        }

        public int get_Priority([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSTASKPRIORITY")]VSTASKPRIORITY[] ptpPriority)
        {
            ptpPriority[0] = _priority;
            return VSConstants.S_OK;
        }

        public int get_Text(out string pbstrName)
        {
            pbstrName = _text;
            return VSConstants.S_OK;
        }

        public int HasHelp([ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfHasHelp)
        {
            pfHasHelp = _hasHelp ? 1 : 0;
            return VSConstants.S_OK;
        }

        public int ImageListIndex(out int pIndex)
        {
            pIndex = _imageListIndex;
            return VSConstants.S_OK;
        }

        public int IsReadOnly([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSTASKFIELD")]VSTASKFIELD field, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfReadOnly)
        {
            pfReadOnly = _isReadOnly ? 1 : 0;
            return VSConstants.S_OK;
        }

        public int Line(out int piLine)
        {
            piLine = _line;
            return VSConstants.S_OK;
        }

        public int NavigateTo()
        {
            return VSConstants.S_OK;
        }

        public int NavigateToHelp()
        {
            return VSConstants.S_OK;
        }

        public int OnDeleteTask()
        {
            return VSConstants.S_OK;
        }

        public int OnFilterTask([ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]int fVisible)
        {
            return VSConstants.S_OK;
        }

        public int put_Checked([ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]int fChecked)
        {
            _isChecked = (fChecked != 0);
            return VSConstants.S_OK;
        }

        public int put_Priority([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSTASKPRIORITY")]VSTASKPRIORITY tpPriority)
        {
            _priority = tpPriority;
            return VSConstants.S_OK;
        }

        public int put_Text(string bstrName)
        {
            _text = bstrName;
            return VSConstants.S_OK;
        }

        public int SubcategoryIndex(out int pIndex)
        {
            pIndex = _subcategoryIndex;
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsTaskItem2 members
        int IVsTaskItem2.BrowseObject(out object ppObj)
        {
            ppObj = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem2.get_CustomColumnText([ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid guidView, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint iCustomColumnIndex, out string pbstrText)
        {
            if (_customColumnText.TryGetValue(iCustomColumnIndex, out pbstrText))
            {
                return VSConstants.E_NOTIMPL;
            }

            return VSConstants.E_INVALIDARG;
        }

        int IVsTaskItem2.put_CustomColumnText([ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid guidView, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint iCustomColumnIndex, string bstrText)
        {
            _customColumnText[iCustomColumnIndex] = bstrText;

            return VSConstants.E_INVALIDARG;
        }

        int IVsTaskItem2.IsCustomColumnReadOnly([ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")]ref Guid guidView, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint iCustomColumnIndex, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int pfReadOnly)
        {
            pfReadOnly = _customColumnsReadOnly ? 1 : 0;
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsTaskListItem3 members
        int IVsTaskItem3.GetColumnValue(int iField, out uint ptvtType, out uint ptvfFlags, out object pvarValue, out string pbstrAccessibilityName)
        {
            ptvtType = (uint)(__VSTASKVALUETYPE.TVT_TEXT);
            ptvfFlags = (uint)(__VSTASKVALUEFLAGS.TVF_FILENAME);
            pvarValue = @"d:\test\a.txt";
            pbstrAccessibilityName = @"d:\test\a.txt";

            return VSConstants.S_OK;
        }

        int IVsTaskItem3.GetDefaultEditField(out int piField)
        {
            piField = 0;
            return VSConstants.S_OK;
        }

        int IVsTaskItem3.GetEnumCount(int iField, out int pnValues)
        {
            pnValues = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem3.GetEnumValue(int iField, int iValue, out object pvarValue, out string pbstrAccessibilityName)
        {
            pvarValue = null;
            pbstrAccessibilityName = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem3.GetNavigationStatusText(out string pbstrText)
        {
            pbstrText = string.Empty;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem3.GetSurrogateProviderGuid(out Guid pguidProvider)
        {
            pguidProvider = Guid.Empty;
            return VSConstants.E_NOTIMPL;
        }

        int IVsTaskItem3.GetTaskName(out string pbstrName)
        {
            pbstrName = "taskname";
            return VSConstants.S_OK;
        }

        int IVsTaskItem3.GetTaskProvider(out IVsTaskProvider3 ppProvider)
        {
            ppProvider = this.Provider;
            return VSConstants.S_OK;
        }

        int IVsTaskItem3.GetTipText(int iField, out string pbstrTipText)
        {
            pbstrTipText = "tip";
            return VSConstants.S_OK;
        }

        int IVsTaskItem3.IsDirty(out int pfDirty)
        {
            pfDirty = 0;
            return VSConstants.S_OK;
        }

        int IVsTaskItem3.OnLinkClicked(int iField, int iLinkIndex)
        {
            return VSConstants.S_OK;
        }

        int IVsTaskItem3.SetColumnValue(int iField, ref object pvarValue)
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsErrorItem members
        int IVsErrorItem.GetHierarchy(out IVsHierarchy ppProject)
        {
            ppProject = null;
            return VSConstants.E_FAIL;
        }

        int IVsErrorItem.GetCategory(out uint pCategory)
        {
            pCategory = (uint)_errorCategory;
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsErrorItem2 members
        int IVsErrorItem2.GetCustomIconIndex(out int index)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
