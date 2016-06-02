// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
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

        protected void SendKeys(params object[] textOrVirtualKeys)
        {
            _editorWindow.SendKeys(textOrVirtualKeys);
        }

        protected void VerifyCurrentLineText(string expectedText, bool trimWhitespace = false)
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
                    lineText = lineText.Trim();
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
    }
}
