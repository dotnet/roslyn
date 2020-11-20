// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Test.Utilities
{
    public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test()
            {
                ReferenceAssemblies = AdditionalMetadataReferences.Default;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var project = solution.GetProject(projectId)!;
                    var parseOptions = (VisualBasicParseOptions)project.ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    if (AnalyzerConfigDocument is not null)
                    {
                        solution = solution.AddAnalyzerConfigDocument(
                            DocumentId.CreateNewId(projectId, debugName: ".editorconfig"),
                            ".editorconfig",
                            SourceText.From($"is_global = true" + Environment.NewLine + AnalyzerConfigDocument),
                            filePath: @"/.editorconfig");
                    }

                    return solution;
                });
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.VisualBasic15_5;

            public string? AnalyzerConfigDocument { get; set; }
        }
    }
}
