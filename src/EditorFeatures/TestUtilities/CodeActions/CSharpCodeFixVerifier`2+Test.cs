// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            /// <summary>
            /// By default, the compiler reports diagnostics for nullable reference types at
            /// <see cref="DiagnosticSeverity.Warning"/>, and the analyzer test framework defaults to only validating
            /// diagnostics at <see cref="DiagnosticSeverity.Error"/>. This map contains all compiler diagnostic IDs
            /// related to nullability mapped to <see cref="ReportDiagnostic.Error"/>, which is then used to enable all
            /// of these warnings for default validation during analyzer and code fix tests.
            /// </summary>
            private static readonly ImmutableDictionary<string, ReportDiagnostic> s_nullableWarnings = GetNullableWarningsFromCompiler();

            public Test()
            {
                MarkupOptions = Testing.MarkupOptions.UseFirstDescriptor;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (CSharpParseOptions)solution.GetRequiredProject(projectId).ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    var compilationOptions = solution.GetRequiredProject(projectId).CompilationOptions!;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(s_nullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

#if !CODE_STYLE // TODO: Add support for Options based tests in CodeStyle layer
                    var options = solution.Options;
                    foreach (var (key, value) in Options)
                    {
                        options = options.WithChangedOption(key, value);
                    }

                    solution = solution.WithOptions(options);
#endif

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

            /// <summary>
            /// Gets or sets the language version to use for the test. The default value is
            /// <see cref="LanguageVersion.CSharp8"/>.
            /// </summary>
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp8;

#if !CODE_STYLE // TODO: Add support for Options based tests in CodeStyle layer
            /// <summary>
            /// Gets a collection of options to apply to <see cref="Solution.Options"/> for testing. Values may be added
            /// using a collection initializer.
            /// </summary>
            public OptionsCollection Options { get; } = new OptionsCollection(LanguageNames.CSharp);
#endif

            public Func<ImmutableArray<Diagnostic>, Diagnostic?>? DiagnosticSelector { get; set; }

#if !CODE_STYLE
            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
            {
                return new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), project.Solution);
            }
#endif

            protected override Diagnostic? TrySelectDiagnosticToFix(ImmutableArray<Diagnostic> fixableDiagnostics)
            {
                return DiagnosticSelector?.Invoke(fixableDiagnostics)
                    ?? base.TrySelectDiagnosticToFix(fixableDiagnostics);
            }
        }
    }
}
