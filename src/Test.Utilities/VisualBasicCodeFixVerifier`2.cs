// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Test.Utilities
{
    public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static DiagnosticResult Diagnostic()
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(descriptor);

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
            => VerifyAnalyzerAsync(source, expectedDiagnostics: expected);

        public static Task VerifyAnalyzerAsync(string source, CompilerDiagnostics compilerDiagnostics, params DiagnosticResult[] expected)
            => VerifyAnalyzerAsync(source, expected, compilerDiagnostics);

        public static Task VerifyAnalyzerWithEditorConfigAsync(string source, string editorConfigSource, params DiagnosticResult[] expected)
            => VerifyAnalyzerAsync(source, expected, additionalFiles: new SourceFileCollection { (".editorconfig", SourceText.From(editorConfigSource)) });

        public static async Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expectedDiagnostics,
            CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors, SourceFileCollection additionalFiles = null)
        {
            var test = new Test
            {
                TestCode = source,
                CompilerDiagnostics = compilerDiagnostics
            };

            if (additionalFiles?.Count > 0)
            {
                test.TestState.AdditionalFiles.AddRange(additionalFiles);
            }
            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            await test.RunAsync();
        }

        public static Task VerifyCodeFixAsync(string source, string fixedSource)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
