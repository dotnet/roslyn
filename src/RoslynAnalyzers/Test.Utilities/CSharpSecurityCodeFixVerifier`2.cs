// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Test.Utilities
{
    public static partial class CSharpSecurityCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static DiagnosticResult Diagnostic()
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

        public static async Task VerifyAnalyzerAsync([StringSyntax("C#-test")] string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            await RunTestAsync(test, expected);
        }

        public static Task VerifyCodeFixAsync([StringSyntax("C#-test")] string source, [StringSyntax("C#-test")] string fixedSource)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        public static Task VerifyCodeFixAsync([StringSyntax("C#-test")] string source, DiagnosticResult expected, [StringSyntax("C#-test")] string fixedSource)
            => VerifyCodeFixAsync(source, [expected], fixedSource);

        public static async Task VerifyCodeFixAsync([StringSyntax("C#-test")] string source, DiagnosticResult[] expected, [StringSyntax("C#-test")] string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            await RunTestAsync(test, expected);
        }

        public static async Task RunTestAsync(Test test, params DiagnosticResult[] expected)
        {
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
