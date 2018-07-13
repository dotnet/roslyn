// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Xunit;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Roslyn.VisualStudio.IntegrationTests.Fixtures
{
    public abstract class ClassLibraryProjectFixture : IAsyncLifetime
    {
        protected ClassLibraryProjectFixture(string languageName)
        {
            LanguageName = languageName;
        }

        private JoinableTaskFactory JoinableTaskFactory => ThreadHelper.JoinableTaskFactory;

        private string LanguageName
        {
            get;
        }

        private string LanguageExtension
            => LanguageName == LanguageNames.CSharp ? ".cs" : ".vb";

        private TestServices TestServices
        {
            get;
            set;
        }

        private string BaseSolutionPath
        {
            get;
            set;
        }

        internal async Task CreateOrOpenAsync(string solutionName)
        {
            var solutionFileName = Path.ChangeExtension(solutionName, ".sln");
            var solutionFilePath = Path.Combine(BaseSolutionPath, solutionName, solutionFileName);

            if (File.Exists(solutionFilePath))
            {
                await TestServices.SolutionExplorer.OpenSolutionAsync(solutionFilePath);
                await TestServices.SolutionExplorer.OpenFileAsync("TestProj", "Class1" + LanguageExtension);
            }
            else
            {
                var solutionPath = Path.Combine(BaseSolutionPath, solutionName);
                await TestServices.SolutionExplorer.CreateSolutionAsync(solutionPath, solutionFileName);
                await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ClassLibrary, LanguageName);

                // Make sure to save the solution after adding the project, or it will be empty when reopened
                await TestServices.SolutionExplorer.SaveSolutionAsync();
            }

            await TestServices.Workspace.SetUseSuggestionModeAsync(false);
            await ClearEditorAsync();
        }

        public async Task ClearEditorAsync()
            => await SetUpEditorAsync("$$");

        public async Task SetUpEditorAsync(string markupCode)
        {
            MarkupTestFile.GetPosition(markupCode, out string code, out int caretPosition);

            var originalValue = TestServices.Workspace.IsPrettyListingOn(LanguageName);

            await TestServices.Workspace.SetPrettyListingAsync(LanguageName, false);
            try
            {
                await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
                await TestServices.Editor.SetTextAsync(code);
                await TestServices.Editor.MoveCaretAsync(caretPosition);
                await TestServices.Editor.ActivateAsync();
            }
            finally
            {
                await TestServices.Workspace.SetPrettyListingAsync(LanguageName, originalValue);
            }
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            TestServices = await TestServices.CreateAsync(JoinableTaskFactory);
            BaseSolutionPath = Path.Combine(TempRoot.Root, Path.GetRandomFileName());
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            if (!Directory.Exists(BaseSolutionPath))
            {
                return;
            }

            await TestServices.SolutionExplorer.CloseSolutionAsync();
            IntegrationHelper.TryDeleteDirectoryRecursively(BaseSolutionPath);

            await TestServices.Workspace.WaitForAllAsyncOperationsAsync();
        }
    }
}
