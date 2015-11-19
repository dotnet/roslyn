// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    /// <summary>
    /// Base class of a all test-containing classes. Automatically creates a FileCodeModel for testing with the given
    /// file.
    /// </summary>
    public abstract class AbstractFileCodeElementTests : IDisposable
    {
        private readonly Task<Tuple<TestWorkspace, FileCodeModel>> _task;

        protected async Task<TestWorkspace> GetWorkspaceAsync()
        {
            var tuple = await _task;
            return tuple.Item1;
        }

        protected async Task<FileCodeModel> GetCodeModelAsync()
        {
            var tuple = await _task;
            return tuple.Item2;
        }

        protected async Task<CodeAnalysis.Solution> GetCurrentSolutionAsync()
        {
            return (await GetWorkspaceAsync()).CurrentSolution;
        }

        protected async Task<CodeAnalysis.Project> GetCurrentProjectAsync()
        {
            return (await GetCurrentSolutionAsync()).Projects.Single();
        }

        protected async Task<CodeAnalysis.Document> GetCurrentDocumentAsync()
        {
            return (await GetCurrentProjectAsync()).Documents.Single();
        }

        public AbstractFileCodeElementTests(string contents)
        {
            _task = CreateWorkspaceAndFileCodeModelAsync(contents);
        }

        protected static Task<Tuple<TestWorkspace, EnvDTE.FileCodeModel>> CreateWorkspaceAndFileCodeModelAsync(string file)
        {
            return FileCodeModelTestHelpers.CreateWorkspaceAndFileCodeModelAsync(file);
        }

        protected async Task<CodeElement> GetCodeElementAsync(params object[] path)
        {
            if (path.Length == 0)
            {
                throw new ArgumentException("path must be non-empty.", "path");
            }

            CodeElement codeElement = (await GetCodeModelAsync()).CodeElements.Item(path[0]);

            foreach (var pathElement in path.Skip(1))
            {
                codeElement = codeElement.Children.Item(pathElement);
            }

            return codeElement;
        }

        public void Dispose()
        {
            GetWorkspaceAsync().Result.Dispose();
        }

        /// <summary>
        /// Returns the current text of the test buffer.
        /// </summary>
        protected async Task<string> GetFileTextAsync()
        {
            return (await GetWorkspaceAsync()).Documents.Single().GetTextBuffer().CurrentSnapshot.GetText();
        }
    }
}
