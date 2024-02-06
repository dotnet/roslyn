// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using EnvDTE;
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
    public abstract class AbstractFileCodeElementTests
    {
        private static readonly ConditionalWeakTable<EditorTestWorkspace, FileCodeModel> s_codeModelForWorkspace = new();

        private readonly string _contents;

        protected AbstractFileCodeElementTests(string contents)
        {
            _contents = contents;
        }

        protected static FileCodeModel GetCodeModel(EditorTestWorkspace workspace)
        {
            if (!s_codeModelForWorkspace.TryGetValue(workspace, out var codeModel))
                throw new InvalidOperationException();

            return codeModel;
        }

        protected Microsoft.CodeAnalysis.Solution GetCurrentSolution(EditorTestWorkspace workspace)
            => workspace.CurrentSolution;

        protected Microsoft.CodeAnalysis.Project GetCurrentProject(EditorTestWorkspace workspace)
            => GetCurrentSolution(workspace).Projects.Single();

        protected Microsoft.CodeAnalysis.Document GetCurrentDocument(EditorTestWorkspace workspace)
            => GetCurrentProject(workspace).Documents.Single();

        protected EditorTestWorkspace CreateWorkspaceAndFileCodeModel()
        {
            var (workspace, codeModel) = FileCodeModelTestHelpers.CreateWorkspaceAndFileCodeModel(_contents);
            s_codeModelForWorkspace.Add(workspace, codeModel);
            return workspace;
        }

        protected CodeElement GetCodeElement(EditorTestWorkspace workspace, params object[] path)
        {
            WpfTestRunner.RequireWpfFact($"Tests create {nameof(CodeElement)}s which use the affinitized {nameof(CleanableWeakComHandleTable<SyntaxNodeKey, CodeElement>)}");

            if (path.Length == 0)
            {
                throw new ArgumentException("path must be non-empty.", nameof(path));
            }

            var codeElement = GetCodeModel(workspace).CodeElements.Item(path[0]);

            foreach (var pathElement in path.Skip(1))
            {
                codeElement = codeElement.Children.Item(pathElement);
            }

            return codeElement;
        }

        /// <summary>
        /// Returns the current text of the test buffer.
        /// </summary>
        protected string GetFileText(EditorTestWorkspace workspace)
        {
            return workspace.Documents.Single().GetTextBuffer().CurrentSnapshot.GetText();
        }
    }
}
