// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP1_1 && !NET46

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AnalyzerConfigFilesFixTests
    {
        private const string RootEditorConfig = @"
root = true

[*]
key = value
";

        [Fact]
        public async Task TestDiagnosticFixedByAddingAnalyzerConfigFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileExists)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23) },
                },
                FixedState =
                {
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByAddingAnalyzerConfigFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileExists)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|Brace:{|} }" },
                },
                FixedState =
                {
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByAddingAnalyzerConfigFileFailsIfTextIncorrect()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileExists)
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {|Brace:{|} }" },
                    },
                    FixedState =
                    {
                        AnalyzerConfigFiles =
                        {
                            ("/.editorconfig", SourceText.From(RootEditorConfig + "# Wrong line", Encoding.UTF8)),
                        },
                    },
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}content of '/.editorconfig' did not match. Diff shown with expected as baseline:{Environment.NewLine} {Environment.NewLine} root = true{Environment.NewLine} {Environment.NewLine} [*]{Environment.NewLine} key = value{Environment.NewLine}-# Wrong line{Environment.NewLine}+{Environment.NewLine}", exception.Message);
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByAddingAnalyzerConfigFileFailsIfEncodingIncorrect()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileExists)
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {|Brace:{|} }" },
                    },
                    FixedState =
                    {
                        AnalyzerConfigFiles =
                        {
                            ("/.editorconfig", RootEditorConfig),
                        },
                    },
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}encoding of '/.editorconfig' was expected to be '' but was 'utf-8'", exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticFixedByRemovingAnalyzerConfigFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileMissing)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23) },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
                FixedState =
                {
                    InheritanceMode = StateInheritanceMode.Explicit,
                    Sources = { "namespace MyNamespace { }" },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestDiagnosticFixedByRemovingAnalyzerConfigFileWithUndeclaredCompileError()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileMissing)
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {" },
                        ExpectedDiagnostics =
                        {
                            new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23),
                            DiagnosticResult.CompilerError("CS1513").WithLocation(1, 24),
                        },
                        AnalyzerConfigFiles =
                        {
                            ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                        },
                    },
                    FixedState =
                    {
                        // When Explicit mode is used, compile errors in the original ExpectedDiagnostics are not inherited.
                        InheritanceMode = StateInheritanceMode.Explicit,
                        Sources = { "namespace MyNamespace {" },
                    },
                }.RunAsync();
            });

            var expected =
                "Context: Diagnostics of fixed state" + Environment.NewLine +
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// /0/Test0.cs(1,24): error CS1513: } expected" + Environment.NewLine +
                "DiagnosticResult.CompilerError(\"CS1513\").WithSpan(1, 24, 1, 24)," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByRemovingAnalyzerConfigFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileMissing)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|Brace:{|} }" },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
                FixedState =
                {
                    InheritanceMode = StateInheritanceMode.Explicit,
                    Sources = { "namespace MyNamespace { }" },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByRemovingAnalyzerConfigFileWithCompileError()
        {
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileMissing)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|Brace:{|}{|CS1513:|}" },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
                FixedState =
                {
                    InheritanceMode = StateInheritanceMode.Explicit,
                    Sources = { "namespace MyNamespace {{|CS1513:|}" },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByRemovingAnalyzerConfigFileAllowsMarkupInFixedState()
        {
            var testCode = "namespace MyNamespace {|Brace:{|} }";
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileMissing)
            {
                TestState =
                {
                    Sources = { testCode },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
                FixedState =
                {
                    InheritanceMode = StateInheritanceMode.Explicit,
                    Sources = { testCode },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByRemovingAnalyzerConfigFileAllowsMarkupInFixedStateKeepsCompileErrors()
        {
            var testCode = "namespace MyNamespace {|Brace:{|}{|CS1513:|}";
            await new CSharpTest(SuppressDiagnosticIf.AnalyzerConfigFileMissing)
            {
                TestState =
                {
                    Sources = { testCode },
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8)),
                    },
                },
                FixedState =
                {
                    InheritanceMode = StateInheritanceMode.Explicit,
                    Sources = { testCode },
                },
            }.RunAsync();
        }

        private enum SuppressDiagnosticIf
        {
            AnalyzerConfigFileExists,
            AnalyzerConfigFileMissing,
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private class HighlightBracesAnalyzer : DiagnosticAnalyzer
        {
            internal static readonly DiagnosticDescriptor Descriptor =
                new DiagnosticDescriptor("Brace", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            private readonly SuppressDiagnosticIf _suppressDiagnosticIf;

            public HighlightBracesAnalyzer(SuppressDiagnosticIf suppressDiagnosticIf)
            {
                _suppressDiagnosticIf = suppressDiagnosticIf;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

                context.RegisterSyntaxTreeAction(HandleSyntaxTree);
            }

            private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                if (_suppressDiagnosticIf == SuppressDiagnosticIf.AnalyzerConfigFileExists && context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree).TryGetValue("key", out _))
                {
                    return;
                }
                else if (_suppressDiagnosticIf == SuppressDiagnosticIf.AnalyzerConfigFileMissing && !context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree).TryGetValue("key", out _))
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

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        private class ToggleAnalyzerConfigFileFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds
                => ImmutableArray.Create(HighlightBracesAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider()
                => new FixAll();

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                var hasAnalyzerConfigFiles = context.Document.Project.AnalyzerConfigDocuments.Any();
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "ToggleFile",
                            ct => CreateChangedSolution(context.Document, remove: hasAnalyzerConfigFiles, ct),
                            nameof(ToggleAnalyzerConfigFileFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }

            private static Task<Solution> CreateChangedSolution(Document document, bool remove, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;
                if (remove)
                {
                    foreach (var config in document.Project.AnalyzerConfigDocuments)
                    {
                        solution = solution.RemoveAnalyzerConfigDocument(config.Id);
                    }
                }
                else
                {
                    var id = DocumentId.CreateNewId(document.Project.Id, "/.editorconfig");
                    solution = solution.AddAnalyzerConfigDocument(id, "/.editorconfig", SourceText.From(RootEditorConfig, Encoding.UTF8), filePath: "/.editorconfig");
                }

                return Task.FromResult(solution);
            }

            private class FixAll : FixAllProvider
            {
                public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
                {
                    var hasAnalyzerConfigFiles = fixAllContext.Solution.Projects.Single().AnalyzerConfigDocuments.Any();
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        "ToggleFile",
                        ct => CreateChangedSolution(fixAllContext.Solution.Projects.Single().Documents.First(), remove: hasAnalyzerConfigFiles, ct),
                        nameof(ToggleAnalyzerConfigFileFix)));
                }
            }
        }

        private class CSharpTest : CSharpCodeFixTest<EmptyDiagnosticAnalyzer, ToggleAnalyzerConfigFileFix>
        {
            private readonly SuppressDiagnosticIf _suppressDiagnosticIf;

            public CSharpTest(SuppressDiagnosticIf suppressDiagnosticIf)
            {
                _suppressDiagnosticIf = suppressDiagnosticIf;
            }

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new HighlightBracesAnalyzer(_suppressDiagnosticIf);
            }
        }
    }
}

#endif
