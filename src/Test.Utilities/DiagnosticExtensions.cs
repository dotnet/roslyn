// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using Xunit;

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

            AnalyzerDriver driver = AnalyzerDriver.CreateAndAttachToCompilation(c, analyzersArray, options, new AnalyzerManager(analyzersArray), onAnalyzerException, null, false, out Compilation newCompilation, CancellationToken.None);

            ImmutableArray<Diagnostic> diagnostics = newCompilation.GetDiagnostics();
            if (validationMode != TestValidationMode.AllowCompileErrors)
            {
                ValidateNoCompileErrors(diagnostics);
            }

            return driver.GetDiagnosticsAsync(newCompilation).Result.AddRange(exceptionDiagnostics);
        }

        private static void ValidateNoCompileErrors(ImmutableArray<Diagnostic> compilerDiagnostics)
        {
            var compileErrors = compilerDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

            if (compileErrors.Any())
            {
                var builder = new StringBuilder();
                builder.Append($"Test contains compilation error(s). Pass {nameof(TestValidationMode)}.{nameof(TestValidationMode.AllowCompileErrors)} if these are intended:");
                builder.Append(string.Concat(compileErrors.Select(x => "\n" + x.ToString())));

                string message = builder.ToString();
                Assert.True(false, message);
            }
        }
    }
}
