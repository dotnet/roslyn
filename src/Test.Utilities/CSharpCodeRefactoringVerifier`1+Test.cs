// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Test.Utilities
{
    public static partial class CSharpCodeRefactoringVerifier<TRefactoring>
        where TRefactoring : CodeRefactoringProvider, new()
    {
        public class Test : CSharpCodeRefactoringTest<TRefactoring, XUnitVerifier>
        {
            public Test()
            {
                ReferenceAssemblies = AdditionalMetadataReferences.Default;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });
            }

            private static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings
                => CSharpCodeFixVerifier<EmptyDiagnosticAnalyzer, EmptyCodeFixProvider>.Test.NullableWarnings;

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp7_3;
        }
    }
}
