// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AdditionalFilesFixTests
    {
        [Fact]
        public async Task TestDiagnosticFixedByAddingAdditionalFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileExists)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23) },
                },
                FixedState =
                {
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByAddingAdditionalFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileExists)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|Brace:{|} }" },
                },
                FixedState =
                {
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByAddingAdditionalFileFailsIfTextIncorrect()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(SuppressDiagnosticIf.AdditionalFileExists)
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {|Brace:{|} }" },
                    },
                    FixedState =
                    {
                        AdditionalFiles =
                        {
                            ("File1.txt", "Wrong file text"),
                        },
                    },
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}content of 'File1.txt' did not match. Diff shown with expected as baseline:{Environment.NewLine}-Wrong file text{Environment.NewLine}+File text{Environment.NewLine}", exception.Message);
        }

        [Fact]
        public async Task TestMarkupDiagnosticFixedByAddingAdditionalFileFailsIfTextIndentedIncorrectly()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(SuppressDiagnosticIf.AdditionalFileExists)
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {|Brace:{|} }" },
                    },
                    FixedState =
                    {
                        AdditionalFiles =
                        {
                            ("File1.txt", "  File text"),
                        },
                    },
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}content of 'File1.txt' did not match. Diff shown with expected as baseline:{Environment.NewLine}-  File text{Environment.NewLine}+File text{Environment.NewLine}", exception.Message);
        }

        [Fact]
        public async Task TestDiagnosticFixedByRemovingAdditionalFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileMissing)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace { }" },
                    ExpectedDiagnostics = { new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23) },
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
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
        public async Task TestDiagnosticFixedByRemovingAdditionalFileWithUndeclaredCompileError()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest(SuppressDiagnosticIf.AdditionalFileMissing)
                {
                    TestState =
                    {
                        Sources = { "namespace MyNamespace {" },
                        ExpectedDiagnostics =
                        {
                            new DiagnosticResult(HighlightBracesAnalyzer.Descriptor).WithLocation(1, 23),
                            DiagnosticResult.CompilerError("CS1513").WithLocation(1, 24),
                        },
                        AdditionalFiles =
                        {
                            ("File1.txt", "File text"),
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
        public async Task TestMarkupDiagnosticFixedByRemovingAdditionalFile()
        {
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileMissing)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|Brace:{|} }" },
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
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
        public async Task TestMarkupDiagnosticFixedByRemovingAdditionalFileWithCompileError()
        {
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileMissing)
            {
                TestState =
                {
                    Sources = { "namespace MyNamespace {|Brace:{|}{|CS1513:|}" },
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
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
        public async Task TestMarkupDiagnosticFixedByRemovingAdditionalFileAllowsMarkupInFixedState()
        {
            var testCode = "namespace MyNamespace {|Brace:{|} }";
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileMissing)
            {
                TestState =
                {
                    Sources = { testCode },
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
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
        public async Task TestMarkupDiagnosticFixedByRemovingAdditionalFileAllowsMarkupInFixedStateKeepsCompileErrors()
        {
            var testCode = "namespace MyNamespace {|Brace:{|}{|CS1513:|}";
            await new CSharpTest(SuppressDiagnosticIf.AdditionalFileMissing)
            {
                TestState =
                {
                    Sources = { testCode },
                    AdditionalFiles =
                    {
                        ("File1.txt", "File text"),
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
            AdditionalFileExists,
            AdditionalFileMissing,
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
                if (_suppressDiagnosticIf == SuppressDiagnosticIf.AdditionalFileExists && !context.Options.AdditionalFiles.IsEmpty)
                {
                    return;
                }
                else if (_suppressDiagnosticIf == SuppressDiagnosticIf.AdditionalFileMissing && context.Options.AdditionalFiles.IsEmpty)
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
        private class ToggleAdditionalFileFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds
                => ImmutableArray.Create(HighlightBracesAnalyzer.Descriptor.Id);

            public override FixAllProvider GetFixAllProvider()
                => new FixAll();

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                var hasAdditionalFiles = context.Document.Project.AdditionalDocumentIds.Count > 0;
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "ToggleFile",
                            ct => CreateChangedSolution(context.Document, remove: hasAdditionalFiles, ct),
                            nameof(ToggleAdditionalFileFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }

            private static Task<Solution> CreateChangedSolution(Document document, bool remove, CancellationToken cancellationToken)
            {
                var solution = document.Project.Solution;
                if (remove)
                {
                    foreach (var id in document.Project.AdditionalDocumentIds)
                    {
                        solution = solution.RemoveAdditionalDocument(id);
                    }
                }
                else
                {
                    var id = DocumentId.CreateNewId(document.Project.Id, "File1.txt");
                    solution = solution.AddAdditionalDocument(id, "File1.txt", "File text", filePath: "File1.txt");
                }

                return Task.FromResult(solution);
            }

            private class FixAll : FixAllProvider
            {
                public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
                {
                    var hasAdditionalFiles = fixAllContext.Solution.Projects.Single().AdditionalDocumentIds.Count > 0;
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        "ToggleFile",
                        ct => CreateChangedSolution(fixAllContext.Solution.Projects.Single().Documents.First(), remove: hasAdditionalFiles, ct),
                        nameof(ToggleAdditionalFileFix)));
                }
            }
        }

        private class CSharpTest : CSharpCodeFixTest<EmptyDiagnosticAnalyzer, ToggleAdditionalFileFix>
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
