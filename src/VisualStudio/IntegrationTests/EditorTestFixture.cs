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
        protected readonly EditorWindow EditorWindow;

        protected EditorTestFixture(VisualStudioInstanceFactory instanceFactory, string solutionName)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _solution = _visualStudio.Instance.SolutionExplorer.CreateSolution(solutionName);
            _project = _solution.AddProject("TestProj", ProjectTemplate.ClassLibrary, ProjectLanguage.CSharp);

            _workspace = _visualStudio.Instance.Workspace;
            _workspace.UseSuggestionMode = false;

            EditorWindow = _visualStudio.Instance.EditorWindow;
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

            EditorWindow.SetText(code);
            EditorWindow.MoveCaret(caretPosition);
        }

        protected void VerifyCurrentLine(string text)
        {
            var caretIndex = text.IndexOf("$$");
            if (caretIndex >= 0)
            {
                var firstPart = text.Substring(0, caretIndex);
                var secondPart = text.Substring(caretIndex + "$$".Length);

                var lineText = EditorWindow.GetCurrentLineText();

                Assert.Equal(firstPart.Length + secondPart.Length, lineText.Length);
                Assert.Equal(firstPart, lineText.Substring(0, caretIndex));
                Assert.Equal(secondPart, lineText.Substring(caretIndex));
            }
        }

        protected void VerifyTextContains(string text)
        {
            Assert.Contains(text, EditorWindow.GetText());
        }
    }
}
