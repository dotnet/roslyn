// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

                    Assert.AreEqual(expectedText, lineText);
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

                Assert.AreEqual(expectedTextBeforeCaret, lineTextBeforeCaret);
                Assert.AreEqual(expectedTextAfterCaret, lineTextAfterCaret);
                Assert.AreEqual(expectedTextBeforeCaret.Length + expectedTextAfterCaret.Length, lineText.Length);
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
                    ExtendedAssert.Contains(expectedText, editorText);
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
                ExtendedAssert.Contains(expectedTextWithoutCaret, editorText);

                var index = editorText.IndexOf(expectedTextWithoutCaret);

                var caretPosition = _textViewWindow.GetCaretPosition();
                Assert.AreEqual(caretStartIndex + index, caretPosition);
            }

            public void CompletionItemDoNotExist(
                params string[] expectedItems)
            {
                var completionItems = _textViewWindow.GetCompletionItems();
                foreach (var expectedItem in expectedItems)
                {
                    ExtendedAssert.DoesNotContain(expectedItem, completionItems);
                }
            }

            public void CurrentCompletionItem(
                string expectedItem)
            {
                var currentItem = _textViewWindow.GetCurrentCompletionItem();
                Assert.AreEqual(expectedItem, currentItem);
            }

            public void VerifyCurrentSignature(
                Signature expectedSignature)
            {
                var currentSignature = _textViewWindow.GetCurrentSignature();
                Assert.AreEqual(expectedSignature, currentSignature);
            }

            public void CurrentSignature(string content)
            {
                var currentSignature = _textViewWindow.GetCurrentSignature();
                Assert.AreEqual(content, currentSignature.Content);
            }

            public void CurrentParameter(
                string name,
                string documentation)
            {
                var currentParameter = _textViewWindow.GetCurrentSignature().CurrentParameter;
                Assert.AreEqual(name, currentParameter.Name);
                Assert.AreEqual(documentation, currentParameter.Documentation);
            }

            public void Parameters(
                params (string name, string documentation)[] parameters)
            {
                var currentParameters = _textViewWindow.GetCurrentSignature().Parameters;
                for (var i = 0; i < parameters.Length; i++)
                {
                    var (expectedName, expectedDocumentation) = parameters[i];
                    Assert.AreEqual(expectedName, currentParameters[i].Name);
                    Assert.AreEqual(expectedDocumentation, currentParameters[i].Documentation);
                }
            }

            public void Dialog(
                string dialogName,
                bool isOpen)
            {
                _textViewWindow.VerifyDialog(dialogName, isOpen);
            }

            public void ErrorTags(params string[] expectedTags)
            {
                _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
                _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
                var actualTags = _textViewWindow.GetErrorTags();
                Assert.AreEqual(expectedTags, actualTags);
            }

            public void IsProjectItemDirty(bool expectedValue)
            {
                Assert.AreEqual(expectedValue, _textViewWindow._editorInProc.IsProjectItemDirty());
            }
        }
    }
}
