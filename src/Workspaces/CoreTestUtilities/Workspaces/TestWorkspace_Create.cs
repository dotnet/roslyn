// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public partial class TestWorkspace
    {
        public static TestWorkspace Create(string xmlDefinition, bool openDocuments = false, TestComposition composition = null)
            => Create(XElement.Parse(xmlDefinition), openDocuments, composition);

        public static TestWorkspace CreateWorkspace(
            XElement workspaceElement,
            bool openDocuments = true,
            TestComposition composition = null,
            string workspaceKind = null)
        {
            return Create(workspaceElement, openDocuments, composition, workspaceKind);
        }

        internal static TestWorkspace Create(
            XElement workspaceElement,
            bool openDocuments = true,
            TestComposition composition = null,
            string workspaceKind = null,
            IDocumentServiceProvider documentServiceProvider = null,
            bool ignoreUnchangeableDocumentsWhenApplyingChanges = true)
        {
            var workspace = new TestWorkspace(composition, workspaceKind, ignoreUnchangeableDocumentsWhenApplyingChanges: ignoreUnchangeableDocumentsWhenApplyingChanges);
            workspace.InitializeDocuments(workspaceElement, openDocuments, documentServiceProvider);
            return workspace;
        }

        /// <summary>
        /// Creates a single buffer in a workspace.
        /// </summary>
        /// <param name="content">Lines of text, the buffer contents</param>
        internal static TestWorkspace Create(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string content)
        {
            return Create(language, compilationOptions, parseOptions, [content]);
        }

        /// <summary>
        /// Creates a single buffer in a workspace.
        /// </summary>
        /// <param name="content">Lines of text, the buffer contents</param>
        internal static TestWorkspace Create(
            string workspaceKind,
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string content)
        {
            return Create(workspaceKind, language, compilationOptions, parseOptions, [content]);
        }

        /// <param name="files">Can pass in multiple file contents: files will be named test1.cs, test2.cs, etc.</param>
        internal static TestWorkspace Create(
            string workspaceKind,
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params string[] files)
        {
            return Create(language, compilationOptions, parseOptions, files, workspaceKind: workspaceKind);
        }

        internal static TestWorkspace Create(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string[] files,
            string[] sourceGeneratedFiles = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            string workspaceKind = null,
            string extension = null,
            bool commonReferences = true,
            bool isMarkup = true,
            bool openDocuments = false,
            IDocumentServiceProvider documentServiceProvider = null)
        {
            var workspaceElement = CreateWorkspaceElement(language, compilationOptions, parseOptions, files, sourceGeneratedFiles, metadataReferences, extension, commonReferences, isMarkup);
            return Create(workspaceElement, openDocuments, composition, workspaceKind, documentServiceProvider);
        }

        internal static TestWorkspace CreateWithSingleEmptySourceFile(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            TestComposition composition)
        {
            var documentElements = new[]
            {
                CreateDocumentElement(code: "", filePath: GetDefaultTestSourceDocumentName(index: 0, GetSourceFileExtension(language, parseOptions)), parseOptions: parseOptions)
            };

            var workspaceElement = CreateWorkspaceElement(
                CreateProjectElement("Test", language, commonReferences: true, parseOptions, compilationOptions, documentElements));

            return Create(workspaceElement, composition: composition);
        }

        #region C#

        public static TestWorkspace CreateCSharp(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool isMarkup = true,
            bool openDocuments = false)
        {
            return CreateCSharp([file], [], parseOptions, compilationOptions, composition, metadataReferences, isMarkup, openDocuments);
        }

        public static TestWorkspace CreateCSharp(
            string[] files,
            string[] sourceGeneratedFiles = null,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool isMarkup = true,
            bool openDocuments = false)
        {
            return Create(LanguageNames.CSharp, compilationOptions, parseOptions, files, sourceGeneratedFiles, composition, metadataReferences, isMarkup: isMarkup, openDocuments: openDocuments);
        }

        #endregion

        #region VB

        public static TestWorkspace CreateVisualBasic(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool openDocuments = false)
        {
            return CreateVisualBasic([file], [], parseOptions, compilationOptions, composition, metadataReferences, openDocuments);
        }

        public static TestWorkspace CreateVisualBasic(
            string[] files,
            string[] sourceGeneratedFiles = null,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool openDocuments = false)
        {
            return Create(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, sourceGeneratedFiles, composition, metadataReferences, openDocuments: openDocuments);
        }

        #endregion
    }
}
