// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP1_1 && !NET46

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Xunit;
using CSharpAnalyzerTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpSuppressorTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer,
    Microsoft.CodeAnalysis.Testing.DiagnosticSuppressorTests.HighlightBracesSuppressor>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DiagnosticSuppressorTests
    {
        private static readonly DiagnosticDescriptor DiagnosticDescriptor = new HighlightBracesAnalyzer().Descriptor;

        [Fact]
        public async Task TestUnspecifiedSuppression()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|#0:{|} }" },
                    ExpectedDiagnostics =
                    {
                        new DiagnosticResult(DiagnosticDescriptor).WithLocation(0).WithIsSuppressed(null),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestNormalSuppression()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|#0:{|} }" },
                    ExpectedDiagnostics =
                    {
                        new DiagnosticResult(DiagnosticDescriptor).WithLocation(0).WithIsSuppressed(true),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestUnexpectedSuppressionPresent()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {|#0:{|} }" },
                        ExpectedDiagnostics =
                        {
                            new DiagnosticResult(DiagnosticDescriptor).WithLocation(0).WithIsSuppressed(false),
                        },
                    },
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic suppression state to match

                Expected diagnostic:
                    // /0/Test0.cs(1,23,1,24): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24).WithIsSuppressed(false),

                Actual diagnostic:
                    // /0/Test0.cs(1,23): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24).WithIsSuppressed(true),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestExpectedSuppressionMissing()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {|#0:{|} }" },
                        ExpectedDiagnostics =
                        {
                            new DiagnosticResult(DiagnosticDescriptor).WithLocation(0).WithIsSuppressed(true),
                        },
                    },
                }.RunAsync();
            });

            var expected =
                """
                Expected diagnostic suppression state to match

                Expected diagnostic:
                    // /0/Test0.cs(1,23,1,24): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24).WithIsSuppressed(true),

                Actual diagnostic:
                    // /0/Test0.cs(1,23): warning Brace: message
                VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24),


                """.ReplaceLineEndings();
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal class HighlightBracesSuppressor : DiagnosticSuppressor
        {
            internal static readonly SuppressionDescriptor Descriptor =
                new("XBrace", DiagnosticDescriptor.Id, "justification");

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Descriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    context.ReportSuppression(Suppression.Create(Descriptor, diagnostic));
                }
            }
        }
    }
}

#endif
