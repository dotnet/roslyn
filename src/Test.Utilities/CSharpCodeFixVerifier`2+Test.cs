// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Test.Utilities
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.Netstandard);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemXmlReference);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemRuntimeFacadeRef);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemThreadingFacadeRef);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemThreadingTaskFacadeRef);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.WorkspacesReference);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemDiagnosticsDebugReference);

                    if (IncludeCodeAnalysisReference)
                    {
                        solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.CSharpSymbolsReference);
                    }

                    if (!IncludeImmutableCollectionsReference)
                    {
                        solution = solution.RemoveMetadataReference(projectId, MetadataReferences.SystemCollectionsImmutableReference);
                    }

                    if (IncludeSystemDataReference)
                    {
                        solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemDataReference)
                            .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemXmlDataReference);
                    }

                    var parseOptions = (CSharpParseOptions)solution.GetProject(projectId).ParseOptions;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    return solution;
                });
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp7_3;

            public bool IncludeCodeAnalysisReference { get; set; } = true;

            public bool IncludeImmutableCollectionsReference { get; set; } = true;

            public bool IncludeSystemDataReference { get; set; } = true;
        }
    }
}
