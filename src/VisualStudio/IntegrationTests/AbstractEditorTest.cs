// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Common;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Roslyn.VisualStudio.Test.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        protected readonly VisualStudioWorkspace_OutOfProc VisualStudioWorkspaceOutOfProc;
        protected readonly Editor_OutOfProc Editor;

        protected AbstractEditorTest(VisualStudioInstanceFactory instanceFactory, string solutionName)
            : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            VisualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ClassLibrary, LanguageName);

            VisualStudioWorkspaceOutOfProc = VisualStudio.Instance.VisualStudioWorkspace;
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);

            Editor = VisualStudio.Instance.Editor;

            ClearEditor();
        }

        protected abstract string LanguageName { get; }

        private void WaitForAsyncOperations(string featuresToWaitFor)
        {
            VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(featuresToWaitFor);
        }

        protected void ClearEditor()
        {
            SetUpEditor("$$");
        }

        protected void SetUpEditor(string markupCode)
        {
            string code;
            int caretPosition;
            MarkupTestFile.GetPosition(markupCode, out code, out caretPosition);

            var originalValue = VisualStudioWorkspaceOutOfProc.IsPrettyListingOn(LanguageName);

            VisualStudioWorkspaceOutOfProc.SetPrettyListing(LanguageName, false);
            try
            {
                Editor.SetText(code);
                Editor.MoveCaret(caretPosition);
            }
            finally
            {
                VisualStudioWorkspaceOutOfProc.SetPrettyListing(LanguageName, originalValue);
            }
        }

        protected void SendKeys(params object[] keys)
        {
            Editor.SendKeys(keys);
        }

        protected KeyPress KeyPress(VirtualKey virtualKey, ShiftState shiftState)
        {
            return new KeyPress(virtualKey, shiftState);
        }

        protected KeyPress Ctrl(VirtualKey virtualKey)
        {
            return new KeyPress(virtualKey, ShiftState.Ctrl);
        }

        protected KeyPress Shift(VirtualKey virtualKey)
        {
            return new KeyPress(virtualKey, ShiftState.Shift);
        }

        protected void DisableSuggestionMode()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
        }

        protected void EnableSuggestionMode()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);
        }

        protected void InvokeCompletionList()
        {
            ExecuteCommand(WellKnownCommandNames.ListMembers);
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);
        }

        protected void ExecuteCommand(string commandName)
        {
            VisualStudio.Instance.ExecuteCommand(commandName);
        }

        private void VerifyCurrentLineTextAndAssertCaretPosition(string expectedText, bool trimWhitespace)
        {
            var caretStartIndex = expectedText.IndexOf("$$");
            if (caretStartIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var caretEndIndex = caretStartIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText.Substring(0, caretStartIndex);
            var expectedTextAfterCaret = expectedText.Substring(caretEndIndex);

            var lineText = Editor.GetCurrentLineText();

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

        protected void VerifyCurrentLineText(string expectedText, bool assertCaretPosition = false, bool trimWhitespace = true)
        {
            if (assertCaretPosition)
            {
                VerifyCurrentLineTextAndAssertCaretPosition(expectedText, trimWhitespace);
            }
            else
            {
                var lineText = Editor.GetCurrentLineText();

                if (trimWhitespace)
                {
                    lineText = lineText.Trim();
                }

                Assert.Equal(expectedText, lineText);
            }
        }

        private void VerifyTextContainsAndAssertCaretPosition(string expectedText)
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

            var editorText = Editor.GetText();
            Assert.Contains(expectedTextWithoutCaret, editorText);

            var index = editorText.IndexOf(expectedTextWithoutCaret);

            var caretPosition = Editor.GetCaretPosition();
            Assert.Equal(caretStartIndex + index, caretPosition);
        }

        protected void VerifyTextContains(string expectedText, bool assertCaretPosition = false)
        {
            if (assertCaretPosition)
            {
                VerifyTextContainsAndAssertCaretPosition(expectedText);
            }
            else
            {
                var editorText = Editor.GetText();
                Assert.Contains(expectedText, editorText);
            }
        }

        protected void VerifyCompletionItemExists(params string[] expectedItems)
        {
            var completionItems = Editor.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        protected void VerifyCurrentCompletionItem(string expectedItem)
        {
            var currentItem = Editor.GetCurrentCompletionItem();
            Assert.Equal(expectedItem, currentItem);
        }

        protected void VerifyCurrentSignature(Signature expectedSignature)
        {
            var currentSignature = Editor.GetCurrentSignature();
            Assert.Equal(expectedSignature, currentSignature);
        }

        protected void VerifyCaretIsOnScreen()
        {
            Assert.True(Editor.IsCaretOnScreen());
        }

        protected void VerifyCompletionListIsActive(bool expected)
        {
            Assert.Equal(expected, Editor.IsCompletionActive());
        }
    }
}
