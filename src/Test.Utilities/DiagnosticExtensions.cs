// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Test.Utilities
{
    public static class DiagnosticExtensions
    {
        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            TestValidationMode validationMode,
            AnalyzerOptions options = null)
            where TCompilation : Compilation
        {
            var compilationWithAnalyzers = c.WithAnalyzers(analyzers.ToImmutableArray(), options, CancellationToken.None);
            var diagnostics = c.GetDiagnostics();
            if (validationMode != TestValidationMode.AllowCompileErrors)
            {
                CompilationUtils.ValidateNoCompileErrors(diagnostics);
            }

            var analyzerDiagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
            var allDiagnostics = compilationWithAnalyzers.GetAllDiagnosticsAsync().Result;
            var failureDiagnostics = allDiagnostics.Where(diagnostic => diagnostic.Id == "AD0001");
            var resultDiagnostics = analyzerDiagnostics.Concat(failureDiagnostics);
            return resultDiagnostics.ToImmutableArray();
        }
    }
}
