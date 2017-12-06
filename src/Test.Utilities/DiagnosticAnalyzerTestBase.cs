// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;
using TestResources.NetFX;
using Xunit;

namespace Test.Utilities
{
    public abstract class DiagnosticAnalyzerTestBase
    {
        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference s_systemXmlReference = MetadataReference.CreateFromFile(typeof(System.Xml.XmlDocument).Assembly.Location);
        private static readonly MetadataReference s_systemXmlDataReference = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        private static readonly MetadataReference s_csharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference s_visualBasicSymbolsReference = MetadataReference.CreateFromFile(typeof(VisualBasicCompilation).Assembly.Location);
        private static readonly MetadataReference s_visualBasicReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Devices.ComputerInfo).Assembly.Location);
        private static readonly MetadataReference s_codeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference s_workspacesReference = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
        private static readonly MetadataReference s_systemDiagnosticsDebugReference = MetadataReference.CreateFromFile(typeof(Debug).Assembly.Location);
        private static readonly MetadataReference s_immutableCollectionsReference = MetadataReference.CreateFromFile(typeof(ImmutableArray<int>).Assembly.Location);
        private static readonly MetadataReference s_systemDataReference = MetadataReference.CreateFromFile(typeof(System.Data.DataSet).Assembly.Location);
        protected static readonly CompilationOptions s_CSharpDefaultOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        protected static readonly CompilationOptions s_CSharpUnsafeCodeDefaultOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true);
        protected static readonly CompilationOptions s_visualBasicDefaultOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal const string DefaultFilePathPrefix = "Test";
        internal const string CSharpDefaultFileExt = "cs";
        internal const string VisualBasicDefaultExt = "vb";
        internal static readonly string CSharpDefaultFilePath = DefaultFilePathPrefix + 0 + "." + CSharpDefaultFileExt;
        internal static readonly string VisualBasicDefaultFilePath = DefaultFilePathPrefix + 0 + "." + VisualBasicDefaultExt;

        private const string TestProjectName = "TestProject";

        protected const TestValidationMode DefaultTestValidationMode = TestValidationMode.AllowCompileWarnings;

        /// <summary>
        /// Return the C# diagnostic analyzer to get analyzer diagnostics.
        /// This may return null when used in context of verifying a code fix for compiler diagnostics.
        /// </summary>
        /// <returns></returns>
        protected abstract DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer();

        /// <summary>
        /// Return the VB diagnostic analyzer to get analyzer diagnostics.
        /// This may return null when used in context of verifying a code fix for compiler diagnostics.
        /// </summary>
        /// <returns></returns>
        protected abstract DiagnosticAnalyzer GetBasicDiagnosticAnalyzer();

        private static MetadataReference s_systemRuntimeFacadeRef;
        public static MetadataReference SystemRuntimeFacadeRef
        {
            get
            {
                if (s_systemRuntimeFacadeRef == null)
                {
                    s_systemRuntimeFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Runtime).GetReference(display: "System.Runtime.dll");
                }

                return s_systemRuntimeFacadeRef;
            }
        }

        private static MetadataReference s_systemThreadingFacadeRef;
        public static MetadataReference SystemThreadingFacadeRef
        {
            get
            {
                if (s_systemThreadingFacadeRef == null)
                {
                    s_systemThreadingFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Threading).GetReference(display: "System.Threading.dll");
                }

                return s_systemThreadingFacadeRef;
            }
        }

        private static MetadataReference s_systemThreadingTasksFacadeRef;
        public static MetadataReference SystemThreadingTaskFacadeRef
        {
            get
            {
                if (s_systemThreadingTasksFacadeRef == null)
                {
                    s_systemThreadingTasksFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Threading_Tasks).GetReference(display: "System.Threading.Tasks.dll");
                }

                return s_systemThreadingTasksFacadeRef;
            }
        }

        protected bool PrintActualDiagnosticsOnFailure { get; set; }

        // It is assumed to be of the format, Get<RuleId>CSharpResultAt(line: {0}, column: {1}, message: {2})
        private string ExpectedDiagnosticsAssertionTemplate { get; set; }

        protected static DiagnosticResult GetGlobalResult(string id, string message)
        {
            return new DiagnosticResult
            {
                Id = id,
                Severity = DiagnosticHelpers.DefaultDiagnosticSeverity,
                Message = message
            };
        }

        protected static DiagnosticResult GetGlobalResult(DiagnosticDescriptor rule, params string[] messageArguments)
        {
            return new DiagnosticResult
            {
                Id = rule.Id,
                Severity = rule.DefaultSeverity,
                Message = string.Format(rule.MessageFormat.ToString(), messageArguments)
            };
        }

        protected static DiagnosticResult GetBasicResultAt(int line, int column, string id, string message)
        {
            return GetResultAt(VisualBasicDefaultFilePath, line, column, id, message);
        }

        protected static DiagnosticResult GetBasicResultAt(int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return GetResultAt(VisualBasicDefaultFilePath, line, column, rule, messageArguments);
        }

        protected static DiagnosticResult GetCSharpResultAt(int line, int column, string id, string message)
        {
            return GetResultAt(CSharpDefaultFilePath, line, column, id, message);
        }

        protected static DiagnosticResult GetCSharpResultAt(string id, string message, params string[] locationStrings)
        {
            return GetResultAt(CSharpDefaultFilePath, id, message, locationStrings);
        }

        protected static DiagnosticResult GetCSharpResultAt(int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return GetResultAt(CSharpDefaultFilePath, line, column, rule, messageArguments);
        }

        protected static DiagnosticResult GetAdditionalFileResultAt(int line, int column, string additionalFilePath, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            return GetResultAt(additionalFilePath, line, column, rule, messageArguments);
        }

        private static DiagnosticResult GetResultAt(string path, int line, int column, string id, string message)
        {
            var location = new DiagnosticResultLocation(path, line, column);

            return new DiagnosticResult
            {
                Locations = new[] { location },
                Id = id,
                Severity = DiagnosticHelpers.DefaultDiagnosticSeverity,
                Message = message
            };
        }

        protected static DiagnosticResult GetResultAt(string path, string id, string message, params string[] locationStrings)
        {
            return new DiagnosticResult
            {
                Locations = ParseResultLocations(path, locationStrings),
                Id = id,
                Severity = DiagnosticHelpers.DefaultDiagnosticSeverity,
                Message = message
            };
        }

        private static DiagnosticResult GetResultAt(string path, int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
        {
            var location = new DiagnosticResultLocation(path, line, column);

            return new DiagnosticResult
            {
                Locations = new[] { location },
                Id = rule.Id,
                Severity = rule.DefaultSeverity,
                Message = string.Format(rule.MessageFormat.ToString(), messageArguments)
            };
        }

        private static DiagnosticResultLocation[] ParseResultLocations(string defaultPath, string[] locationStrings)
        {
            var builder = new List<DiagnosticResultLocation>();

            foreach (string str in locationStrings)
            {
                string[] tokens = str.Split('(', ',', ')');
                Assert.True(tokens.Length == 4, "Location string must be of the format 'FileName.cs(line,column)' or just 'line,column' to use " + defaultPath + " as the file name.");

                string path = tokens[0] == "" ? defaultPath : tokens[0];

                Assert.True(int.TryParse(tokens[1], out int line) && line >= -1, "Line must be >= -1 in location string: " + str);

                Assert.True(int.TryParse(tokens[2], out int column) && line >= -1, "Column must be >= -1 in location string: " + str);

                builder.Add(new DiagnosticResultLocation(path, line, column));
            }

            return builder.ToArray();
        }

        protected void VerifyCSharpUnsafeCode(string source, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), DefaultTestValidationMode, true, compilationOptions: null, expected: expected);
        }

        protected void VerifyCSharp(string source, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyCSharp(string source, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions, expected: expected);
        }

        protected void VerifyCSharp(string source, TestValidationMode validationMode, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), validationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyCSharp(string source, TestValidationMode validationMode, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), validationMode, false, compilationOptions, expected: expected);
        }

        protected void VerifyCSharp(string source, bool addLanguageSpecificCodeAnalysisReference, TestValidationMode validationMode = DefaultTestValidationMode, params DiagnosticResult[] expected)
        {
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), addLanguageSpecificCodeAnalysisReference, validationMode, compilationOptions: null, expected: expected);
        }

        protected void VerifyCSharp(string[] sources, params DiagnosticResult[] expected)
        {
            Verify(sources.ToFileAndSource(), LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyCSharp(FileAndSource[] sources, params DiagnosticResult[] expected)
        {
            Verify(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyBasic(string source, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyBasic(string source, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions, expected: expected);
        }

        protected void VerifyBasic(string source, TestValidationMode validationMode, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), validationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyBasic(string source, TestValidationMode validationMode, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }.ToFileAndSource(), LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), validationMode, false, compilationOptions, expected: expected);
        }

        protected void VerifyBasic(string source, bool addLanguageSpecificCodeAnalysisReference, TestValidationMode validationMode = DefaultTestValidationMode, params DiagnosticResult[] expected)
        {
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), addLanguageSpecificCodeAnalysisReference, validationMode, compilationOptions: null, expected: expected);
        }

        protected void VerifyBasic(string[] sources, params DiagnosticResult[] expected)
        {
            Verify(sources.ToFileAndSource(), LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions: null, expected: expected);
        }

        protected void VerifyBasic(FileAndSource[] sources, params DiagnosticResult[] expected)
        {
            Verify(sources, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), DefaultTestValidationMode, false, compilationOptions: null, expected: expected);
        }

        protected void Verify(string source, string language, DiagnosticAnalyzer analyzer, IEnumerable<TestAdditionalDocument> additionalFiles, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            var diagnostics = GetSortedDiagnostics(new[] { source }.ToFileAndSource(), language, analyzer, compilationOptions, additionalFiles: additionalFiles);
            diagnostics.Verify(analyzer, PrintActualDiagnosticsOnFailure, ExpectedDiagnosticsAssertionTemplate, expected);
        }

        private void Verify(string source, string language, DiagnosticAnalyzer analyzer, bool addLanguageSpecificCodeAnalysisReference, TestValidationMode validationMode, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            var diagnostics = GetSortedDiagnostics(new[] { source }.ToFileAndSource(), language, analyzer, compilationOptions, addLanguageSpecificCodeAnalysisReference: addLanguageSpecificCodeAnalysisReference, validationMode: validationMode);
            diagnostics.Verify(analyzer, PrintActualDiagnosticsOnFailure, ExpectedDiagnosticsAssertionTemplate, expected);
        }

        private void Verify(FileAndSource[] sources, string language, DiagnosticAnalyzer analyzer, TestValidationMode validationMode, bool allowUnsafeCode, CompilationOptions compilationOptions, params DiagnosticResult[] expected)
        {
            var diagnostics = GetSortedDiagnostics(sources, language, analyzer, compilationOptions, validationMode, allowUnsafeCode: allowUnsafeCode);
            diagnostics.Verify(analyzer, PrintActualDiagnosticsOnFailure, ExpectedDiagnosticsAssertionTemplate, expected);
        }

        private static Tuple<Document[], bool, TextSpan?[]> GetDocumentsAndSpans(FileAndSource[] sources, string language, CompilationOptions compilationOptions, bool addLanguageSpecificCodeAnalysisReference = true, string projectName = TestProjectName, bool allowUnsafeCode = false)
        {
            Assert.True(language == LanguageNames.CSharp || language == LanguageNames.VisualBasic, "Unsupported language");

            var spans = new TextSpan?[sources.Length];
            bool useSpans = false;

            for (int i = 0; i < sources.Length; i++)
            {
                MarkupTestFile.GetPositionAndSpan(sources[i].Source, out string source, out int? pos, out TextSpan? span);

                sources[i].Source = source;
                spans[i] = span;

                if (span != null)
                {
                    useSpans = true;
                }
            }

            Project project = CreateProject(sources, language, addLanguageSpecificCodeAnalysisReference, null, projectName, allowUnsafeCode, compilationOptions: compilationOptions);
            Document[] documents = project.Documents.ToArray();
            Assert.Equal(sources.Length, documents.Length);

            return Tuple.Create(documents, useSpans, spans);
        }

        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp, bool addLanguageSpecificCodeAnalysisReference = true, bool allowUnsafeCode = false)
        {
            return CreateProject(new[] { source }.ToFileAndSource(), language, addLanguageSpecificCodeAnalysisReference, allowUnsafeCode: allowUnsafeCode).Documents.First();
        }

        protected static Project CreateProject(string[] sources, string language = LanguageNames.CSharp, bool addLanguageSpecificCodeAnalysisReference = true, Solution addToSolution = null)
        {
            return CreateProject(sources.ToFileAndSource(), language, addLanguageSpecificCodeAnalysisReference, addToSolution);
        }

        private static Project CreateProject(
            FileAndSource[] sources,
            string language = LanguageNames.CSharp,
            bool addLanguageSpecificCodeAnalysisReference = true,
            Solution addToSolution = null,
            string projectName = TestProjectName,
            bool allowUnsafeCode = false,
            CompilationOptions compilationOptions = null)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;
            CompilationOptions options = compilationOptions ??
                (language == LanguageNames.CSharp
                    ? (allowUnsafeCode ? s_CSharpUnsafeCodeDefaultOptions : s_CSharpDefaultOptions) :
                      s_visualBasicDefaultOptions);

            ProjectId projectId = ProjectId.CreateNewId(debugName: projectName);

            Project project = (addToSolution ?? new AdhocWorkspace().CurrentSolution)
                .AddProject(projectId, projectName, projectName, language)
                .AddMetadataReference(projectId, s_corlibReference)
                .AddMetadataReference(projectId, s_systemCoreReference)
                .AddMetadataReference(projectId, s_systemXmlReference)
                .AddMetadataReference(projectId, s_systemXmlDataReference)
                .AddMetadataReference(projectId, s_codeAnalysisReference)
                .AddMetadataReference(projectId, SystemRuntimeFacadeRef)
                .AddMetadataReference(projectId, SystemThreadingFacadeRef)
                .AddMetadataReference(projectId, SystemThreadingTaskFacadeRef)
                .AddMetadataReference(projectId, s_immutableCollectionsReference)
                .AddMetadataReference(projectId, s_workspacesReference)
                .AddMetadataReference(projectId, s_systemDiagnosticsDebugReference)
                .AddMetadataReference(projectId, s_systemDataReference)
                .WithProjectCompilationOptions(projectId, options)
                .GetProject(projectId);

            if (addLanguageSpecificCodeAnalysisReference)
            {
                MetadataReference symbolsReference = language == LanguageNames.CSharp ? s_csharpSymbolsReference : s_visualBasicSymbolsReference;
                project = project.AddMetadataReference(symbolsReference);
            }

            if (language == LanguageNames.VisualBasic)
            {
                project = project.AddMetadataReference(s_visualBasicReference);
            }

            int count = 0;
            foreach (FileAndSource source in sources)
            {
                string newFileName = source.FilePath ?? fileNamePrefix + count++ + "." + fileExt;
                DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                project = project.AddDocument(newFileName, SourceText.From(source.Source)).Project;
            }

            return project;
        }

        protected static Diagnostic[] GetSortedDiagnostics(FileAndSource[] sources, string language, DiagnosticAnalyzer analyzer, CompilationOptions compilationOptions, TestValidationMode validationMode = DefaultTestValidationMode, bool addLanguageSpecificCodeAnalysisReference = true, bool allowUnsafeCode = false, string projectName = TestProjectName, IEnumerable<TestAdditionalDocument> additionalFiles = null)
        {
            Tuple<Document[], bool, TextSpan?[]> documentsAndUseSpan = GetDocumentsAndSpans(sources, language, compilationOptions, addLanguageSpecificCodeAnalysisReference, projectName, allowUnsafeCode);
            Document[] documents = documentsAndUseSpan.Item1;
            bool useSpans = documentsAndUseSpan.Item2;
            TextSpan?[] spans = documentsAndUseSpan.Item3;
            return GetSortedDiagnostics(analyzer, documents, useSpans ? spans : null, validationMode, additionalFiles);
        }

        protected static Diagnostic[] GetSortedDiagnostics(DiagnosticAnalyzer analyzerOpt, Document document, TextSpan?[] spans = null, IEnumerable<TestAdditionalDocument> additionalFiles = null)
        {
            return GetSortedDiagnostics(analyzerOpt, new[] { document }, spans, additionalFiles: additionalFiles);
        }

        protected static Diagnostic[] GetSortedDiagnostics(DiagnosticAnalyzer analyzerOpt, Document[] documents, TextSpan?[] spans = null, TestValidationMode validationMode = DefaultTestValidationMode, IEnumerable<TestAdditionalDocument> additionalFiles = null)
        {
            if (analyzerOpt == null)
            {
                return Array.Empty<Diagnostic>();
            }

            var projects = new HashSet<Project>();
            foreach (Document document in documents)
            {
                projects.Add(document.Project);
            }

            var analyzerOptions = additionalFiles != null ? new AnalyzerOptions(additionalFiles.ToImmutableArray<AdditionalText>()) : null;
            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            foreach (Project project in projects)
            {
                Compilation compilation = project.GetCompilationAsync().Result;
                compilation = EnableAnalyzer(analyzerOpt, compilation);

                ImmutableArray<Diagnostic> diags = compilation.GetAnalyzerDiagnostics(new[] { analyzerOpt }, validationMode, analyzerOptions);
                if (spans == null)
                {
                    diagnostics.AddRange(diags);
                }
                else
                {
                    Debug.Assert(spans.Length == documents.Length);
                    foreach (Diagnostic diag in diags)
                    {
                        if (diag.Location == Location.None || diag.Location.IsInMetadata)
                        {
                            diagnostics.Add(diag);
                        }
                        else
                        {
                            for (int i = 0; i < documents.Length; i++)
                            {
                                Document document = documents[i];
                                SyntaxTree tree = document.GetSyntaxTreeAsync().Result;
                                if (tree == diag.Location.SourceTree)
                                {
                                    TextSpan? span = spans[i];
                                    if (span == null || span.Value.Contains(diag.Location.SourceSpan))
                                    {
                                        diagnostics.Add(diag);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Diagnostic[] results = diagnostics.AsEnumerable().OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            diagnostics.Free();
            return results;
        }

        private static Compilation EnableAnalyzer(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            return compilation.WithOptions(
                compilation
                    .Options
                    .WithSpecificDiagnosticOptions(
                        analyzer
                            .SupportedDiagnostics
                            .Select(x => KeyValuePair.Create(x.Id, ReportDiagnostic.Default))
                            .ToImmutableDictionaryOrEmpty()));
        }
    }

    // Justification for suppression: We are not going to compare FileAndSource objects for equality.
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct FileAndSource
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public string FilePath { get; set; }
        public string Source { get; set; }
    }
}
