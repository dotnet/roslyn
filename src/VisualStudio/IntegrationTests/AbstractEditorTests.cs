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
    public abstract class AbstractEditorTests : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;
        private readonly VisualStudioWorkspace_OutOfProc _visualStudioWorkspace;
        private readonly Editor_OutOfProc _editor;

        protected AbstractEditorTests(VisualStudioInstanceFactory instanceFactory, string solutionName)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _visualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            _visualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ClassLibrary, LanguageName);

            _visualStudioWorkspace = _visualStudio.Instance.VisualStudioWorkspace;
            _visualStudioWorkspace.SetUseSuggestionMode(false);

            _editor = _visualStudio.Instance.Editor;
        }

        protected abstract string LanguageName { get; }

        public void Dispose()
        {
            _visualStudio.Dispose();
        }

        public void WaitForAsyncOperations(string featuresToWaitFor)
        {
            _visualStudioWorkspace.WaitForAsyncOperations(featuresToWaitFor);
        }

        protected void WaitForAllAsyncOperations()
        {
            _visualStudioWorkspace.WaitForAllAsyncOperations();
        }

        protected void SetUpEditor(string markupCode)
        {
            string code;
            int caretPosition;
            MarkupTestFile.GetPosition(markupCode, out code, out caretPosition);

            var originalValue = _visualStudioWorkspace.IsPrettyListingOn(LanguageName);

            _visualStudioWorkspace.SetPrettyListing(LanguageName, false);
            try
            {
                _editor.SetText(code);
                _editor.MoveCaret(caretPosition);
            }
            finally
            {
                _visualStudioWorkspace.SetPrettyListing(LanguageName, originalValue);
            }
        }

        protected void SendKeys(params object[] keys)
        {
            _editor.SendKeys(keys);
        }

        protected KeyPress KeyPress(VirtualKey virtualKey, ShiftState shiftState)
        {
            return new KeyPress(virtualKey, shiftState);
        }

        protected void DisableSuggestionMode()
        {
            _visualStudioWorkspace.SetUseSuggestionMode(false);
        }

        protected void EnableSuggestionMode()
        {
            _visualStudioWorkspace.SetUseSuggestionMode(true);
        }

        protected void InvokeCompletionList()
        {
            ExecuteCommand(WellKnownCommandNames.ListMembers);
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);
        }

        protected void ExecuteCommand(string commandName)
        {
            _visualStudio.Instance.ExecuteCommand(commandName);
        }

        protected void VerifyCurrentLineText(string expectedText, bool trimWhitespace = true)
        {
            var caretStartIndex = expectedText.IndexOf("$$");

            if (caretStartIndex >= 0)
            {
                var caretEndIndex = caretStartIndex + "$$".Length;

                var expectedTextBeforeCaret = caretStartIndex < expectedText.Length
                    ? expectedText.Substring(0, caretStartIndex)
                    : expectedText;

                var expectedTextAfterCaret = caretEndIndex < expectedText.Length
                    ? expectedText.Substring(caretEndIndex)
                    : string.Empty;

                var lineText = _editor.GetCurrentLineText();

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
            else
            {
                var lineText = _editor.GetCurrentLineText();

                if (trimWhitespace)
                {
                    lineText = lineText.Trim();
                }

                Assert.Equal(expectedText, lineText);
            }
        }

        protected void VerifyTextContains(string expectedText)
        {
            var caretStartIndex = expectedText.IndexOf("$$");

            if (caretStartIndex >= 0)
            {
                var caretEndIndex = caretStartIndex + "$$".Length;

                var expectedTextBeforeCaret = caretStartIndex < expectedText.Length
                    ? expectedText.Substring(0, caretStartIndex)
                    : expectedText;

                var expectedTextAfterCaret = caretEndIndex < expectedText.Length
                    ? expectedText.Substring(caretEndIndex)
                    : string.Empty;

                var expectedTextWithoutCaret = expectedTextBeforeCaret + expectedTextAfterCaret;

                var editorText = _editor.GetText();
                Assert.Contains(expectedTextWithoutCaret, editorText);

                var index = editorText.IndexOf(expectedTextWithoutCaret);

                var caretPosition = _editor.GetCaretPosition();
                Assert.Equal(caretStartIndex + index, caretPosition);
            }
            else
            {
                var editorText = _editor.GetText();
                Assert.Contains(expectedText, editorText);
            }
        }

        protected void VerifyCompletionItemExists(params string[] expectedItems)
        {
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);

            var completionItems = _editor.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        protected void VerifyCurrentCompletionItem(string expectedItem)
        {
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);

            var currentItem = _editor.GetCurrentCompletionItem();
            Assert.Equal(expectedItem, currentItem);
        }

        protected void VerifyCurrentSignature(Signature expectedSignature)
        {
            WaitForAsyncOperations(FeatureAttribute.SignatureHelp);

            var currentSignature = _editor.GetCurrentSignature();
            Assert.Equal(expectedSignature, currentSignature);
        }

        protected void VerifyCaretIsOnScreen()
        {
            Assert.True(_editor.IsCaretOnScreen());
        }

        protected void VerifyCompletionListIsActive(bool expected)
        {
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            Assert.Equal(expected, _editor.IsCompletionActive());
        }
    }
}
