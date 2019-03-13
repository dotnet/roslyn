// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AnalyzerVerifier<TAnalyzer, TTest, TVerifier>
           where TAnalyzer : DiagnosticAnalyzer, new()
           where TTest : AnalyzerTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
        public static DiagnosticResult Diagnostic()
        {
            var analyzer = new TAnalyzer();
            try
            {
                return Diagnostic(analyzer.SupportedDiagnostics.Single());
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"'{nameof(Diagnostic)}()' can only be used when the analyzer has a single supported diagnostic. Use the '{nameof(Diagnostic)}(DiagnosticDescriptor)' overload to specify the descriptor from which to create the expected result.",
                    ex);
            }
        }

        public static DiagnosticResult Diagnostic(string diagnosticId)
        {
            var analyzer = new TAnalyzer();
            try
            {
                return Diagnostic(analyzer.SupportedDiagnostics.Single(i => i.Id == diagnosticId));
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"'{nameof(Diagnostic)}(string)' can only be used when the analyzer has a single supported diagnostic with the specified ID. Use the '{nameof(Diagnostic)}(DiagnosticDescriptor)' overload to specify the descriptor from which to create the expected result.",
                    ex);
            }
        }

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => new DiagnosticResult(descriptor);

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new TTest
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(CancellationToken.None);
        }
    }
}
