// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceGeneratorTests
    {
        [Fact]
        public async Task TestOneIterationRequiredWithoutChangingGeneratedSource()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =  5;
                }

                """;

            await new CSharpTest<TreeNameGenerator>
            {
                TestState =
                {
                    Sources = { testCode },
                    GeneratedSources =
                    {
                        (typeof(TreeNameGenerator), "Generated.g.cs", CreateExpectedGeneratedSource("// Test0.cs")),
                    },
                },
                FixedState =
                {
                    Sources = { fixedCode },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneIterationRequiredWithChangeToGeneratedSource()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =  5;
                }

                """;

            await new CSharpTest<LiteralValueGenerator>
            {
                TestState =
                {
                    Sources = { testCode },
                    GeneratedSources =
                    {
                        (typeof(LiteralValueGenerator), "Generated.g.cs", CreateExpectedGeneratedSource("// Test0.cs: 4")),
                    },
                },
                FixedState =
                {
                    Sources = { fixedCode },
                    GeneratedSources =
                    {
                        (typeof(LiteralValueGenerator), "Generated.g.cs", CreateExpectedGeneratedSource("// Test0.cs: 5")),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestErrorForMissingGeneratedSourcesInTestState()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =  5;
                }

                """;

            // No error is reported if generated source validation is disabled
            await CreateTest(TestBehaviors.SkipGeneratedSourcesCheck).RunAsync();

            // Generated sources are validated by the default behaviors
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await CreateTest(TestBehaviors.None).RunAsync();
            });

            var expectedMessage =
                $"""
                Context: Generated sources of test state
                Context: Source generator application
                Context: Verifying source generated files
                Expected source file list to match
                +{GetGeneratedFilePath(typeof(LiteralValueGenerator), "Generated.g.cs")}

                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());

            CSharpTest<LiteralValueGenerator> CreateTest(TestBehaviors testBehaviors)
            {
                return new CSharpTest<LiteralValueGenerator>
                {
                    TestBehaviors = testBehaviors,
                    TestState =
                    {
                        Sources = { testCode },
                    },
                    FixedState =
                    {
                        Sources = { fixedCode },
                        GeneratedSources =
                        {
                            (typeof(LiteralValueGenerator), "Generated.g.cs", CreateExpectedGeneratedSource("// Test0.cs: 5")),
                        },
                    },
                };
            }
        }

        [Fact]
        public async Task TestErrorForMissingGeneratedSourcesInFixedState()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =  5;
                }

                """;

            // No error is reported if generated source validation is disabled
            await CreateTest(TestBehaviors.SkipGeneratedSourcesCheck).RunAsync();

            // Generated sources are validated by the default behaviors
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await CreateTest(TestBehaviors.None).RunAsync();
            });

            var expectedMessage =
                $"""
                Context: Iterative code fix application
                Context: Generated sources of fixed state
                Context: Source generator application
                Context: Verifying source generated files
                Expected source file list to match
                +{GetGeneratedFilePath(typeof(LiteralValueGenerator), "Generated.g.cs")}

                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());

            CSharpTest<LiteralValueGenerator> CreateTest(TestBehaviors testBehaviors)
            {
                return new CSharpTest<LiteralValueGenerator>
                {
                    TestBehaviors = testBehaviors,
                    TestState =
                    {
                        Sources = { testCode },
                        GeneratedSources =
                        {
                            (typeof(LiteralValueGenerator), "Generated.g.cs", CreateExpectedGeneratedSource("// Test0.cs: 4")),
                        },
                    },
                    FixedState =
                    {
                        Sources = { fixedCode },
                        InheritanceMode = StateInheritanceMode.Explicit,
                    },
                };
            }
        }

        private static string GetGeneratedFilePath(Type sourceGeneratorType, string fileName)
            => Path.Combine(sourceGeneratorType.Assembly.GetName().Name!, sourceGeneratorType.FullName!, fileName);

        private static SourceText CreateExpectedGeneratedSource(string source)
            => SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256);

        private class CSharpTest<TSourceGenerator> : CSharpCodeFixWithSourceGeneratorTest<LiteralUnderFiveAnalyzer, IncrementFix, TSourceGenerator>
            where TSourceGenerator : ISourceGenerator, new()
        {
            public int DiagnosticIndexToFix { get; set; }

            public CSharpTest()
            {
                CodeActionValidationMode = CodeActionValidationMode.None;
            }

            protected override Diagnostic? TrySelectDiagnosticToFix(ImmutableArray<Diagnostic> fixableDiagnostics)
            {
                return fixableDiagnostics[DiagnosticIndexToFix];
            }
        }

        [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
#pragma warning disable RS1042 // Do not implement
        internal class TreeNameGenerator : ISourceGenerator
#pragma warning restore RS1042 // Do not implement
        {
            private const string CSharpCommentPrefix = @"//";
            private const string VisualBasicCommentPrefix = @"'";

            public void Execute(GeneratorExecutionContext context)
            {
                var prefix = context.Compilation.Language == LanguageNames.CSharp ? CSharpCommentPrefix : VisualBasicCommentPrefix;
                var sourceBuilder = new StringBuilder();
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    if (sourceBuilder.Length > 0)
                    {
                        sourceBuilder.Append('\n');
                    }

                    sourceBuilder.Append($"{prefix} {Path.GetFileName(tree.FilePath)}");
                }

                var source = sourceBuilder.ToString();
                var hintName = context.Compilation.Language == LanguageNames.CSharp
                    ? "Generated.g.cs"
                    : "Generated.g.vb";

                context.AddSource(hintName, source);
            }

            public void Initialize(GeneratorInitializationContext context)
            {
            }
        }

        [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
#pragma warning disable RS1042 // Do not implement
        internal class LiteralValueGenerator : ISourceGenerator
#pragma warning restore RS1042 // Do not implement
        {
            private const string CSharpCommentPrefix = @"//";
            private const string VisualBasicCommentPrefix = @"'";

            public void Execute(GeneratorExecutionContext context)
            {
                var prefix = context.Compilation.Language == LanguageNames.CSharp ? CSharpCommentPrefix : VisualBasicCommentPrefix;
                var sourceBuilder = new StringBuilder();
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    if (sourceBuilder.Length > 0)
                    {
                        sourceBuilder.Append('\n');
                    }

                    var literalValue = tree.GetRoot(context.CancellationToken).DescendantTokens().Single(token => token.Value is int).ValueText;
                    sourceBuilder.Append($"{prefix} {Path.GetFileName(tree.FilePath)}: {literalValue}");
                }

                var source = sourceBuilder.ToString();
                var hintName = context.Compilation.Language == LanguageNames.CSharp
                    ? "Generated.g.cs"
                    : "Generated.g.vb";

                context.AddSource(hintName, source);
            }

            public void Initialize(GeneratorInitializationContext context)
            {
            }
        }
    }
}
