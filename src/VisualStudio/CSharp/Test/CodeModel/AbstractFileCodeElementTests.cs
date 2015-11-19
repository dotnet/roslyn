// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
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
        protected TestWorkspace Workspace { get; }
        protected FileCodeModel CodeModel { get; }

        protected Microsoft.CodeAnalysis.Solution CurrentSolution { get; }
        protected Microsoft.CodeAnalysis.Project CurrentProject { get; }
        protected Microsoft.CodeAnalysis.Document CurrentDocument { get; }

        public AbstractFileCodeElementTests(string file)
        {
            var pair = FileCodeModelTestHelpers.CreateWorkspaceAndFileCodeModel(file);
            Workspace = pair.Item1;
            CodeModel = pair.Item2;

            CurrentSolution = Workspace.CurrentSolution;
            CurrentProject = CurrentSolution.Projects.Single();
            CurrentDocument = CurrentProject.Documents.Single();
        }

        protected CodeElement GetCodeElement(params object[] path)
        {
            if (path.Length == 0)
            {
                throw new ArgumentException("path must be non-empty.", "path");
            }

            CodeElement codeElement = CodeModel.CodeElements.Item(path[0]);

            foreach (var pathElement in path.Skip(1))
            {
                codeElement = codeElement.Children.Item(pathElement);
            }

            return codeElement;
        }

        public void Dispose()
        {
            Workspace.Dispose();
        }

        /// <summary>
        /// Returns the current text of the test buffer.
        /// </summary>
        protected string GetFileText()
        {
            return Workspace.Documents.Single().GetTextBuffer().CurrentSnapshot.GetText();
        }
    }
}
