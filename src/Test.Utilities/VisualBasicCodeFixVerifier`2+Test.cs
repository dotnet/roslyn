// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
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
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.Netstandard);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemXmlReference);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemRuntimeFacadeRef);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.WorkspacesReference);
                    solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemDiagnosticsDebugReference);

                    if (IncludeCodeAnalysisReference)
                    {
                        solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.VisualBasicSymbolsReference);
                    }

                    if (IncludeSystemDataReference)
                    {
                        solution = solution.AddMetadataReference(projectId, AdditionalMetadataReferences.SystemDataReference)
                            .AddMetadataReference(projectId, AdditionalMetadataReferences.SystemXmlDataReference);
                    }

                    var parseOptions = (VisualBasicParseOptions)solution.GetProject(projectId).ParseOptions;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    return solution;
                });
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.VisualBasic15_5;

            public bool IncludeCodeAnalysisReference { get; set; } = true;

            public bool IncludeSystemDataReference { get; set; } = true;
        }
    }
}
