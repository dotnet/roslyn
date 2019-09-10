// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string CryptoKeyFileAttributeName = "CryptoKeyFile";
        private const string StrongNameProviderAttributeName = "StrongNameProvider";
        private const string DelaySignAttributeName = "DelaySign";
        private const string ParseOptionsElementName = "ParseOptions";
        private const string LanguageVersionAttributeName = "LanguageVersion";
        private const string FeaturesAttributeName = "Features";
        private const string DocumentationModeAttributeName = "DocumentationMode";
        private const string DocumentElementName = "Document";
        private const string AdditionalDocumentElementName = "AdditionalDocument";
        private const string AnalyzerConfigDocumentElementName = "AnalyzerConfigDocument";
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
        private const string CheckOverflowAttributeName = "CheckOverflow";
        private const string AllowUnsafeAttributeName = "AllowUnsafe";
        private const string OutputKindName = "OutputKind";
        private const string NullableAttributeName = "Nullable";

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
            return Create(language, compilationOptions, parseOptions, new[] { content });
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
            return Create(workspaceKind, language, compilationOptions, parseOptions, new[] { content });
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
            string content,
            ExportProvider exportProvider)
        {
            return Create(language, compilationOptions, parseOptions, new[] { content }, exportProvider: exportProvider, workspaceKind: workspaceKind);
        }

        /// <param name="files">Can pass in multiple file contents: files will be named test1.cs, test2.cs, etc.</param>
        internal static TestWorkspace Create(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params string[] files)
        {
            return Create(language, compilationOptions, parseOptions, files, exportProvider: null);
        }

        /// <param name="files">Can pass in multiple file contents: files will be named test1.cs, test2.cs, etc.</param>
        internal static TestWorkspace Create(
            string workspaceKind,
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params string[] files)
        {
            return Create(language, compilationOptions, parseOptions, files, exportProvider: null, workspaceKind: workspaceKind);
        }

        internal static string GetDefaultTestSourceDocumentName(int index, string extension)
           => "test" + (index + 1) + extension;

        internal static TestWorkspace Create(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            string[] files,
            ExportProvider exportProvider,
            string[] metadataReferences = null,
            string workspaceKind = null,
            string extension = null,
            bool commonReferences = true,
            bool openDocuments = true)
        {
            var documentElements = new List<XElement>();
            var index = 0;

            if (extension == null)
            {
                extension = language == LanguageNames.CSharp
                ? CSharpExtension
                : VisualBasicExtension;
            }

            foreach (var file in files)
            {
                documentElements.Add(CreateDocumentElement(file, GetDefaultTestSourceDocumentName(index++, extension), parseOptions));
            }

            metadataReferences = metadataReferences ?? Array.Empty<string>();
            foreach (var reference in metadataReferences)
            {
                documentElements.Add(CreateMetadataReference(reference));
            }

            var workspaceElement = CreateWorkspaceElement(
                CreateProjectElement(compilationOptions?.ModuleName ?? "Test", language, commonReferences, parseOptions, compilationOptions, documentElements));

            return Create(workspaceElement, openDocuments: openDocuments, exportProvider: exportProvider, workspaceKind: workspaceKind);
        }

        internal static TestWorkspace Create(
            string language,
            CompilationOptions compilationOptions,
            ParseOptions[] parseOptions,
            string[] files,
            ExportProvider exportProvider)
        {
            Debug.Assert(parseOptions == null || (files.Length == parseOptions.Length), "Please specify a parse option for each file.");

            var documentElements = new List<XElement>();
            var index = 0;
            var extension = "";

            for (var i = 0; i < files.Length; i++)
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

                documentElements.Add(CreateDocumentElement(files[i], GetDefaultTestSourceDocumentName(index++, extension), parseOptions == null ? null : parseOptions[i]));
            }

            var workspaceElement = CreateWorkspaceElement(
                CreateProjectElement("Test", language, true, parseOptions.FirstOrDefault(), compilationOptions, documentElements));

            return Create(workspaceElement, exportProvider: exportProvider);
        }

        #region C#

        public static TestWorkspace CreateCSharp(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null,
            bool openDocuments = true)
        {
            return CreateCSharp(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences, openDocuments);
        }

        public static TestWorkspace CreateCSharp(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null,
            bool openDocuments = true)
        {
            return Create(LanguageNames.CSharp, compilationOptions, parseOptions, files, exportProvider, metadataReferences, openDocuments: openDocuments);
        }

        public static TestWorkspace CreateCSharp2(
            string[] files,
            ParseOptions[] parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return Create(LanguageNames.CSharp, compilationOptions, parseOptions, files, exportProvider);
        }

        #endregion

        #region VB

        public static TestWorkspace CreateVisualBasic(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null,
            bool openDocuments = true)
        {
            return CreateVisualBasic(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences, openDocuments);
        }

        public static TestWorkspace CreateVisualBasic(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null,
            bool openDocuments = true)
        {
            return Create(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider, metadataReferences, openDocuments: openDocuments);
        }

        /// <param name="files">Can pass in multiple file contents with individual source kind: files will be named test1.vb, test2.vbx, etc.</param>
        public static TestWorkspace CreateVisualBasic(
            string[] files,
            ParseOptions[] parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return Create(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider);
        }

        #endregion
    }
}
