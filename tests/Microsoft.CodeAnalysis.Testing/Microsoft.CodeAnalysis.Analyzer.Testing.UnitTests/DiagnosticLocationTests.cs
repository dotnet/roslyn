// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DiagnosticLocationTests
    {
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

        [Fact(Skip = "https://github.com/dotnet/roslyn-sdk/issues/207")]
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
                "Diagnostic:" + Environment.NewLine +
                "    // Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "GetCSharpResultAt(1, 17, HighlightBraceSpanAnalyzer.Brace)" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
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
                "Diagnostic:" + Environment.NewLine +
                "    // Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "GetCSharpResultAt(1, 17, HighlightBraceSpanAnalyzer.Brace)" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
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
                "Diagnostic:" + Environment.NewLine +
                "    // Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "GetCSharpResultAt(1, 17, HighlightBracePositionAnalyzer.Brace)" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
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
                "Diagnostic:" + Environment.NewLine +
                "    // Test0.cs(1,17): warning Brace: message" + Environment.NewLine +
                "GetCSharpResultAt(1, 17, HighlightBracePositionAnalyzer.Brace)" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        private abstract class HighlightBraceAnalyzer : DiagnosticAnalyzer
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

                    context.ReportDiagnostic(CreateDiagnostic(token));
                }
            }

            protected abstract Diagnostic CreateDiagnostic(SyntaxToken token);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracePositionAnalyzer : HighlightBraceAnalyzer
        {
            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                var location = token.GetLocation();
                return Diagnostic.Create(Descriptor, Location.Create(location.SourceTree, new TextSpan(location.SourceSpan.Start, 0)));
            }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBraceSpanAnalyzer : HighlightBraceAnalyzer
        {
            protected override Diagnostic CreateDiagnostic(SyntaxToken token)
            {
                return Diagnostic.Create(Descriptor, token.GetLocation());
            }
        }
    }
}
