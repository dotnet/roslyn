// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AdditionalFilesTests
    {
        [Fact]
        public async Task TestDiagnosticInNormalFile()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23) },
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
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithSpan("File1.txt", 1, 14, 1, 15) },
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
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(0) },
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
                        ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(0) },
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
                        ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(0) },
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
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithSpan("File1.txt", 1, 14, 1, 15) },
                    AdditionalFiles =
                    {
                        ("File1.txt", "Content with {|Literal:text|}"),
                    },
                    MarkupHandling = MarkupMode.None,
                },
            }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracesAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxTreeAction(HandleSyntaxTree);
                context.RegisterCompilationAction(HandleCompilation);
            }

            private void HandleCompilation(CompilationAnalysisContext context)
            {
                foreach (var file in context.Options.AdditionalFiles)
                {
                    var sourceText = file.GetText(context.CancellationToken);
                    var text = sourceText.ToString();
                    for (var i = text.IndexOf('{'); i >= 0; i = text.IndexOf('{', i + 1))
                    {
                        var textSpan = new TextSpan(i, 1);
                        var lineSpan = sourceText.Lines.GetLinePositionSpan(textSpan);
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.Create(file.Path, textSpan, lineSpan)));
                    }
                }
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

        private class CSharpTest : AnalyzerTest<DefaultVerifier>
        {
            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override ParseOptions CreateParseOptions()
                => new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new HighlightBracesAnalyzer();
            }
        }
    }
}
