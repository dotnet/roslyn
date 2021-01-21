// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.HighlightBracesAnalyzer>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AdditionalFilesTests
    {
        private static DiagnosticResult Diagnostic()
            => AnalyzerVerifier<HighlightBracesAnalyzer, CSharpTest, DefaultVerifier>.Diagnostic();

        [Fact]
        public async Task TestDiagnosticInNormalFile()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { Diagnostic().WithLocation(1, 23) },
                    AdditionalFiles =
                    {
                        ("File1.txt", "Content without braces"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticInNormalFileNotDeclared()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace { }" },
                        AdditionalFiles =
                        {
                            ("File1.txt", "Content without braces"),
                        },
                    },
                }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// /0/Test0.cs(1,23): warning Brace: message" + Environment.NewLine +
                "VerifyCS.Diagnostic().WithSpan(1, 23, 1, 24)," + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFile()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]" },
                    ExpectedDiagnostics = { Diagnostic().WithSpan("File1.txt", 1, 14, 1, 15) },
                    AdditionalFiles =
                    {
                        ("File1.txt", "Content with { braces }"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFileWithCombinedSyntax()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]" },
                    ExpectedDiagnostics = { Diagnostic().WithLocation(0) },
                    AdditionalFiles =
                    {
                        ("File1.txt", "Content with {|#0:{|} braces }"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFileWithCombinedSyntaxDuplicate()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources = { "[assembly: System.Reflection.AssemblyVersion{|#0:(|}\"1.0.0.0\")]" },
                        ExpectedDiagnostics = { Diagnostic().WithLocation(0) },
                        AdditionalFiles =
                        {
                            ("File1.txt", "Content with {|#0:{|} braces }"),
                        },
                    },
                }.RunAsync();
            });

            var expected = "Input contains multiple markup locations with key '#0'";
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFileWithCombinedSyntaxMismatch()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources = { "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]" },
                        ExpectedDiagnostics = { Diagnostic().WithLocation(0) },
                        AdditionalFiles =
                        {
                            ("File1.txt", "Content with {|#1:{|} braces }"),
                        },
                    },
                }.RunAsync();
            });

            var expected = "The markup location '#0' was not found in the input.";
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFileNotDeclared()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources = { "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]" },
                        AdditionalFiles =
                        {
                            ("File1.txt", "Content with { braces }"),
                        },
                    },
                }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// File1.txt(1,14): warning Brace: message" + Environment.NewLine +
                "new DiagnosticResult(HighlightBracesAnalyzer.Brace).WithSpan(\"File1.txt\", 1, 14, 1, 15)," + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFileDeclaredWithMarkup()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]" },
                    AdditionalFiles =
                    {
                        ("File1.txt", "Content with {|Brace:{|} braces }"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticInAdditionalFileBraceNotTreatedAsMarkup()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]" },
                    ExpectedDiagnostics = { Diagnostic().WithSpan("File1.txt", 1, 14, 1, 15) },
                    AdditionalFiles =
                    {
                        ("File1.txt", "Content with {|Literal:text|}"),
                    },
                    MarkupHandling = MarkupMode.None,
                },
            }.RunAsync();
        }
    }
}
