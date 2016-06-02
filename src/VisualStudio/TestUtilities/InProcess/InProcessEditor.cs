// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
{
    internal class InProcessEditor : BaseInProcessComponent
    {
        private static readonly Guid IWpfTextViewId = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");

        private InProcessEditor() { }

        public static InProcessEditor Create()
        {
            return new InProcessEditor();
        }

        private static ITextView GetActiveTextView()
        {
            return GetActiveTextViewHost().TextView;
        }

        private static IVsTextView GetActiveVsTextView()
        {
            IVsTextView vsTextView = null;

            var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();

            var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out vsTextView);
            Marshal.ThrowExceptionForHR(hresult);

            return vsTextView;
        }

        private static IWpfTextViewHost GetActiveTextViewHost()
        {
            // The active text view might not have finished composing yet, waiting for the application to 'idle'
            // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
            WaitForApplicationIdle();

            var activeVsTextView = (IVsUserData)GetActiveVsTextView();

            object wpfTextViewHost;
            var hresult = activeVsTextView.GetData(IWpfTextViewId, out wpfTextViewHost);
            Marshal.ThrowExceptionForHR(hresult);

            return (IWpfTextViewHost)wpfTextViewHost;
        }

        private static void ExecuteOnActiveView(Action<ITextView> action)
        {
            InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                action(view);
            });
        }

        private static T ExecuteOnActiveView<T>(Func<ITextView, T> action)
        {
            return InvokeOnUIThread(() =>
            {
                var view = GetActiveTextView();
                return action(view);
            });
        }

        public void Activate()
        {
            GetDTE().ActiveDocument.Activate();
        }

        public string GetText()
        {
            return ExecuteOnActiveView(view =>
            {
                return view.TextSnapshot.GetText();
            });
        }

        public void SetText(string text)
        {
            ExecuteOnActiveView(view =>
            {
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            });
        }

        public string GetCurrentLineText()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });
        }

        public int GetCaretPosition()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;

                return bufferPosition.Position;
            });
        }

        public string GetLineTextBeforeCaret()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text.Substring(0, bufferPosition.Position - line.Start);
            });
        }

        public string GetLineTextAfterCaret()
        {
            return ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();
                var text = line.GetText();

                return text.Substring(bufferPosition.Position - line.Start);
            });
        }

        public void MoveCaret(int position)
        {
            ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
            });
        }
    }
}
