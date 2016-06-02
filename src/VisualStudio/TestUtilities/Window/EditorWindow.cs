// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.VisualStudio.Test.Utilities.InProcess;
using Roslyn.VisualStudio.Test.Utilities.Input;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>
    /// Provides a means of interacting with the active editor window in the Visual Studio host.
    /// </summary>
    public partial class EditorWindow
    {
        private readonly VisualStudioInstance _visualStudioInstance;
        private readonly InProcessEditor _inProcessEditor;

        internal EditorWindow(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;

            // Create MarshalByRefObject that can be used to execute code in the VS process.
            _inProcessEditor = _visualStudioInstance.ExecuteInHostProcess<InProcessEditor>(
                type: typeof(InProcessEditor),
                methodName: nameof(InProcessEditor.Create));
        }

        public void Activate() => _inProcessEditor.Activate();

        public string GetText() => _inProcessEditor.GetText();
        public void SetText(string value) => _inProcessEditor.SetText(value);

        public string GetCurrentLineText() => _inProcessEditor.GetCurrentLineText();
        public int GetCaretPosition() => _inProcessEditor.GetCaretPosition();
        public string GetLineTextBeforeCaret() => _inProcessEditor.GetLineTextBeforeCaret();
        public string GetLineTextAfterCaret() => _inProcessEditor.GetLineTextAfterCaret();

        public void MoveCaret(int position) => _inProcessEditor.MoveCaret(position);

        public string[] GetCompletionItems() => _inProcessEditor.GetCompletionItems();
        public string GetCurrentCompletionItem() => _inProcessEditor.GetCurrentCompletionItem();

        public void SendKeys(params object[] keys)
        {
            Activate();

            foreach (var key in keys)
            {
                if (key is string)
                {
                    var text = ((string)key)
                        .Replace("\r\n", "\r")
                        .Replace("\n", "\r");

                    foreach (var ch in text)
                    {
                        _visualStudioInstance.SendKeys.Send(ch);
                    }
                }
                else if (key is char)
                {
                    _visualStudioInstance.SendKeys.Send((char)key);
                }
                else if (key is VirtualKey)
                {
                    _visualStudioInstance.SendKeys.Send((VirtualKey)key);
                }
                else if (key is KeyPress)
                {
                    _visualStudioInstance.SendKeys.Send((KeyPress)key);
                }
                else if (key == null)
                {
                    throw new ArgumentNullException(nameof(keys));
                }
                else
                {
                    throw new ArgumentException($"Unexpected type encountered: {key.GetType()}", nameof(keys));
                }
            }

            _visualStudioInstance.WaitForApplicationIdle();
        }
    }
}
