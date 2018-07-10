// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public partial class Editor_OutOfProc : TextViewWindow_OutOfProc
    {
        public class Verifier : Verifier<Editor_OutOfProc>
        {
            public Verifier(Editor_OutOfProc editor, VisualStudioInstance instance)
                : base(editor, instance)
            {
            }

            public void CurrentLineText(
                string expectedText,
                bool assertCaretPosition = false,
                bool trimWhitespace = true)
            {
                if (assertCaretPosition)
                {
                    CurrentLineTextAndAssertCaretPosition(expectedText, trimWhitespace);
                }
                else
                {
                    var lineText = _textViewWindow.GetCurrentLineText();

                    if (trimWhitespace)
                    {
                        lineText = lineText.Trim();
                    }

                    Assert.Equal(expectedText, lineText);
                }
            }

            private void CurrentLineTextAndAssertCaretPosition(
                string expectedText,
                bool trimWhitespace)
            {
                var caretStartIndex = expectedText.IndexOf("$$");
                if (caretStartIndex < 0)
                {
                    throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
                }

                var caretEndIndex = caretStartIndex + "$$".Length;

                var expectedTextBeforeCaret = expectedText.Substring(0, caretStartIndex);
                var expectedTextAfterCaret = expectedText.Substring(caretEndIndex);

                var lineText = _textViewWindow.GetCurrentLineText();

                // Asserts below perform separate verifications of text before and after the caret.
                // Depending on the position of the caret, if trimWhitespace, we trim beginning, end or both sides.
                if (trimWhitespace)
                {
                    if (caretStartIndex == 0)
                    {
                        lineText = lineText.TrimEnd();
                    }
                    else if (caretEndIndex == expectedText.Length)
                    {
                        lineText = lineText.TrimStart();
                    }
                    else
                    {
                        lineText = lineText.Trim();
                    }
                }

                var lineTextBeforeCaret = caretStartIndex < lineText.Length
                    ? lineText.Substring(0, caretStartIndex)
                    : lineText;

                var lineTextAfterCaret = caretStartIndex < lineText.Length
                    ? lineText.Substring(caretStartIndex)
                    : string.Empty;

                Assert.Equal(expectedTextBeforeCaret, lineTextBeforeCaret);
                Assert.Equal(expectedTextAfterCaret, lineTextAfterCaret);
                Assert.Equal(expectedTextBeforeCaret.Length + expectedTextAfterCaret.Length, lineText.Length);
            }

            public void TextContains(
                string expectedText,
                bool assertCaretPosition = false)
            {
                if (assertCaretPosition)
                {
                    TextContainsAndAssertCaretPosition(expectedText);
                }
                else
                {
                    var editorText = _textViewWindow.GetText();
                    Assert.Contains(expectedText, editorText);
                }
            }

            private void TextContainsAndAssertCaretPosition(
                string expectedText)
            {
                var caretStartIndex = expectedText.IndexOf("$$");
                if (caretStartIndex < 0)
                {
                    throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
                }

                var caretEndIndex = caretStartIndex + "$$".Length;

                var expectedTextBeforeCaret = expectedText.Substring(0, caretStartIndex);
                var expectedTextAfterCaret = expectedText.Substring(caretEndIndex);

                var expectedTextWithoutCaret = expectedTextBeforeCaret + expectedTextAfterCaret;

                var editorText = _textViewWindow.GetText();
                Assert.Contains(expectedTextWithoutCaret, editorText);

                var index = editorText.IndexOf(expectedTextWithoutCaret);

                var caretPosition = _textViewWindow.GetCaretPosition();
                Assert.Equal(caretStartIndex + index, caretPosition);
            }

            public void Dialog(
                string dialogName,
                bool isOpen)
            {
                _textViewWindow.VerifyDialog(dialogName, isOpen);
            }
        }
    }
}
