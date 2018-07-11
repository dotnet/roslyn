// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Editor_InProc : TextViewWindow_InProc
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        private Editor_InProc() { }

        public static Editor_InProc Create()
            => new Editor_InProc();

        protected override IWpfTextView GetActiveTextView()
            => GetActiveTextViewHost().TextView;

        private static IVsTextView GetActiveVsTextView()
        {
            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            WaitForApplicationIdle();

            var activeVsTextView = (IVsUserData)GetActiveVsTextView();

            var hresult = activeVsTextView.GetData(IWpfTextViewId, out var wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            return (IWpfTextViewHost)wpfTextViewHost;
        }

        public string GetActiveBufferName()
        {
            return GetDTE().ActiveDocument.Name;
        }

        public void WaitForActiveView(string expectedView)
        {
            Retry(GetActiveBufferName, (actual) => actual == expectedView, TimeSpan.FromMilliseconds(100));
        }

        public void Activate()
            => GetDTE().ActiveDocument.Activate();

        public void SetText(string text)
            => ExecuteOnActiveView(view =>
            {
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            });

        public string GetSelectedText()
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var selectedSpan = view.Selection.SelectedSpans[0];
                return subjectBuffer.CurrentSnapshot.GetText(selectedSpan);
            });

        public void MoveCaret(int position)
            => ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
            });

        public void SendKeysToNavigateTo(string keys)
        {
            var dialogAutomationElement = FindNavigateTo();
            if (dialogAutomationElement == null)
            {
                throw new InvalidOperationException($"Expected the NavigateTo dialog to be open, but it is not.");
            }

            dialogAutomationElement.SetFocus();
            SendKeys.SendWait(keys);
        }

        private static IUIAutomationElement FindNavigateTo()
        {
            var vsAutomationElement = Helper.Automation.ElementFromHandle((IntPtr)GetDTE().MainWindow.HWnd);
            return vsAutomationElement.FindDescendantByAutomationId("PART_SearchBox");
        }

        private T Retry<T>(Func<T> action, Func<T, bool> stoppingCondition, TimeSpan delay)
        {
            var beginTime = DateTime.UtcNow;
            var retval = default(T);

            do
            {
                try
                {
                    retval = action();
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.

                    Thread.Sleep(delay);
                    continue;
                }

                if (stoppingCondition(retval))
                {
                    return retval;
                }
                else
                {
                    Thread.Sleep(delay);
                }
            }
            while (true);
        }
    }
}
