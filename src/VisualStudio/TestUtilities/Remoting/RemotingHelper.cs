// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    internal static class RemotingHelper
    {
        private static Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        public static InteractiveWindowWrapper CreateCSharpInteractiveWindowWrapper()
        {
            var componentModel = (IComponentModel)(ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)));
            var vsInteractiveWindowProvider = componentModel.GetService<CSharpVsInteractiveWindowProvider>();
            var vsInteractiveWindow = ExecuteOnUIThread(() => vsInteractiveWindowProvider.Open(0, true));
            return new InteractiveWindowWrapper(vsInteractiveWindow.InteractiveWindow);
        }

        public static string GetActiveTextViewContents()
        {
            var textViewHost = GetActiveTextViewHost();
            return ExecuteOnUIThread(textViewHost.TextView.TextSnapshot.GetText);
        }

        public static void SetActiveTextViewContents(string text)
        {
            var textViewHost = GetActiveTextViewHost();
            ExecuteOnUIThread(() => {
                var textView = textViewHost.TextView;
                var textSnapshot = textView.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                textView.TextBuffer.Replace(replacementSpan, text);
            });
        }

        private static void ExecuteOnUIThread(Action action)
            => Application.Current.Dispatcher.Invoke(action);

        private static T ExecuteOnUIThread<T>(Func<T> action)
            => Application.Current.Dispatcher.Invoke(action);

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should have finished composing
            WaitForApplicationIdle();

            var vsTextManager = (IVsTextManager)(ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager)));

            IVsTextView vsTextView = null;
            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out vsTextView);

            if (hresult != VSConstants.S_OK)
            {
                throw Marshal.GetExceptionForHR(hresult);
            }

            var vsUserData = (IVsUserData)(vsTextView);

            object wpfTextViewHost = null;
            vsUserData.GetData(ref IWpfTextViewId, out wpfTextViewHost);

            if (hresult != VSConstants.S_OK)
            {
                throw Marshal.GetExceptionForHR(hresult);
            }

            return (IWpfTextViewHost)(wpfTextViewHost);
        }

        private static void WaitForApplicationIdle()
            => Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
