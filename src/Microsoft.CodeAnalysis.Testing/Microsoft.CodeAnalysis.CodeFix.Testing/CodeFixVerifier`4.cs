// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    public class CodeFixVerifier<TAnalyzer, TCodeFix, TTest, TVerifier>
           where TAnalyzer : DiagnosticAnalyzer, new()
           where TCodeFix : CodeFixProvider, new()
           where TTest : CodeFixTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
        public static DiagnosticResult Diagnostic()
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.Diagnostic(descriptor);

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
            => AnalyzerVerifier<TAnalyzer, TTest, TVerifier>.VerifyAnalyzerAsync(source, expected);

        public static Task VerifyCodeFixAsync(string source, string fixedSource)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new TTest
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(CancellationToken.None);
        }
    }
}
