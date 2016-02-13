// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigationBar
{
    internal class NavigationBarClient :
        IVsDropdownBarClient,
        IVsDropdownBarClient3,
        IVsDropdownBarClientEx,
        IVsCoTaskMemFreeMyStrings,
        INavigationBarPresenter,
        IVsCodeWindowEvents
    {
        private readonly IVsDropdownBarManager _manager;
        private readonly IVsCodeWindow _codeWindow;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IComEventSink _codeWindowEventsSink;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IntPtr _imageList;
        private readonly IVsImageService2 _imageService;
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new Dictionary<IVsTextView, ITextView>();
        private IVsDropdownBar _dropdownBar;
        private IList<NavigationBarProjectItem> _projectItems;
        private IList<NavigationBarItem> _currentTypeItems;

        public NavigationBarClient(
            IVsDropdownBarManager manager,
            IVsCodeWindow codeWindow,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl workspace)
        {
            _manager = manager;
            _codeWindow = codeWindow;
            _workspace = workspace;
            _imageService = (IVsImageService2)serviceProvider.GetService(typeof(SVsImageService));
            _projectItems = SpecializedCollections.EmptyList<NavigationBarProjectItem>();
            _currentTypeItems = SpecializedCollections.EmptyList<NavigationBarItem>();

            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (vsShell != null)
            {
                object varImageList;
                int hresult = vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_ObjectMgrTypesImgList, out varImageList);
                if (ErrorHandler.Succeeded(hresult) && varImageList != null)
                {
                    _imageList = (IntPtr)(int)varImageList;
                }
            }

            _codeWindowEventsSink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
            _editorAdaptersFactoryService = serviceProvider.GetMefService<IVsEditorAdaptersFactoryService>();

            IVsTextView pTextView;
            codeWindow.GetPrimaryView(out pTextView);
            StartTrackingView(pTextView);

            pTextView = null;
            codeWindow.GetSecondaryView(out pTextView);
            StartTrackingView(pTextView);
        }

        private void StartTrackingView(IVsTextView pTextView)
        {
            if (pTextView != null)
            {
                var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(pTextView);

                if (wpfTextView != null)
                {
                    _trackedTextViews.Add(pTextView, wpfTextView);
                    wpfTextView.Caret.PositionChanged += OnCaretPositionChanged;
                    wpfTextView.GotAggregateFocus += OnViewGotAggregateFocus;
                }
            }
        }

        private NavigationBarItem GetCurrentTypeItem()
        {
            int currentTypeIndex;
            _dropdownBar.GetCurrentSelection(1, out currentTypeIndex);

            return currentTypeIndex >= 0
                ? _currentTypeItems[currentTypeIndex]
                : null;
        }

        private NavigationBarItem GetItem(int combo, int index)
        {
            switch (combo)
            {
                case 0:
                    return _projectItems[index];

                case 1:
                    return _currentTypeItems[index];

                case 2:
                    return GetCurrentTypeItem().ChildItems[index];

                default:
                    throw new ArgumentException();
            }
        }

        int IVsDropdownBarClient.GetComboAttributes(int iCombo, out uint pcEntries, out uint puEntryType, out IntPtr phImageList)
        {
            puEntryType = (uint)(DROPDOWNENTRYTYPE.ENTRY_TEXT | DROPDOWNENTRYTYPE.ENTRY_ATTR | DROPDOWNENTRYTYPE.ENTRY_IMAGE);
            phImageList = _imageList;

            switch (iCombo)
            {
                case 0:
                    pcEntries = (uint)_projectItems.Count;
                    break;

                case 1:
                    pcEntries = (uint)_currentTypeItems.Count;
                    break;

                case 2:
                    var currentTypeItem = GetCurrentTypeItem();

                    pcEntries = currentTypeItem != null
                        ? (uint)currentTypeItem.ChildItems.Count
                        : 0;

                    break;

                default:
                    pcEntries = 0;
                    return VSConstants.E_INVALIDARG;
            }

            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.GetComboTipText(int iCombo, out string pbstrText)
        {
            int selectionIndex;
            var selectedItemPreviewText = string.Empty;

            if (_dropdownBar.GetCurrentSelection(iCombo, out selectionIndex) == VSConstants.S_OK && selectionIndex >= 0)
            {
                selectedItemPreviewText = GetItem(iCombo, selectionIndex).Text;
            }

            switch (iCombo)
            {
                case 0:
                    var keybindingString = KeyBindingHelper.GetGlobalKeyBinding(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.MoveToDropdownBar);
                    if (!string.IsNullOrWhiteSpace(keybindingString))
                    {
                        pbstrText = string.Format(ServicesVSResources.ProjectNavBarTooltipWithShortcut, selectedItemPreviewText, keybindingString);
                    }
                    else
                    {
                        pbstrText = string.Format(ServicesVSResources.ProjectNavBarTooltipWithoutShortcut, selectedItemPreviewText);
                    }

                    return VSConstants.S_OK;

                case 1:
                case 2:
                    pbstrText = string.Format(ServicesVSResources.NavBarTooltip, selectedItemPreviewText);
                    return VSConstants.S_OK;

                default:
                    pbstrText = null;
                    return VSConstants.E_INVALIDARG;
            }
        }

        int IVsDropdownBarClient.GetEntryAttributes(int iCombo, int iIndex, out uint pAttr)
        {
            DROPDOWNFONTATTR attributes = DROPDOWNFONTATTR.FONTATTR_PLAIN;

            var item = GetItem(iCombo, iIndex);

            if (item.Grayed)
            {
                attributes |= DROPDOWNFONTATTR.FONTATTR_GRAY;
            }

            if (item.Bolded)
            {
                attributes |= DROPDOWNFONTATTR.FONTATTR_BOLD;
            }

            pAttr = (uint)attributes;
            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.GetEntryImage(int iCombo, int iIndex, out int piImageIndex)
        {
            var item = GetItem(iCombo, iIndex);

            piImageIndex = item.Glyph.GetGlyphIndex();

            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.GetEntryText(int iCombo, int iIndex, out string ppszText)
        {
            ppszText = GetItem(iCombo, iIndex).Text;
            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.OnComboGetFocus(int iCombo)
        {
            DropDownFocused?.Invoke(this, EventArgs.Empty);

            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.OnItemChosen(int iCombo, int iIndex)
        {
            int selection;

            // If we chose an item for the type drop-down, then refresh the member dropdown
            if (iCombo == (int)NavigationBarDropdownKind.Type)
            {
                _dropdownBar.GetCurrentSelection((int)NavigationBarDropdownKind.Member, out selection);
                _dropdownBar.RefreshCombo((int)NavigationBarDropdownKind.Member, selection);
            }

            _dropdownBar.GetCurrentSelection(iCombo, out selection);

            if (selection >= 0)
            {
                var item = GetItem(iCombo, selection);
                ItemSelected?.Invoke(this, new NavigationBarItemSelectedEventArgs(item));
            }

            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.OnItemSelected(int iCombo, int iIndex)
        {
            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient.SetDropdownBar(IVsDropdownBar pDropdownBar)
        {
            _dropdownBar = pDropdownBar;

            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient3.GetComboWidth(int iCombo, out int piWidthPercent)
        {
            piWidthPercent = 100;
            return VSConstants.S_OK;
        }

        int IVsDropdownBarClient3.GetAutomationProperties(int iCombo, out string pbstrName, out string pbstrId)
        {
            switch (iCombo)
            {
                case 0:
                    pbstrName = NavigationBarAutomationStrings.ProjectDropdownName;
                    pbstrId = NavigationBarAutomationStrings.ProjectDropdownId;
                    return VSConstants.S_OK;
                case 1:
                    pbstrName = NavigationBarAutomationStrings.TypeDropdownName;
                    pbstrId = NavigationBarAutomationStrings.TypeDropdownId;
                    return VSConstants.S_OK;
                case 2:
                    pbstrName = NavigationBarAutomationStrings.MemberDropdownName;
                    pbstrId = NavigationBarAutomationStrings.MemberDropdownId;
                    return VSConstants.S_OK;
                default:
                    pbstrName = null;
                    pbstrId = null;
                    return VSConstants.E_INVALIDARG;
            }
        }

        int IVsDropdownBarClient3.GetEntryImage(int iCombo, int iIndex, out int piImageIndex, out IntPtr phImageList)
        {
            var item = GetItem(iCombo, iIndex);

            // If this is a project item, try to get the actual proper image from the VSHierarchy it 
            // represents.  That way the icon will always look right no matter which type of project
            // it is.  For example, if phone/Windows projects have different icons, then this can 
            // ensure we get the right icon, and not just a hardcoded C#/VB icon.
            if (item is NavigationBarProjectItem)
            {
                var projectItem = (NavigationBarProjectItem)item;
                if (_workspace.TryGetImageListAndIndex(_imageService, projectItem.DocumentId.ProjectId, out phImageList, out piImageIndex))
                {
                    return VSConstants.S_OK;
                }
            }

            piImageIndex = GetItem(iCombo, iIndex).Glyph.GetGlyphIndex();
            phImageList = _imageList;
            return VSConstants.S_OK;
        }

        int IVsDropdownBarClientEx.GetEntryIndent(int iCombo, int iIndex, out uint pIndent)
        {
            pIndent = (uint)GetItem(iCombo, iIndex).Indent;
            return VSConstants.S_OK;
        }

        void INavigationBarPresenter.Disconnect()
        {
            _manager.RemoveDropdownBar();
            _codeWindowEventsSink.Unadvise();

            foreach (var view in _trackedTextViews.Values)
            {
                view.Caret.PositionChanged -= OnCaretPositionChanged;
                view.GotAggregateFocus -= OnViewGotAggregateFocus;
            }

            _trackedTextViews.Clear();
        }

        void INavigationBarPresenter.PresentItems(
            IList<NavigationBarProjectItem> projects,
            NavigationBarProjectItem selectedProject,
            IList<NavigationBarItem> types,
            NavigationBarItem selectedType,
            NavigationBarItem selectedMember)
        {
            _projectItems = projects;
            _currentTypeItems = types;

            // It's possible we're presenting items before the dropdown bar has been initialized.
            if (_dropdownBar == null)
            {
                return;
            }

            var projectIndex = selectedProject != null ? _projectItems.IndexOf(selectedProject) : -1;
            var typeIndex = selectedType != null ? _currentTypeItems.IndexOf(selectedType) : -1;
            var memberIndex = selectedType != null && selectedMember != null ? selectedType.ChildItems.IndexOf(selectedMember) : -1;

            _dropdownBar.RefreshCombo((int)NavigationBarDropdownKind.Project, projectIndex);
            _dropdownBar.RefreshCombo((int)NavigationBarDropdownKind.Type, typeIndex);
            _dropdownBar.RefreshCombo((int)NavigationBarDropdownKind.Member, memberIndex);
        }

        public event EventHandler DropDownFocused;
        public event EventHandler<NavigationBarItemSelectedEventArgs> ItemSelected;

        public event EventHandler<EventArgs> ViewFocused;
        public event EventHandler<CaretPositionChangedEventArgs> CaretMoved;

        int IVsCodeWindowEvents.OnCloseView(IVsTextView pView)
        {
            ITextView view;

            if (_trackedTextViews.TryGetValue(pView, out view))
            {
                view.Caret.PositionChanged -= OnCaretPositionChanged;
                view.GotAggregateFocus -= OnViewGotAggregateFocus;

                _trackedTextViews.Remove(pView);
            }

            return VSConstants.S_OK;
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView pView)
        {
            if (!_trackedTextViews.ContainsKey(pView))
            {
                var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(pView);
                wpfTextView.Caret.PositionChanged += OnCaretPositionChanged;
                wpfTextView.GotAggregateFocus += OnViewGotAggregateFocus;

                _trackedTextViews.Add(pView, wpfTextView);
            }

            return VSConstants.S_OK;
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            CaretMoved?.Invoke(this, e);
        }

        private void OnViewGotAggregateFocus(object sender, EventArgs e)
        {
            ViewFocused?.Invoke(this, e);
        }

        ITextView INavigationBarPresenter.TryGetCurrentView()
        {
            IVsTextView lastActiveView;
            _codeWindow.GetLastActiveView(out lastActiveView);
            return _editorAdaptersFactoryService.GetWpfTextView(lastActiveView);
        }
    }
}
