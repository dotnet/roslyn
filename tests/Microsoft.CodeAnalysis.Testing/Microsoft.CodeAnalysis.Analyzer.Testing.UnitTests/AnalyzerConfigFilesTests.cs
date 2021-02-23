// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP1_1 && !NET46

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.AnalyzerConfigFilesTests.HighlightBracesIfAnalyzerConfigMissingAnalyzer>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AnalyzerConfigFilesTests
    {
        private const string RootEditorConfig = @"
root = true

[*]
key = value
";

        [Fact]
        public async Task TestDiagnosticInNormalFile()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesIfAnalyzerConfigMissingAnalyzer.Descriptor).WithLocation(1, 23) },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
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
                        AnalyzerConfigFiles =
                        {
                            ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
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
        public async Task TestDiagnosticInAnalyzerConfigFileWithCombinedSyntaxDuplicate()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources = { "[assembly: System.Reflection.AssemblyVersion{|#0:(|}\"1.0.0.0\")]" },
                        ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesIfAnalyzerConfigMissingAnalyzer.Descriptor).WithLocation(0) },
                        AnalyzerConfigFiles =
                        {
                            ("/.editorconfig", "# Content with {|#0:{|} braces }"),
                        },
                    },
                }.RunAsync();
            });

            var expected = "Input contains multiple markup locations with key '#0'";
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticInAnalyzerConfigFileBraceNotTreatedAsMarkup()
        {
            var editorConfig = @"
root = true

[*]
key = {|Literal:value|}
";

            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesIfAnalyzerConfigMissingAnalyzer.Descriptor).WithLocation(1, 23) },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(editorConfig, Encoding.UTF8)),
                    },
                    MarkupHandling = MarkupMode.None,
                },
            }.RunAsync();
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        internal class HighlightBracesIfAnalyzerConfigMissingAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxTreeAction(HandleSyntaxTree);
            }

            private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                if (!context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree).TryGetValue("key", out _))
                {
                    return;
                }

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
    }
}

#endif
