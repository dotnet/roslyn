// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    internal class EditorWindowWrapper : MarshalByRefObject
    {
        private EditorWindowWrapper() { }

        public static EditorWindowWrapper Create()
        {
            return new EditorWindowWrapper();
        }

        public string GetText()
        {
            return RemotingHelper.ExecuteOnActiveView(view =>
            {
                return view.TextSnapshot.GetText();
            });
        }

        public void SetText(string text)
        {
            RemotingHelper.ExecuteOnActiveView(view =>
            {
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            });
        }

        public string GetCurrentLineText()
        {
            return RemotingHelper.ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;
                var line = bufferPosition.GetContainingLine();

                return line.GetText();
            });
        }

        public int GetCaretPosition()
        {
            return RemotingHelper.ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var bufferPosition = view.Caret.Position.BufferPosition;

                return bufferPosition.Position;
            });
        }

        public string GetLineTextBeforeCaret()
        {
            return RemotingHelper.ExecuteOnActiveView(view =>
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
            return RemotingHelper.ExecuteOnActiveView(view =>
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
            RemotingHelper.ExecuteOnActiveView(view =>
            {
                var subjectBuffer = view.GetBufferContainingCaret();
                var point = new SnapshotPoint(subjectBuffer.CurrentSnapshot, position);

                view.Caret.MoveTo(point);
            });
        }
    }
}
