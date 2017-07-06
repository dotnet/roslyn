// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    /// <summary>
    /// Base class of a all test-containing classes. Automatically creates a FileCodeModel for testing with the given
    /// file.
    /// </summary>
    public abstract class AbstractFileCodeElementTests : IDisposable
    {
        private readonly Tuple<TestWorkspace, FileCodeModel> _workspaceAndCodeModel;

        protected TestWorkspace GetWorkspace()
        {
            return _workspaceAndCodeModel.Item1;
        }

        protected FileCodeModel GetCodeModel()
        {
            return _workspaceAndCodeModel.Item2;
        }

        protected Microsoft.CodeAnalysis.Solution GetCurrentSolution()
            => GetWorkspace().CurrentSolution;

        protected Microsoft.CodeAnalysis.Project GetCurrentProject()
            => GetCurrentSolution().Projects.Single();

        protected Microsoft.CodeAnalysis.Document GetCurrentDocument()
            => GetCurrentProject().Documents.Single();

        public AbstractFileCodeElementTests(string contents)
        {
            _workspaceAndCodeModel = CreateWorkspaceAndFileCodeModelAsync(contents);
        }

        protected static Tuple<TestWorkspace, EnvDTE.FileCodeModel> CreateWorkspaceAndFileCodeModelAsync(string file)
            => FileCodeModelTestHelpers.CreateWorkspaceAndFileCodeModel(file);

        protected CodeElement GetCodeElement(params object[] path)
        {
            WpfTestCase.RequireWpfFact("Tests create CodeElements which use the affinitized CleanableWeakComHandleTable");

            if (path.Length == 0)
            {
                throw new ArgumentException("path must be non-empty.", nameof(path));
            }

            CodeElement codeElement = (GetCodeModel()).CodeElements.Item(path[0]);

            foreach (var pathElement in path.Skip(1))
            {
                codeElement = codeElement.Children.Item(pathElement);
            }

            return codeElement;
        }

        public void Dispose()
        {
            GetWorkspace().Dispose();
        }

        /// <summary>
        /// Returns the current text of the test buffer.
        /// </summary>
        protected string GetFileText()
        {
            return (GetWorkspace()).Documents.Single().GetTextBuffer().CurrentSnapshot.GetText();
        }
    }
}
