// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
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
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.VisualBasic15_5;

            protected override ParseOptions CreateParseOptions()
            {
                return ((VisualBasicParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }

            protected override ImmutableArray<(Project project, Diagnostic diagnostic)> SortDistinctDiagnostics(IEnumerable<(Project project, Diagnostic diagnostic)> diagnostics)
            {
                var baseResult = base.SortDistinctDiagnostics(diagnostics);
                if (typeof(DiagnosticSuppressor).IsAssignableFrom(typeof(TAnalyzer)))
                {
                    // Include suppressed diagnostics when testing diagnostic suppressors
                    return baseResult;
                }

                // Treat suppressed diagnostics as non-existent. Normally this wouldn't be necessary, but some of the
                // tests include diagnostics reported in code wrapped in '#Disable Warning'.
                return baseResult.WhereAsArray(diagnostic => !diagnostic.diagnostic.IsSuppressed);
            }
        }
    }
}
