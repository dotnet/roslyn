// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Test.Utilities
{
    public static class DiagnosticExtensions
    {
        public static readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> FailFastOnAnalyzerException = (e, a, d) => FailFast.OnFatalException(e);

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            TestValidationMode validationMode,
            AnalyzerOptions options = null,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null,
            bool logAnalyzerExceptionAsDiagnostics = true)
            where TCompilation : Compilation
        {
            ImmutableArray<DiagnosticAnalyzer> analyzersArray = analyzers.ToImmutableArray();

            var exceptionDiagnostics = new ConcurrentSet<Diagnostic>();

            if (onAnalyzerException == null)
            {
                if (logAnalyzerExceptionAsDiagnostics)
                {
                    onAnalyzerException = (ex, analyzer, diagnostic) =>
                    {
                        exceptionDiagnostics.Add(diagnostic);
                    };
                }
                else
                {
                    // We want unit tests to throw if any analyzer OR the driver throws, unless the test explicitly provides a delegate.
                    onAnalyzerException = FailFastOnAnalyzerException;
                }
            }

            using (var driver = AnalyzerDriver.CreateAndAttachToCompilation(c, analyzersArray, options, new AnalyzerManager(analyzersArray), onAnalyzerException, null, false, out Compilation newCompilation, CancellationToken.None))
            {
                ImmutableArray<Diagnostic> diagnostics = newCompilation.GetDiagnostics();
                if (validationMode != TestValidationMode.AllowCompileErrors)
                {
                    CompilationUtils.ValidateNoCompileErrors(diagnostics);
                }

                return driver.GetDiagnosticsAsync(newCompilation).Result.AddRange(exceptionDiagnostics);
            }
        }
    }
}
