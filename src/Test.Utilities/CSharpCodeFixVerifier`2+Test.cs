// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;

namespace Test.Utilities
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            static Test()
            {
                // If we have outdated defaults from the host unit test application targeting an older .NET Framework, use more
                // reasonable TLS protocol version for outgoing connections.
#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
#pragma warning disable CS0618 // Type or member is obsolete
                if (ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols
                {
#pragma warning disable CA5386 // Avoid hardcoding SecurityProtocolType value
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#pragma warning restore CA5386 // Avoid hardcoding SecurityProtocolType value
                }
            }

            internal static readonly ImmutableDictionary<string, ReportDiagnostic> NullableWarnings = GetNullableWarningsFromCompiler();

            public Test()
            {
                ReferenceAssemblies = AdditionalMetadataReferences.Default;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var project = solution.GetProject(projectId)!;
                    var parseOptions = (CSharpParseOptions)project.ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    var compilationOptions = project.CompilationOptions!;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    if (AnalyzerConfigDocument is not null)
                    {
                        solution = solution.AddAnalyzerConfigDocument(
                            DocumentId.CreateNewId(projectId, debugName: ".editorconfig"),
                            ".editorconfig",
                            SourceText.From($"is_global = true" + Environment.NewLine + AnalyzerConfigDocument),
                            filePath: @"z:\.editorconfig");
                    }

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

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp7_3;

            public string? AnalyzerConfigDocument { get; set; }
        }
    }
}
