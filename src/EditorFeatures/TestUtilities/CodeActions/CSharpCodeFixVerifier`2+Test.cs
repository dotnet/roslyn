// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            private static readonly ImmutableDictionary<string, ReportDiagnostic> s_nullableWarnings = GetNullableWarningsFromCompiler();

            public Test()
            {
                MarkupOptions = Testing.MarkupOptions.UseFirstDescriptor;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (CSharpParseOptions)solution.GetProject(projectId).ParseOptions;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(s_nullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    var options = solution.Options;
                    foreach (var (key, value) in Options)
                    {
                        options = options.WithChangedOption(key, value);
                    }

                    solution = solution.WithOptions(options);

                    return solution;
                });
            }

            private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
            {
                string[] args = { "/warnaserror:nullable" };
                var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
                var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

                // Workaround for https://github.com/dotnet/roslyn/issues/41610
                nullableWarnings = nullableWarnings
                    .SetItem("CS8632", ReportDiagnostic.Error)
                    .SetItem("CS8669", ReportDiagnostic.Error);

                return nullableWarnings;
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp8;

            public OptionsCollection Options { get; } = new OptionsCollection(LanguageNames.CSharp);

            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
            {
                return new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), project.Solution);
            }
        }
    }
}
