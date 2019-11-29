// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Test.Utilities.CodeMetrics
{
    public abstract class CodeMetricsTestBase
    {
        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly CompilationOptions s_CSharpDefaultOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        private static readonly CompilationOptions s_visualBasicDefaultOptions = new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string CSharpDefaultFilePath = DefaultFilePathPrefix + 0 + "." + CSharpDefaultFileExt;
        internal static string VisualBasicDefaultFilePath = DefaultFilePathPrefix + 0 + "." + VisualBasicDefaultExt;
        internal static string TestProjectName = "TestProject";

        protected abstract string GetMetricsDataString(Compilation compilation);

        protected Project CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;
            var options = language == LanguageNames.CSharp ? s_CSharpDefaultOptions : s_visualBasicDefaultOptions;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

#pragma warning disable CA2000 // Dispose objects before losing scope - Current solution/project takes the dispose ownership of the created AdhocWorkspace
            var solution = new AdhocWorkspace()
#pragma warning restore CA2000 // Dispose objects before losing scope
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .WithProjectCompilationOptions(projectId, options)
                .AddMetadataReference(projectId, s_corlibReference)
                .AddMetadataReference(projectId, s_systemCoreReference);

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

        protected void VerifyCSharp(string source, string expectedMetricsText, bool expectDiagnostics = false)
            => Verify(new[] { source }, expectedMetricsText, expectDiagnostics, LanguageNames.CSharp);

        protected void VerifyCSharp(string[] sources, string expectedMetricsText, bool expectDiagnostics = false)
            => Verify(sources, expectedMetricsText, expectDiagnostics, LanguageNames.CSharp);

        protected void VerifyBasic(string source, string expectedMetricsText, bool expectDiagnostics = false)
            => Verify(new[] { source }, expectedMetricsText, expectDiagnostics, LanguageNames.VisualBasic);

        private void Verify(string[] sources, string expectedMetricsText, bool expectDiagnostics, string language)
        {
            var project = CreateProject(sources, language);
            var compilation = project.GetCompilationAsync(CancellationToken.None).Result;
            var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning || d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectDiagnostics, diagnostics.Any());

            var actualMetricsText = GetMetricsDataString(compilation).Trim();
            expectedMetricsText = expectedMetricsText.Trim();
            var actualMetricsTextLines = actualMetricsText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var expectedMetricsTextLines = expectedMetricsText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            var success = true;
            if (actualMetricsTextLines.Length != expectedMetricsTextLines.Length)
            {
                success = false;
            }
            else
            {
                for (int i = 0; i < actualMetricsTextLines.Length; i++)
                {
                    var actual = actualMetricsTextLines[i].Trim();
                    var expected = expectedMetricsTextLines[i].Trim();
                    if (actual != expected)
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (!success)
            {
                // Dump the entire expected and actual lines for easy update to baseline.
                Assert.True(false, $"Expected:\r\n{expectedMetricsText}\r\n\r\nActual:\r\n{actualMetricsText}");
            }
        }
    }
}
