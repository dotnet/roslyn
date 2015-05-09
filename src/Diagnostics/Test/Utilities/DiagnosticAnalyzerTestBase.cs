// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public abstract class DiagnosticAnalyzerTestBase
    {
        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromAssembly(typeof(Enumerable).Assembly);
        private static readonly MetadataReference s_CSharpSymbolsReference = MetadataReference.CreateFromAssembly(typeof(CSharpCompilation).Assembly);
        private static readonly MetadataReference s_visualBasicSymbolsReference = MetadataReference.CreateFromAssembly(typeof(VisualBasicCompilation).Assembly);
        private static readonly MetadataReference s_codeAnalysisReference = MetadataReference.CreateFromAssembly(typeof(Compilation).Assembly);
        private static readonly MetadataReference s_immutableCollectionsReference = MetadataReference.CreateFromAssembly(typeof(ImmutableArray<int>).Assembly);
        private static readonly CompilationOptions s_CSharpDefaultOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        private static readonly CompilationOptions s_visualBasicDefaultOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string CSharpDefaultFilePath = DefaultFilePathPrefix + 0 + "." + CSharpDefaultFileExt;
        internal static string VisualBasicDefaultFilePath = DefaultFilePathPrefix + 0 + "." + VisualBasicDefaultExt;
        internal static string TestProjectName = "TestProject";

        protected abstract DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer();
        protected abstract DiagnosticAnalyzer GetBasicDiagnosticAnalyzer();

        protected static DiagnosticResult GetGlobalResult(string id, string message)
        {
            return new DiagnosticResult
            {
                Id = id,
                Severity = DiagnosticSeverity.Warning,
                Message = message
            };
        }

        protected static DiagnosticResult GetGlobalResult(DiagnosticDescriptor rule, params string[] messageArguments)
        {
            return new DiagnosticResult
            {
                Id = rule.Id,
                Severity = rule.DefaultSeverity,
                Message = rule.MessageFormat.ToString()
            };
        }

        protected static DiagnosticResult GetBasicResultAt(int line, int column, string id, string message)
        {
            return GetResultAt(VisualBasicDefaultFilePath, line, column, id, message);
        }

        protected static DiagnosticResult GetBasicResultAt(string id, string message, params string[] locationStrings)
        {
            return GetResultAt(VisualBasicDefaultFilePath, id, message, locationStrings);
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

        protected static DiagnosticResult GetResultAt(string path, int line, int column, string id, string message)
        {
            var location = new DiagnosticResultLocation(path, line, column);

            return new DiagnosticResult
            {
                Locations = new[] { location },
                Id = id,
                Severity = DiagnosticSeverity.Warning,
                Message = message
            };
        }

        protected static DiagnosticResult GetResultAt(string defaultPath, string id, string message, params string[] locationStrings)
        {
            return new DiagnosticResult
            {
                Locations = ParseResultLocations(defaultPath, locationStrings),
                Id = id,
                Severity = DiagnosticSeverity.Warning,
                Message = message
            };
        }

        protected static DiagnosticResult GetResultAt(string path, int line, int column, DiagnosticDescriptor rule, params object[] messageArguments)
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

        protected static DiagnosticResultLocation[] ParseResultLocations(string defaultPath, string[] locationStrings)
        {
            var builder = new List<DiagnosticResultLocation>();

            foreach (var str in locationStrings)
            {
                var tokens = str.Split('(', ',', ')');
                Assert.True(tokens.Length == 4, "Location string must be of the format 'FileName.cs(line,column)' or just 'line,column' to use " + defaultPath + " as the file name.");

                string path = tokens[0] == "" ? defaultPath : tokens[0];

                int line;
                Assert.True(int.TryParse(tokens[1], out line) && line >= -1, "Line must be >= -1 in location string: " + str);

                int column;
                Assert.True(int.TryParse(tokens[2], out column) && line >= -1, "Column must be >= -1 in location string: " + str);

                builder.Add(new DiagnosticResultLocation(path, line, column));
            }

            return builder.ToArray();
        }

        protected void VerifyCSharp(string source, params DiagnosticResult[] expected)
        {
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), expected);
        }

        protected void VerifyCSharp(string source, bool addLanguageSpecificCodeAnalysisReference, params DiagnosticResult[] expected)
        {
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), addLanguageSpecificCodeAnalysisReference, expected);
        }

        protected void VerifyBasic(string source, params DiagnosticResult[] expected)
        {
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), expected);
        }

        protected void VerifyBasic(string source, bool addLanguageSpecificCodeAnalysisReference, params DiagnosticResult[] expected)
        {
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), addLanguageSpecificCodeAnalysisReference, expected);
        }

        protected void Verify(string source, string language, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }, language, analyzer, expected);
        }

        protected void Verify(string source, string language, DiagnosticAnalyzer analyzer, bool addLanguageSpecificCodeAnalysisReference, params DiagnosticResult[] expected)
        {
            Verify(new[] { source }, language, analyzer, addLanguageSpecificCodeAnalysisReference, expected);
        }

        protected void VerifyBasic(string[] sources, params DiagnosticResult[] expected)
        {
            Verify(sources, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), expected);
        }

        protected void VerifyBasic(string[] sources, bool addLanguageSpecificCodeAnalysisReference, params DiagnosticResult[] expected)
        {
            Verify(sources, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), addLanguageSpecificCodeAnalysisReference, expected);
        }

        protected void VerifyCSharp(string[] sources, params DiagnosticResult[] expected)
        {
            Verify(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), expected);
        }

        protected void VerifyCSharp(string[] sources, bool addLanguageSpecificCodeAnalysisReference, params DiagnosticResult[] expected)
        {
            Verify(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), addLanguageSpecificCodeAnalysisReference, expected);
        }

        protected void Verify(string[] sources, string language, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expected)
        {
            GetSortedDiagnostics(sources, language, analyzer).Verify(analyzer, expected);
        }

        protected void Verify(string[] sources, string language, DiagnosticAnalyzer analyzer, bool addLanguageSpecificCodeAnalysisReference, params DiagnosticResult[] expected)
        {
            GetSortedDiagnostics(sources, language, analyzer, addLanguageSpecificCodeAnalysisReference).Verify(analyzer, expected);
        }

        protected static Diagnostic[] GetSortedDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer, bool addLanguageSpecificCodeAnalysisReference = true)
        {
            var documentsAndUseSpan = GetDocumentsAndSpans(sources, language, addLanguageSpecificCodeAnalysisReference);
            var documents = documentsAndUseSpan.Item1;
            var useSpans = documentsAndUseSpan.Item2;
            var spans = documentsAndUseSpan.Item3;
            return GetSortedDiagnostics(analyzer, documents, useSpans ? spans : null);
        }

        protected static Tuple<Document[], bool, TextSpan?[]> GetDocumentsAndSpans(string[] sources, string language, bool addLanguageSpecificCodeAnalysisReference = true)
        {
            Assert.True(language == LanguageNames.CSharp || language == LanguageNames.VisualBasic, "Unsupported language");

            var spans = new TextSpan?[sources.Length];
            bool useSpans = false;

            for (int i = 0; i < sources.Length; i++)
            {
                string fileName = language == LanguageNames.CSharp ? "Test" + i + ".cs" : "Test" + i + ".vb";

                string source;
                int? pos;
                TextSpan? span;
                MarkupTestFile.GetPositionAndSpan(sources[i], out source, out pos, out span);

                sources[i] = source;
                spans[i] = span;

                if (span != null)
                {
                    useSpans = true;
                }
            }

            var project = CreateProject(sources, language, addLanguageSpecificCodeAnalysisReference);
            var documents = project.Documents.ToArray();
            Assert.Equal(sources.Length, documents.Length);

            return Tuple.Create(documents, useSpans, spans);
        }

        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp, bool addLanguageSpecificCodeAnalysisReference = true)
        {
            return CreateProject(new[] { source }, language, addLanguageSpecificCodeAnalysisReference).Documents.First();
        }

        protected static Project CreateProject(string[] sources, string language = LanguageNames.CSharp, bool addLanguageSpecificCodeAnalysisReference = true, Solution addToSolution = null)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;
            var options = language == LanguageNames.CSharp ? s_CSharpDefaultOptions : s_visualBasicDefaultOptions;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = (addToSolution ?? new AdhocWorkspace().CurrentSolution)
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .AddMetadataReference(projectId, s_corlibReference)
                .AddMetadataReference(projectId, s_systemCoreReference)
                .AddMetadataReference(projectId, s_codeAnalysisReference)
                .AddMetadataReference(projectId, TestBase.SystemRef)
                .AddMetadataReference(projectId, TestBase.SystemRuntimeFacadeRef)
                .AddMetadataReference(projectId, s_immutableCollectionsReference)
                .WithProjectCompilationOptions(projectId, options);

            if (addLanguageSpecificCodeAnalysisReference)
            {
                var symbolsReference = language == LanguageNames.CSharp ? s_CSharpSymbolsReference : s_visualBasicSymbolsReference;
                var project = solution.GetProject(projectId);
                project = project.AddMetadataReference(symbolsReference);
                solution = project.Solution;
            }

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId);
        }

        protected static Diagnostic[] GetSortedDiagnostics(DiagnosticAnalyzer analyzer, Document document, TextSpan?[] spans = null)
        {
            return GetSortedDiagnostics(analyzer, new[] { document }, spans);
        }

        protected static Diagnostic[] GetSortedDiagnostics(DiagnosticAnalyzer analyzer, Document[] documents, TextSpan?[] spans = null)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = DiagnosticBag.GetInstance();
            foreach (var project in projects)
            {
                var compilation = project.GetCompilationAsync().Result;
                compilation = EnableAnalyzer(analyzer, compilation);

                var diags = compilation.GetAnalyzerDiagnostics(new[] { analyzer });

                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                            {
                                var span = spans != null ? spans[i] : null;
                                if (span == null || span.Value.Contains(diag.Location.SourceSpan))
                                {
                                    diagnostics.Add(diag);
                                }
                            }
                        }
                    }
                }
            }

            var results = GetSortedDiagnostics(diagnostics.AsEnumerable());
            diagnostics.Free();
            return results;
        }

        private static Compilation EnableAnalyzer(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            return compilation
                .WithOptions(
                    compilation
                        .Options
                        .WithSpecificDiagnosticOptions(
                            analyzer
                                .SupportedDiagnostics
                                .Select(x =>
                                    KeyValuePair.Create(x.Id, ReportDiagnostic.Default))
                                    .ToImmutableDictionaryOrEmpty()));
        }

        protected static void AnalyzeDocumentCore(DiagnosticAnalyzer analyzer, Document document, Action<Diagnostic> addDiagnostic, TextSpan? span = null, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = true)
        {
            var semanticModel = document.GetSemanticModelAsync().Result;
            var compilation = semanticModel.Compilation;
            compilation = EnableAnalyzer(analyzer, compilation);

            var diagnostics = compilation.GetAnalyzerDiagnostics(new[] { analyzer }, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!span.HasValue ||
                    diagnostic.Location == Location.None ||
                    diagnostic.Location.IsInMetadata ||
                    (diagnostic.Location.SourceTree == semanticModel.SyntaxTree &&
                    span.Value.Contains(diagnostic.Location.SourceSpan)))
                {
                    addDiagnostic(diagnostic);
                }
            }
        }

        protected static Diagnostic[] GetSortedDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }
    }
}
