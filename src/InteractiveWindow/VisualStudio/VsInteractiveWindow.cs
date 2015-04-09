// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// Default tool window for hosting interactive windows inside of Visual Studio.  This hooks up support for
    /// find in windows, forwarding commands down to the text view adapter, and providing access for setting
    /// VS specific concepts (such as language service GUIDs) for the interactive window.
    /// 
    /// Interactive windows can also be hosted outside of this tool window if the user creates an IInteractiveWindow
    /// directly.  In that case the user is responsible for doing what this class does themselves.  But the
    /// interactive window will be properly initialized for running inside of Visual Studio's process by our 
    /// VsInteractiveWindowEditorsFactoryService which handles all of the mapping of VS commands to API calls
    /// on the interactive window.
    /// </summary>
    internal sealed class VsInteractiveWindow : ToolWindowPane, IVsFindTarget, IOleCommandTarget, IVsInteractiveWindow
    {
        private readonly IComponentModel _componentModel;
        private readonly IVsEditorAdaptersFactoryService _editorAdapters;

        private IInteractiveWindow _window;
        private IVsFindTarget _findTarget;
        private IOleCommandTarget _commandTarget;
        private IInteractiveEvaluator _evaluator;
        private IWpfTextViewHost _textViewHost;

        internal VsInteractiveWindow(IComponentModel model, Guid providerId, int instanceId, string title, IInteractiveEvaluator evaluator, __VSCREATETOOLWIN creationFlags)
        {
            _componentModel = model;
            this.Caption = title;
            _editorAdapters = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _evaluator = evaluator;

            // The following calls this.OnCreate:
            Guid clsId = this.ToolClsid;
            Guid empty = Guid.Empty;
            Guid typeId = providerId;
            IVsWindowFrame frame;
            var vsShell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));

            // we don't pass __VSCREATETOOLWIN.CTW_fMultiInstance because multi instance panes are
            // destroyed when closed.  We are really multi instance but we don't want to be closed.

            ErrorHandler.ThrowOnFailure(
                vsShell.CreateToolWindow(
                    (uint)(__VSCREATETOOLWIN.CTW_fInitNew | __VSCREATETOOLWIN.CTW_fToolbarHost | creationFlags),
                    (uint)instanceId,
                    this.GetIVsWindowPane(),
                    ref clsId,
                    ref typeId,
                    ref empty,
                    null,
                    title,
                    null,
                    out frame
                )
            );

            this.Frame = frame;
        }

        public void SetLanguage(Guid languageServiceGuid, IContentType contentType)
        {
            _window.SetLanguage(languageServiceGuid, contentType);
        }

        public IInteractiveWindow InteractiveWindow { get { return _window; } }

        #region ToolWindowPane overrides

        protected override void OnCreate()
        {
            _window = _componentModel.GetService<IInteractiveWindowFactoryService>().CreateWindow(_evaluator);
            _window.SubmissionBufferAdded += SubmissionBufferAdded;
            _textViewHost = _window.GetTextViewHost();
            var viewAdapter = _editorAdapters.GetViewAdapter(_textViewHost.TextView);
            _findTarget = viewAdapter as IVsFindTarget;
            _commandTarget = viewAdapter as IOleCommandTarget;
        }

        private void SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs e)
        {
            GetToolbarHost().ForceUpdateUI();
        }

        protected override void OnClose()
        {
            _window.Close();
            base.OnClose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_window != null)
                {
                    _window.Dispose();
                }
            }
        }

        /// <summary>
        /// This property returns the control that should be hosted in the Tool Window. It can be
        /// either a FrameworkElement (for easy creation of tool windows hosting WPF content), or it
        /// can be an object implementing one of the IVsUIWPFElement or IVsUIWin32Element
        /// interfaces.
        /// </summary>
        public override object Content
        {
            get { return _textViewHost; }
            set { }
        }

        public override void OnToolWindowCreated()
        {
            Guid commandUiGuid = VSConstants.GUID_TextEditorFactory;
            ((IVsWindowFrame)Frame).SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref commandUiGuid);

            base.OnToolWindowCreated();

            // add our toolbar which  is defined in our VSCT file
            var toolbarHost = GetToolbarHost();
            Guid guidInteractiveCmdSet = Guids.InteractiveCommandSetId;
            ErrorHandler.ThrowOnFailure(toolbarHost.AddToolbar(VSTWT_LOCATION.VSTWT_TOP, ref guidInteractiveCmdSet, (uint)MenuIds.InteractiveWindowToolbar));
        }

        #endregion

        #region Window IOleCommandTarget

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return _commandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        #endregion

        #region IVsInteractiveWindow

        public void Show(bool focus)
        {
            var windowFrame = (IVsWindowFrame)Frame;
            ErrorHandler.ThrowOnFailure(focus ? windowFrame.Show() : windowFrame.ShowNoActivate());

            if (focus)
            {
                IInputElement input = _window.TextView as IInputElement;
                if (input != null)
                {
                    Keyboard.Focus(input);
                }
            }
        }

        private IVsToolWindowToolbarHost GetToolbarHost()
        {
            var frame = (IVsWindowFrame)Frame;
            object result;
            ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_ToolbarHost, out result));
            return (IVsToolWindowToolbarHost)result;
        }

        #endregion

        #region IVsFindTarget

        public int Find(string pszSearch, uint grfOptions, int fResetStartPoint, IVsFindHelper pHelper, out uint pResult)
        {
            if (_findTarget != null)
            {
                return _findTarget.Find(pszSearch, grfOptions, fResetStartPoint, pHelper, out pResult);
            }
            pResult = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int GetCapabilities(bool[] pfImage, uint[] pgrfOptions)
        {
            if (_findTarget != null && pgrfOptions != null && pgrfOptions.Length > 0)
            {
                return _findTarget.GetCapabilities(pfImage, pgrfOptions);
            }
            return VSConstants.E_NOTIMPL;
        }

        public int GetCurrentSpan(TextSpan[] pts)
        {
            if (_findTarget != null)
            {
                return _findTarget.GetCurrentSpan(pts);
            }
            return VSConstants.E_NOTIMPL;
        }

        public int GetFindState(out object ppunk)
        {
            if (_findTarget != null)
            {
                return _findTarget.GetFindState(out ppunk);
            }
            ppunk = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetMatchRect(RECT[] prc)
        {
            if (_findTarget != null)
            {
                return _findTarget.GetMatchRect(prc);
            }
            return VSConstants.E_NOTIMPL;
        }

        public int GetProperty(uint propid, out object pvar)
        {
            if (_findTarget != null)
            {
                return _findTarget.GetProperty(propid, out pvar);
            }
            pvar = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetSearchImage(uint grfOptions, IVsTextSpanSet[] ppSpans, out IVsTextImage ppTextImage)
        {
            if (_findTarget != null)
            {
                return _findTarget.GetSearchImage(grfOptions, ppSpans, out ppTextImage);
            }
            ppTextImage = null;
            return VSConstants.E_NOTIMPL;
        }

        public int MarkSpan(TextSpan[] pts)
        {
            if (_findTarget != null)
            {
                return _findTarget.MarkSpan(pts);
            }
            return VSConstants.E_NOTIMPL;
        }

        public int NavigateTo(TextSpan[] pts)
        {
            if (_findTarget != null)
            {
                return _findTarget.NavigateTo(pts);
            }
            return VSConstants.E_NOTIMPL;
        }

        public int NotifyFindTarget(uint notification)
        {
            if (_findTarget != null)
            {
                return _findTarget.NotifyFindTarget(notification);
            }
            return VSConstants.E_NOTIMPL;
        }

        public int Replace(string pszSearch, string pszReplace, uint grfOptions, int fResetStartPoint, IVsFindHelper pHelper, out int pfReplaced)
        {
            if (_findTarget != null)
            {
                return _findTarget.Replace(pszSearch, pszReplace, grfOptions, fResetStartPoint, pHelper, out pfReplaced);
            }
            pfReplaced = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int SetFindState(object pUnk)
        {
            if (_findTarget != null)
            {
                return _findTarget.SetFindState(pUnk);
            }
            return VSConstants.E_NOTIMPL;
        }

        #endregion
    }
}
