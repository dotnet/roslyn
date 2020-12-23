// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DiagnosticLocationTests
    {
        private static DiagnosticResult Diagnostic<TAnalyzer>()
            where TAnalyzer : DiagnosticAnalyzer, new()
            => AnalyzerVerifier<TAnalyzer, CSharpAnalyzerTest<TAnalyzer>, DefaultVerifier>.Diagnostic();

        [Fact]
        public async Task TestDiagnosticMatchesCorrectSpan()
        {
            await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
            {
                TestCode = @"class TestClass [|{|] }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticMatchesCorrectLocation()
        {
            await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
            {
                TestCode = @"class TestClass $${ }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticWithoutLocation()
        {
            await new CSharpAnalyzerTest<ReportCompilationDiagnosticAnalyzer>
            {
                TestCode = @"class TestClass { }",
                ExpectedDiagnostics = { new DiagnosticResult(ReportCompilationDiagnosticAnalyzer.Descriptor) },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticExplicitWithoutLocation()
        {
            await new CSharpAnalyzerTest<ReportCompilationDiagnosticAnalyzer>
            {
                TestCode = @"class TestClass { }",
                ExpectedDiagnostics = { new DiagnosticResult(ReportCompilationDiagnosticAnalyzer.Descriptor).WithNoLocation() },
            }.RunAsync();
        }

        [Fact]
        [WorkItem(207, "https://github.com/dotnet/roslyn-sdk/issues/207")]
        public async Task TestDiagnosticDoesNotMatchIncorrectSpan()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass [||]{ }",
                }.RunAsync();
            });

            var expected =
                "Expected diagnostic to end at column \"17\" was actually at column \"18\"" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17,1,17): warning Brace" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 17)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticDoesNotMatchIncorrectLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass {$$ }",
                }.RunAsync();
            });

            var expected =
                "Expected diagnostic to start at column \"18\" was actually at column \"17\"" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,18): warning Brace" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithLocation(1, 18)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticDoesNotMatchMissingLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass { }",
                    ExpectedDiagnostics = { Diagnostic<HighlightBraceSpanAnalyzer>() },
                }.RunAsync();
            });

            var expected =
                "Expected a project diagnostic with no location:" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // warning Brace: message" + Environment.NewLine +
                "new DiagnosticResult(HighlightBraceSpanAnalyzer.Brace)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticDoesNotMatchNoLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBraceSpanAnalyzer>
                {
                    TestCode = @"class TestClass { }",
                    ExpectedDiagnostics = { Diagnostic<HighlightBraceSpanAnalyzer>().WithNoLocation() },
                }.RunAsync();
            });

            var expected =
                "Expected a project diagnostic with no location:" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // warning Brace: message" + Environment.NewLine +
                "new DiagnosticResult(HighlightBraceSpanAnalyzer.Brace)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticMatchesCorrectSpan()
        {
            await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
            {
                TestCode = @"class TestClass [||]{ }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticMatchesCorrectLocation()
        {
            await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
            {
                TestCode = @"class TestClass $${ }",
            }.RunAsync();
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticDoesNotMatchIncorrectSpan()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
                {
                    TestCode = @"class TestClass [|{|] }",
                }.RunAsync();
            });

            var expected =
                "Expected diagnostic to end at column \"18\" was actually at column \"17\"" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17,1,18): warning Brace" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 18)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 17)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestZeroWidthDiagnosticDoesNotMatchIncorrectLocation()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerTest<HighlightBracePositionAnalyzer>
                {
                    TestCode = @"class TestClass {$$ }",
                }.RunAsync();
            });

            var expected =
                "Expected diagnostic to start at column \"18\" was actually at column \"17\"" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,18): warning Brace" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithLocation(1, 18)," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 17, 1, 17)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracePositionAnalyzer : AbstractHighlightBracesAnalyzer
        {
            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                var location = token.GetLocation();
                return CodeAnalysis.Diagnostic.Create(Descriptor, Location.Create(location.SourceTree!, new TextSpan(location.SourceSpan.Start, 0)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanAnalyzer : AbstractHighlightBracesAnalyzer
        {
            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                return CodeAnalysis.Diagnostic.Create(Descriptor, token.GetLocation());
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class ReportCompilationDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterCompilationAction(HandleCompilation);
            }

            private void HandleCompilation(CompilationAnalysisContext context)
            {
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor, location: null));
            }
        }
    }
}
