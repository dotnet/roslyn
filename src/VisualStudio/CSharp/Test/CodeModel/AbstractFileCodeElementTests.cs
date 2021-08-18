// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Roslyn.Test.Utilities;
using SyntaxNodeKey = Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.SyntaxNodeKey;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    /// <summary>
    /// Base class of a all test-containing classes. Automatically creates a FileCodeModel for testing with the given
    /// file.
    /// </summary>
    [UseExportProvider]
    public abstract class AbstractFileCodeElementTests : IDisposable
    {
        private readonly string _contents;
        private (TestWorkspace workspace, VisualStudioWorkspace extraWorkspaceToDisposeButNotUse, FileCodeModel fileCodeModel)? _workspaceAndCodeModel;

        protected AbstractFileCodeElementTests(string contents)
        {
            _contents = contents;
        }

        public (TestWorkspace workspace, VisualStudioWorkspace extraWorkspaceToDisposeButNotUse, FileCodeModel fileCodeModel) WorkspaceAndCodeModel
        {
            get
            {
                return _workspaceAndCodeModel ??= CreateWorkspaceAndFileCodeModelAsync(_contents);
            }
        }

        protected TestWorkspace GetWorkspace()
        {
            return WorkspaceAndCodeModel.workspace;
        }

        private VisualStudioWorkspace GetExtraWorkspaceToDisposeButNotUse()
        {
            return WorkspaceAndCodeModel.extraWorkspaceToDisposeButNotUse;
        }

        protected FileCodeModel GetCodeModel()
        {
            return WorkspaceAndCodeModel.fileCodeModel;
        }

        protected Microsoft.CodeAnalysis.Solution GetCurrentSolution()
            => GetWorkspace().CurrentSolution;

        protected Microsoft.CodeAnalysis.Project GetCurrentProject()
            => GetCurrentSolution().Projects.Single();

        protected Microsoft.CodeAnalysis.Document GetCurrentDocument()
            => GetCurrentProject().Documents.Single();

        protected static (TestWorkspace workspace, VisualStudioWorkspace extraWorkspaceToDisposeButNotUse, FileCodeModel fileCodeModel) CreateWorkspaceAndFileCodeModelAsync(string file)
            => FileCodeModelTestHelpers.CreateWorkspaceAndFileCodeModel(file);

        protected CodeElement GetCodeElement(params object[] path)
        {
            WpfTestRunner.RequireWpfFact($"Tests create {nameof(CodeElement)}s which use the affinitized {nameof(CleanableWeakComHandleTable<SyntaxNodeKey, CodeElement>)}");

            if (path.Length == 0)
            {
                throw new ArgumentException("path must be non-empty.", nameof(path));
            }

            var codeElement = (GetCodeModel()).CodeElements.Item(path[0]);

            foreach (var pathElement in path.Skip(1))
            {
                codeElement = codeElement.Children.Item(pathElement);
            }

            return codeElement;
        }

        public void Dispose()
        {
            GetExtraWorkspaceToDisposeButNotUse().Dispose();
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
