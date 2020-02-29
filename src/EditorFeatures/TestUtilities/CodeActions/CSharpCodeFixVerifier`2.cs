// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic();

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(descriptor);

        /// <summary>
        /// Verify standard properties of <typeparamref name="TAnalyzer"/>.
        /// </summary>
        /// <remarks>
        /// This validation method is largely specific to dotnet/roslyn scenarios.
        /// </remarks>
        /// <param name="verifyHelpLink"><see langword="true"/> to verify <see cref="DiagnosticDescriptor.HelpLinkUri"/>
        /// property of supported diagnostics; otherwise, <see langword="false"/> to skip this validation.</param>
        public static void VerifyStandardProperties(bool verifyHelpLink = false)
            => CodeFixVerifierHelper.VerifyStandardProperties(new TAnalyzer(), verifyHelpLink);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
        public static async Task VerifyCodeFixAsync(string source, string fixedSource)
            => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
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

        public static async Task VerifyCodeFixAsync((string first, string second) sources, (string first, string second) fixedSources, int numberOfFixAllIterations)
            => await VerifyCodeFixAsync(sources, fixedSources, DiagnosticResult.EmptyDiagnosticResults, numberOfFixAllIterations);

        public static async Task VerifyCodeFixAsync((string first, string second) sources, (string first, string second) fixedSources, DiagnosticResult[] expected, int numberOfFixAllIterations)
        {
            var test = new Test
            {
                TestState = { Sources = { sources.first, sources.second } },
                FixedState = { Sources = { fixedSources.first, fixedSources.second } },
                NumberOfFixAllInDocumentIterations = numberOfFixAllIterations
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        /// <summary>
        /// Verifies fix for a single diagnostic and iterative batch fix for all diagnostics.
        /// </summary>
        /// <param name="source">Original source with multiple diagnostics.</param>
        /// <param name="fixedSource">Fixed source with code fix applied for a single diagnostic selected by <paramref name="diagnosticSelector"/>.</param>
        /// <param name="batchFixedSource">Fixed source for iterative batch fix for all diagnostics.</param>
        /// <param name="diagnosticSelector">Delegate to select a single diagnostic to apply fix to verify <paramref name="fixedSource"/>.</param>
        public static async Task VerifyFixOneAndFixBatchAsync(
            string source,
            string fixedSource,
            string batchFixedSource,
            Func<ImmutableArray<Diagnostic>, Diagnostic?> diagnosticSelector)
        {
            await new Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                BatchFixedCode = batchFixedSource,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                DiagnosticSelector = diagnosticSelector,
            }.RunAsync();
        }
    }
}
