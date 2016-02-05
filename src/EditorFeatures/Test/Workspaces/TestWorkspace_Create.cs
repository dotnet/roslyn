// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public partial class TestWorkspace
    {
        private const string CSharpExtension = ".cs";
        private const string CSharpScriptExtension = ".csx";
        private const string VisualBasicExtension = ".vb";
        private const string VisualBasicScriptExtension = ".vbx";

        private const string WorkspaceElementName = "Workspace";
        private const string ProjectElementName = "Project";
        private const string SubmissionElementName = "Submission";
        private const string MetadataReferenceElementName = "MetadataReference";
        private const string MetadataReferenceFromSourceElementName = "MetadataReferenceFromSource";
        private const string ProjectReferenceElementName = "ProjectReference";
        private const string CompilationOptionsElementName = "CompilationOptions";
        private const string RootNamespaceAttributeName = "RootNamespace";
        private const string OutputTypeAttributeName = "OutputType";
        private const string ReportDiagnosticAttributeName = "ReportDiagnostic";
        private const string ParseOptionsElementName = "ParseOptions";
        private const string LanguageVersionAttributeName = "LanguageVersion";
        private const string DocumentationModeAttributeName = "DocumentationMode";
        private const string DocumentElementName = "Document";
        private const string AnalyzerElementName = "Analyzer";
        private const string AssemblyNameAttributeName = "AssemblyName";
        private const string CommonReferencesAttributeName = "CommonReferences";
        private const string CommonReferencesWinRTAttributeName = "CommonReferencesWinRT";
        private const string CommonReferencesNet45AttributeName = "CommonReferencesNet45";
        private const string CommonReferencesPortableAttributeName = "CommonReferencesPortable";
        private const string CommonReferenceFacadeSystemRuntimeAttributeName = "CommonReferenceFacadeSystemRuntime";
        private const string FilePathAttributeName = "FilePath";
        private const string FoldersAttributeName = "Folders";
        private const string KindAttributeName = "Kind";
        private const string LanguageAttributeName = "Language";
        private const string GlobalImportElementName = "GlobalImport";
        private const string IncludeXmlDocCommentsAttributeName = "IncludeXmlDocComments";
        private const string IsLinkFileAttributeName = "IsLinkFile";
        private const string LinkAssemblyNameAttributeName = "LinkAssemblyName";
        private const string LinkProjectNameAttributeName = "LinkProjectName";
        private const string LinkFilePathAttributeName = "LinkFilePath";
        private const string PreprocessorSymbolsAttributeName = "PreprocessorSymbols";
        private const string AnalyzerDisplayAttributeName = "Name";
        private const string AnalyzerFullPathAttributeName = "FullPath";
        private const string AliasAttributeName = "Alias";
        private const string ProjectNameAttribute = "Name";

        /// <summary>
        /// Creates a single buffer in a workspace.
        /// </summary>
        /// <param name="content">Lines of text, the buffer contents</param>
        internal static Task<TestWorkspace> CreateAsync(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string content)
        {
            return CreateAsync(language, compilationOptions, parseOptions, new[] { content });
        }

        /// <summary>
        /// Creates a single buffer in a workspace.
        /// </summary>
        /// <param name="content">Lines of text, the buffer contents</param>
        internal static Task<TestWorkspace> CreateAsync(
            string workspaceKind,
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string content)
        {
            return CreateAsync(workspaceKind, language, compilationOptions, parseOptions, new[] { content });
        }

        /// <param name="files">Can pass in multiple file contents: files will be named test1.cs, test2.cs, etc.</param>
        internal static Task<TestWorkspace> CreateAsync(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params string[] files)
        {
            return CreateAsync(language, compilationOptions, parseOptions, files, exportProvider: null);
        }

        /// <param name="files">Can pass in multiple file contents: files will be named test1.cs, test2.cs, etc.</param>
        internal static Task<TestWorkspace> CreateAsync(
            string workspaceKind,
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params string[] files)
        {
            return CreateAsync(language, compilationOptions, parseOptions, files, exportProvider: null, workspaceKind: workspaceKind);
        }

        internal static async Task<TestWorkspace> CreateAsync(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string[] files,
            ExportProvider exportProvider,
            string[] metadataReferences = null,
            string workspaceKind = null,
            string extension = null,
            bool commonReferences = true)
        {
            var documentElements = new List<XElement>();
            var index = 1;

            if (extension == null)
            {
                extension = language == LanguageNames.CSharp
                ? CSharpExtension
                : VisualBasicExtension;
            }

            foreach (var file in files)
            {
                documentElements.Add(CreateDocumentElement(file, "test" + index++ + extension, parseOptions));
            }

            metadataReferences = metadataReferences ?? SpecializedCollections.EmptyArray<string>();
            foreach (var reference in metadataReferences)
            {
                documentElements.Add(CreateMetadataReference(reference));
            }

            var workspaceElement = CreateWorkspaceElement(
                CreateProjectElement(compilationOptions?.ModuleName ?? "Test", language, commonReferences, parseOptions, compilationOptions, documentElements));

            return await CreateAsync(workspaceElement, exportProvider: exportProvider, workspaceKind: workspaceKind);
        }

        internal static Task<TestWorkspace> CreateAsync(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions[] parseOptions,
            string[] files,
            ExportProvider exportProvider)
        {
            Contract.Requires(parseOptions == null || (files.Length == parseOptions.Length), "Please specify a parse option for each file.");

            var documentElements = new List<XElement>();
            var index = 1;
            var extension = "";

            for (int i = 0; i < files.Length; i++)
            {
                if (language == LanguageNames.CSharp)
                {
                    extension = parseOptions[i].Kind == SourceCodeKind.Regular
                        ? CSharpExtension
                        : CSharpScriptExtension;
                }
                else if (language == LanguageNames.VisualBasic)
                {
                    extension = parseOptions[i].Kind == SourceCodeKind.Regular
                        ? VisualBasicExtension
                        : VisualBasicScriptExtension;
                }
                else
                {
                    extension = language;
                }

                documentElements.Add(CreateDocumentElement(files[i], "test" + index++ + extension, parseOptions == null ? null : parseOptions[i]));
            }

            var workspaceElement = CreateWorkspaceElement(
                CreateProjectElement("Test", language, true, parseOptions.FirstOrDefault(), compilationOptions, documentElements));

            return CreateAsync(workspaceElement, exportProvider: exportProvider);
        }

        #region C#

        public static Task<TestWorkspace> CreateCSharpAsync(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateCSharpAsync(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateCSharpAsync(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateAsync(LanguageNames.CSharp, compilationOptions, parseOptions, files, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateCSharpAsync(
            string[] files,
            ParseOptions[] parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return CreateAsync(LanguageNames.CSharp, compilationOptions, parseOptions, files, exportProvider);
        }

        #endregion

        #region VB

        public static Task<TestWorkspace> CreateVisualBasicAsync(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateVisualBasicAsync(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateVisualBasicAsync(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateAsync(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider, metadataReferences);
        }

        /// <param name="files">Can pass in multiple file contents with individual source kind: files will be named test1.vb, test2.vbx, etc.</param>
        public static Task<TestWorkspace> CreateVisualBasicAsync(
            string[] files,
            ParseOptions[] parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return CreateAsync(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider);
        }

        #endregion
    }
}
