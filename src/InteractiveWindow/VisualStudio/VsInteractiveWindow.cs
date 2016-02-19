// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Dumps commands in QueryStatus and Exec.
// #define DUMP_COMMANDS

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
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
    internal sealed class VsInteractiveWindow : IOleCommandTarget, IVsInteractiveWindow2, IDisposable
    {
        private readonly VsInteractiveWindowPane _windowPane;

        internal VsInteractiveWindow(IComponentModel model, Guid providerId, int instanceId, string title, IInteractiveEvaluator evaluator, __VSCREATETOOLWIN creationFlags)
        {
            _windowPane = new VsInteractiveWindowPane(model, providerId, instanceId, title, evaluator, creationFlags);
        }

        void IDisposable.Dispose() => _windowPane.Dispose();

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) =>
            _windowPane.CommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) =>
            _windowPane.CommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

        void IVsInteractiveWindow.SetLanguage(Guid languageServiceGuid, IContentType contentType) =>
            InteractiveWindow.SetLanguage(languageServiceGuid, contentType);

        void IVsInteractiveWindow.Show(bool focus)
        {
            var windowFrame = (IVsWindowFrame)WindowFrame;
            ErrorHandler.ThrowOnFailure(focus ? windowFrame.Show() : windowFrame.ShowNoActivate());

            if (focus)
            {
                IInputElement input = InteractiveWindow.TextView as IInputElement;
                if (input != null)
                {
                    Keyboard.Focus(input);
                }
            }
        }

        public IInteractiveWindow InteractiveWindow => _windowPane.InteractiveWindow;

        public object WindowFrame => _windowPane.Frame;
    }
}
