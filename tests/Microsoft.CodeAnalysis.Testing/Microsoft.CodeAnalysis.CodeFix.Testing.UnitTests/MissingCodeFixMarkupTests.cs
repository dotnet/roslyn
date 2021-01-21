// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Verifier tests for cases where a code fix isn't provided for a diagnostic, or is provided but doesn't make any
    /// changes when applied. Each test uses diagnostics provided via markup syntax.
    /// </summary>
    /// <seealso cref="MissingCodeFixTests"/>
    public class MissingCodeFixMarkupTests
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
namespace MyNamespace {|Brace:{|}
}
";

            // Test through the helper
            await new CSharpCodeFixTest<HighlightBracesAnalyzer, CodeFixNotOfferedProvider>
            {
                TestCode = testCode,
                FixedState = { MarkupHandling = MarkupMode.Allow },
            }.RunAsync();

            // Test through the verifier
            await Verify<CodeFixNotOfferedProvider>.VerifyCodeFixAsync(testCode, testCode);
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
namespace MyNamespace {|Brace:{|}
}
";

            // Test through the helper
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpCodeFixTest<HighlightBracesAnalyzer, CodeFixOfferedProvider>
                {
                    TestCode = testCode,
                    FixedState = { MarkupHandling = MarkupMode.Allow },
                }.RunAsync();
            });

            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}Expected '0' iterations but found '1' iterations.", exception.Message);

            // Test through the verifier
            exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Verify<CodeFixOfferedProvider>.VerifyCodeFixAsync(testCode, testCode);
            });

            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}Expected '0' iterations but found '1' iterations.", exception.Message);
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
namespace MyNamespace {|Brace:{|}
}
";

            // Test through the helper
            await new CSharpCodeFixTest<HighlightBracesAnalyzer, CodeFixOfferedProvider>
            {
                TestCode = testCode,
                FixedState = { MarkupHandling = MarkupMode.Allow },
                NumberOfIncrementalIterations = 1,
                NumberOfFixAllIterations = 1,
            }.RunAsync();

            // Test through the verifier
            await Verify<CodeFixNotOfferedProvider>.VerifyCodeFixAsync(testCode, testCode);
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
namespace MyNamespace {|Brace:{|}
}
";

            // Test through the helper (this scenario cannot be described via the verifier)
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpCodeFixTest<HighlightBracesAnalyzer, CodeFixNotOfferedProvider>
                {
                    TestCode = testCode,
                    FixedState = { MarkupHandling = MarkupMode.Allow },
                    NumberOfIncrementalIterations = 1,
                    NumberOfFixAllIterations = 1,
                }.RunAsync();
            });

            Assert.Equal($"Context: Iterative code fix application{Environment.NewLine}Expected '1' iterations but found '0' iterations.", exception.Message);
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class CodeFixNotOfferedProvider : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds
                => ImmutableArray.Create(new HighlightBracesAnalyzer().Descriptor.Id);

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
                => ImmutableArray.Create(new HighlightBracesAnalyzer().Descriptor.Id);

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

        private class Verify<TCodeFix> : CodeFixVerifier<HighlightBracesAnalyzer, TCodeFix, CSharpCodeFixTest<HighlightBracesAnalyzer, TCodeFix>, DefaultVerifier>
            where TCodeFix : CodeFixProvider, new()
        {
        }
    }
}
