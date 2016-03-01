// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
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
    [Guid(Guids.InteractiveToolWindowIdString)]
    internal sealed class VsInteractiveWindow : ToolWindowPane, IOleCommandTarget, IVsInteractiveWindow2
    {
        // Keep in sync with Microsoft.VisualStudio.Editor.Implementation.EnableFindOptionDefinition.OptionName.
        private const string EnableFindOptionName = "Enable Autonomous Find";

        private readonly IComponentModel _componentModel;
        private readonly IVsEditorAdaptersFactoryService _editorAdapters;
        private readonly IInteractiveEvaluator _evaluator;

        private IInteractiveWindow _window;
        private IVsFindTarget _findTarget;
        private IOleCommandTarget _commandTarget;
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
                    out frame));

            var guid = GetType().GUID;
            ErrorHandler.ThrowOnFailure(frame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_CmdUIGuid, ref guid));
            this.Frame = frame;
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) =>
            _commandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) =>
            _commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

        void IVsInteractiveWindow.SetLanguage(Guid languageServiceGuid, IContentType contentType) =>
            _window.SetLanguage(languageServiceGuid, contentType);

        void IVsInteractiveWindow.Show(bool focus)
        {
            var windowFrame = (IVsWindowFrame)this.Frame;
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

        ImageMoniker IVsInteractiveWindow2.BitmapImageMoniker
        {
            get
            {
                return this.BitmapImageMoniker;
            }

            set
            {
                this.BitmapImageMoniker = value;
            }
        }

        IInteractiveWindow IVsInteractiveWindow.InteractiveWindow => _window;

        #region ToolWindowPane overrides

        protected override void OnCreate()
        {
            _window = _componentModel.GetService<IInteractiveWindowFactoryService>().CreateWindow(_evaluator);
            _window.SubmissionBufferAdded += SubmissionBufferAdded;
            _textViewHost = _window.GetTextViewHost();
            var textView = _textViewHost.TextView;
            textView.Options.SetOptionValue(EnableFindOptionName, true);
            var viewAdapter = _editorAdapters.GetViewAdapter(textView);
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

        object IVsInteractiveWindow2.WindowFrame
        {
            get
            {
                throw new NotImplementedException();
            }
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

        private IVsToolWindowToolbarHost GetToolbarHost()
        {
            var frame = (IVsWindowFrame)Frame;
            object result;
            ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_ToolbarHost, out result));
            return (IVsToolWindowToolbarHost)result;
        }
    }
}
