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
using CSharpCompilerTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpSuppressorTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.DiagnosticSuppressorTests.NonNullableFieldSuppressor>;
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
        [WorkItem(1090, "https://github.com/dotnet/roslyn-sdk/issues/1090")]
        public async Task TestSuppressionOfCompilerDiagnostic()
        {
            await new CSharpCompilerTest
            {
                CompilerDiagnostics = CompilerDiagnostics.Warnings,
                TestState =
                {
                    Sources =
                    {
                        @"#nullable enable
class Sample { string {|#0:_value|}; }",
                    },
                    ExpectedDiagnostics =
                    {
                        DiagnosticResult.CompilerWarning("CS8618").WithLocation(0).WithIsSuppressed(true),
                        DiagnosticResult.CompilerWarning("CS0169").WithLocation(0),
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
                "Expected diagnostic suppression state to match" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,23,1,24): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24).WithIsSuppressed(false)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,23): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24).WithIsSuppressed(true)," + Environment.NewLine +
                Environment.NewLine;
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
                "Expected diagnostic suppression state to match" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,23,1,24): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24).WithIsSuppressed(true)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,23): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal class HighlightBracesSuppressor : DiagnosticSuppressor
        {
            internal static readonly SuppressionDescriptor Descriptor =
                new SuppressionDescriptor("XBrace", DiagnosticDescriptor.Id, "justification");

            public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Descriptor);

            public override void ReportSuppressions(SuppressionAnalysisContext context)
            {
                foreach (var diagnostic in context.ReportedDiagnostics)
                {
                    context.ReportSuppression(Suppression.Create(Descriptor, diagnostic));
                }
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal class NonNullableFieldSuppressor : DiagnosticSuppressor
        {
            internal static readonly SuppressionDescriptor Descriptor =
                new SuppressionDescriptor("FieldIsAssigned", "CS8618", "justification");

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
