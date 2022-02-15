// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

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
        private const string CommonReferencesWithoutValueTupleAttributeName = "CommonReferencesWithoutValueTuple";
        private const string CommonReferencesWinRTAttributeName = "CommonReferencesWinRT";
        private const string CommonReferencesNet45AttributeName = "CommonReferencesNet45";
        private const string CommonReferencesPortableAttributeName = "CommonReferencesPortable";
        private const string CommonReferencesNetCoreAppName = "CommonReferencesNetCoreApp";
        private const string CommonReferencesNet6Name = "CommonReferencesNet6";
        private const string CommonReferencesNetStandard20Name = "CommonReferencesNetStandard20";
        private const string ReferencesOnDiskAttributeName = "ReferencesOnDisk";
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
        private const string MarkupAttributeName = "Markup";
        private const string PreprocessorSymbolsAttributeName = "PreprocessorSymbols";
        private const string AnalyzerDisplayAttributeName = "Name";
        private const string AnalyzerFullPathAttributeName = "FullPath";
        private const string AliasAttributeName = "Alias";
        private const string ProjectNameAttribute = "Name";
        private const string CheckOverflowAttributeName = "CheckOverflow";
        private const string AllowUnsafeAttributeName = "AllowUnsafe";
        private const string OutputKindName = "OutputKind";
        private const string NullableAttributeName = "Nullable";
        private const string DocumentFromSourceGeneratorElementName = "DocumentFromSourceGenerator";

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
            string[] sourceGeneratedFiles = null,
            ExportProvider exportProvider = null,
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
            return Create(workspaceElement, openDocuments, exportProvider, composition, workspaceKind, documentServiceProvider);
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
            string extension;

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
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool isMarkup = true,
            bool openDocuments = false)
        {
            return CreateCSharp(new[] { file }, Array.Empty<string>(), parseOptions, compilationOptions, exportProvider, composition, metadataReferences, isMarkup, openDocuments);
        }

        public static TestWorkspace CreateCSharp(
            string[] files,
            string[] sourceGeneratedFiles = null,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool isMarkup = true,
            bool openDocuments = false)
        {
            return Create(LanguageNames.CSharp, compilationOptions, parseOptions, files, sourceGeneratedFiles, exportProvider, composition, metadataReferences, isMarkup: isMarkup, openDocuments: openDocuments);
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
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool openDocuments = false)
        {
            return CreateVisualBasic(new[] { file }, Array.Empty<string>(), parseOptions, compilationOptions, exportProvider, composition, metadataReferences, openDocuments);
        }

        public static TestWorkspace CreateVisualBasic(
            string[] files,
            string[] sourceGeneratedFiles = null,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            TestComposition composition = null,
            string[] metadataReferences = null,
            bool openDocuments = false)
        {
            return Create(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, sourceGeneratedFiles, exportProvider, composition, metadataReferences, openDocuments: openDocuments);
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
