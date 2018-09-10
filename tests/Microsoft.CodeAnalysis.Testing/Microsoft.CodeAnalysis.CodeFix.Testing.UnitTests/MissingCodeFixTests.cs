// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Verifier tests for cases where a code fix isn't provided for a diagnostic, or is provided but doesn't make any
    /// changes when applied. Each test uses diagnostics provided via <see cref="SolutionState.ExpectedDiagnostics"/>.
    /// </summary>
    /// <seealso cref="MissingCodeFixMarkupTests"/>
    public class MissingCodeFixTests
    {
        /// <summary>
        /// Verifies that a test will pass if it expects no code fix to be provided.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [WorkItem(219, "https://github.com/dotnet/roslyn-sdk/issues/219")]
        public async Task TestCodeFixNotProvided()
        {
            var testCode = @"
namespace MyNamespace {
}
";
            var expected = new DiagnosticResult(HighlightBraceAnalyzer.Descriptor).WithSpan(2, 23, 2, 24);

            // Test through the helper
            await new CSharpCodeFixTest<HighlightBraceAnalyzer, CodeFixNotOfferedProvider>
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected },
                FixedState = { InheritanceMode = StateInheritanceMode.AutoInheritAll },
            }.RunAsync();

            // Test through the verifier
            await Verify<CodeFixNotOfferedProvider>.VerifyCodeFixAsync(testCode, expected, testCode);
        }

        /// <summary>
        /// Verifies that a test will fail if it expects no code fix to be provided, but a code fix that takes no action
        /// is provided.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [WorkItem(219, "https://github.com/dotnet/roslyn-sdk/issues/219")]
        public async Task TestCodeFixProvidedWhenNotExpected()
        {
            var testCode = @"
namespace MyNamespace {
}
";
            var expected = new DiagnosticResult(HighlightBraceAnalyzer.Descriptor).WithSpan(2, 23, 2, 24);

            // Test through the helper
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpCodeFixTest<HighlightBraceAnalyzer, CodeFixOfferedProvider>
                {
                    TestCode = testCode,
                    ExpectedDiagnostics = { expected },
                    FixedState = { InheritanceMode = StateInheritanceMode.AutoInheritAll },
                }.RunAsync();
            });

            Assert.Equal("Expected '0' iterations but found '1' iterations.", exception.Message);

            // Test through the verifier
            exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Verify<CodeFixOfferedProvider>.VerifyCodeFixAsync(testCode, expected, testCode);
            });

            Assert.Equal("Expected '0' iterations but found '1' iterations.", exception.Message);
        }

        /// <summary>
        /// Verifies that a test will pass if it expects a code fix that takes no action to be provided.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [WorkItem(219, "https://github.com/dotnet/roslyn-sdk/issues/219")]
        public async Task TestCodeFixProvidedButTakesNoAction()
        {
            var testCode = @"
namespace MyNamespace {
}
";
            var expected = new DiagnosticResult(HighlightBraceAnalyzer.Descriptor).WithSpan(2, 23, 2, 24);

            // Test through the helper
            await new CSharpCodeFixTest<HighlightBraceAnalyzer, CodeFixOfferedProvider>
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected },
                FixedState = { InheritanceMode = StateInheritanceMode.AutoInheritAll },
                NumberOfIncrementalIterations = 1,
                NumberOfFixAllIterations = 1,
            }.RunAsync();

            // Test through the verifier
            await Verify<CodeFixNotOfferedProvider>.VerifyCodeFixAsync(testCode, expected, testCode);
        }

        /// <summary>
        /// Verifies that a test will fail if it expects a code fix that takes no action to be provided, but no code fix
        /// is offered at all.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [WorkItem(219, "https://github.com/dotnet/roslyn-sdk/issues/219")]
        public async Task TestCodeFixNotProvidedWhenNoActionFixIsExpected()
        {
            var testCode = @"
namespace MyNamespace {
}
";
            var expected = new DiagnosticResult(HighlightBraceAnalyzer.Descriptor).WithSpan(2, 23, 2, 24);

            // Test through the helper (this scenario cannot be described via the verifier)
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpCodeFixTest<HighlightBraceAnalyzer, CodeFixNotOfferedProvider>
                {
                    TestCode = testCode,
                    ExpectedDiagnostics = { expected },
                    FixedState = { InheritanceMode = StateInheritanceMode.AutoInheritAll },
                    NumberOfIncrementalIterations = 1,
                    NumberOfFixAllIterations = 1,
                }.RunAsync();
            });

            Assert.Equal("Expected '1' iterations but found '0' iterations.", exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxTreeAction(HandleSyntaxTree);
            }

            private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                foreach (var token in context.Tree.GetRoot(context.CancellationToken).DescendantTokens())
                {
                    if (!token.IsKind(SyntaxKind.OpenBraceToken))
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, token.GetLocation()));
                }
            }
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class CodeFixNotOfferedProvider : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds
                => ImmutableArray.Create(HighlightBraceAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider()
                => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
                => Task.CompletedTask;
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class CodeFixOfferedProvider : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds
                => ImmutableArray.Create(HighlightBraceAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider()
                => WellKnownFixAllProviders.BatchFixer;

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "No action taken",
                            ct => Task.FromResult(context.Document),
                            nameof(CodeFixOfferedProvider)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }
        }

        private class Verify<TCodeFix> : CodeFixVerifier<HighlightBraceAnalyzer, TCodeFix, CSharpCodeFixTest<HighlightBraceAnalyzer, TCodeFix>, DefaultVerifier>
            where TCodeFix : CodeFixProvider, new()
        {
        }
    }
}
