// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class EditorTestFixture : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;
        private readonly Workspace _workspace;
        private readonly Solution _solution;
        private readonly Project _project;
        private readonly EditorWindow _editorWindow;

        protected EditorTestFixture(VisualStudioInstanceFactory instanceFactory, string solutionName)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _solution = _visualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            _project = _solution.AddProject("TestProj", ProjectTemplate.ClassLibrary, ProjectLanguage.CSharp);

            _workspace = _visualStudio.Instance.Workspace;
            _workspace.UseSuggestionMode = false;

            _editorWindow = _visualStudio.Instance.EditorWindow;
        }

        public void Dispose()
        {
            _visualStudio.Dispose();
        }

        protected void WaitForWorkspace()
        {
            _workspace.WaitForAsyncOperations("Workspace");
        }

        public void WaitForAsyncOperations(string featuresToWaitFor)
        {
            _workspace.WaitForAsyncOperations(featuresToWaitFor);
        }

        protected void WaitForAllAsyncOperations()
        {
            _workspace.WaitForAllAsyncOperations();
        }

        protected void SetUpEditor(string markupCode)
        {
            string code;
            int caretPosition;
            MarkupTestFile.GetPosition(markupCode, out code, out caretPosition);

            _editorWindow.SetText(code);
            _editorWindow.MoveCaret(caretPosition);
        }

        protected void SendKeys(params object[] keys)
        {
            _editorWindow.SendKeys(keys);
        }

        protected KeyPress KeyPress(VirtualKey virtualKey, ShiftState shiftState)
        {
            return new KeyPress(virtualKey, shiftState);
        }

        protected void DisableSuggestionMode()
        {
            _workspace.UseSuggestionMode = false;
        }

        protected void EnableSuggestionMode()
        {
            _workspace.UseSuggestionMode = true;
        }

        protected void InvokeVisualStudioCommand(string commandName)
        {
            _visualStudio.Instance.ExecuteCommandAsync("Edit.ToggleCompletionMode").Wait();
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

                var lineText = _editorWindow.GetCurrentLineText();

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
                var lineText = _editorWindow.GetCurrentLineText();

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

                var editorText = _editorWindow.GetText();
                Assert.Contains(expectedTextWithoutCaret, editorText);

                var index = editorText.IndexOf(expectedTextWithoutCaret);

                var caretPosition = _editorWindow.GetCaretPosition();
                Assert.Equal(caretStartIndex + index, caretPosition);
            }
            else
            {
                var editorText = _editorWindow.GetText();
                Assert.Contains(expectedText, editorText);
            }
        }

        protected void VerifyCompletionItemExists(params string[] expectedItems)
        {
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);

            var completionItems = _editorWindow.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        protected void VerifyCurrentCompletionItem(string expectedItem)
        {
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);

            var currentItem = _editorWindow.GetCurrentCompletionItem();
            Assert.Equal(expectedItem, currentItem);
        }
    }
}
