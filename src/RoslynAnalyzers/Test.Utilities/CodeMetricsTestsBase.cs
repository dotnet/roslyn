// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Test.Utilities.CodeMetrics
{
    public abstract class CodeMetricsTestBase
    {
        private static readonly CompilationOptions s_CSharpDefaultOptions = BuildDefaultCSharpOptions();
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

            var defaultReferences = ReferenceAssemblies.NetFramework.Net48.Default;
            var references = Task.Run(() => defaultReferences.ResolveAsync(language, CancellationToken.None)).GetAwaiter().GetResult();

#pragma warning disable CA2000 // Dispose objects before losing scope - Current solution/project takes the dispose ownership of the created AdhocWorkspace
            var solution = new AdhocWorkspace()
#pragma warning restore CA2000 // Dispose objects before losing scope
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .WithProjectCompilationOptions(projectId, options)
                .AddMetadataReferences(projectId, references);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId)!;
        }

        protected void VerifyCSharp(string source, string expectedMetricsText, bool expectDiagnostics = false)
            => Verify([source], expectedMetricsText, expectDiagnostics, LanguageNames.CSharp);

        protected void VerifyCSharp(string[] sources, string expectedMetricsText, bool expectDiagnostics = false)
            => Verify(sources, expectedMetricsText, expectDiagnostics, LanguageNames.CSharp);

        protected void VerifyBasic(string source, string expectedMetricsText, bool expectDiagnostics = false)
            => Verify([source], expectedMetricsText, expectDiagnostics, LanguageNames.VisualBasic);

        private void Verify(string[] sources, string expectedMetricsText, bool expectDiagnostics, string language)
        {
            var project = CreateProject(sources, language);
            var compilation = project.GetCompilationAsync(CancellationToken.None).Result!;
            var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error);
            if (expectDiagnostics)
            {
                Assert.True(diagnostics.Any());
            }
            else
            {
                Assert.Collection(diagnostics, Array.Empty<Action<Diagnostic>>());
            }

            var actualMetricsText = GetMetricsDataString(compilation).Trim();
            expectedMetricsText = expectedMetricsText.Trim();
            var actualMetricsTextLines = actualMetricsText.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            var expectedMetricsTextLines = expectedMetricsText.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

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

        private static CompilationOptions BuildDefaultCSharpOptions()
        {
            // Between the 3.0.0 and 3.5.0 release of Microsoft.CodeAnalysis the
            // NullableContextOptions type changed namespaces, making the bound constructor from 3.0.0
            // not resolve in 3.5.0.
            //
            // This moves the compile-time decision to runtime to work around that limitation.

            foreach (var ctor in typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions).GetConstructors())
            {
                var parameterInfos = ctor.GetParameters();

                if (parameterInfos.Length < 1 || typeof(OutputKind) != parameterInfos[0].ParameterType)
                {
                    continue;
                }

                if (parameterInfos.Length > 1 && !parameterInfos[1].HasDefaultValue)
                {
                    continue;
                }

                object[] parameters = new object[parameterInfos.Length];
                parameters.AsSpan().Fill(Type.Missing);
                parameters[0] = OutputKind.DynamicallyLinkedLibrary;

                return (Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions)ctor.Invoke(
                    BindingFlags.OptionalParamBinding | BindingFlags.CreateInstance,
                    null,
                    parameters,
                    CultureInfo.InvariantCulture);
            }

            throw new Exception("Could not find a compatible CSharpCompilationOptions constructor via reflection.");
        }
    }
}
