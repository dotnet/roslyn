// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ImmediateWindow_InProc : TextViewWindow_InProc
    {
        private static Guid _immediateWindowId = new Guid("{ECB7191A-597B-41F5-9843-03A4CF275DDE}");
        private IVsWindowFrame _immediateWindow;

        public static ImmediateWindow_InProc Create()
            => new ImmediateWindow_InProc();

        public void ShowWindow()
        {
            if (_immediateWindow == null)
            {
                _immediateWindow = AcquireImmediateWindow();
            }

            _immediateWindow.Show();
        }

        private IVsWindowFrame AcquireImmediateWindow()
            => InvokeOnUIThread(() =>
            {
                var uiShell = GetGlobalService<SVsUIShell, IVsUIShell>();
                var hresult = uiShell.FindToolWindowEx((uint)__VSFINDTOOLWIN.FTW_fFindFirst, ref _immediateWindowId, 0, out var windowFrame);
                Marshal.ThrowExceptionForHR(hresult);
                return windowFrame;
            });

        public void Clear()
            => InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, string.Empty);
            });

        protected override IWpfTextView GetActiveTextView()
            => InvokeOnUIThread(() =>
            {
                var hresult = _immediateWindow.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var result);
                Marshal.ThrowExceptionForHR(hresult);
                var textView = (IVsTextView)result;
                var adaptersFactory = GetComponentModelService<IVsEditorAdaptersFactoryService>();
                return adaptersFactory.GetWpfTextView(textView);
            });

        protected override ITextBuffer GetBufferContainingCaret(IWpfTextView view)
            => view.TextBuffer;
    }
}
