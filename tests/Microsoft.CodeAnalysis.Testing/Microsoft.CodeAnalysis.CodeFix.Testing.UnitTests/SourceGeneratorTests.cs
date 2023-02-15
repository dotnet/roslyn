// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceGeneratorTests
    {
        [Fact]
        public async Task TestOneIterationRequiredWithoutChangingGeneratedSource()
        {
            var testCode = @"
class TestClass {
  int field = [|4|];
}
";
            var fixedCode = @"
class TestClass {
  int field =  5;
}
";

            await new CSharpTest<TreeNameGenerator>
            {
                TestState =
                {
                    Sources = { testCode },
                    GeneratedSources =
                    {
                        (typeof(TreeNameGenerator), "Generated.g.cs", "// Test0.cs\r\n"),
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
            var testCode = @"
class TestClass {
  int field = [|4|];
}
";
            var fixedCode = @"
class TestClass {
  int field =  5;
}
";

            await new CSharpTest<TreeLengthGenerator>
            {
                TestState =
                {
                    Sources = { testCode },
                    GeneratedSources =
                    {
                        (typeof(TreeLengthGenerator), "Generated.g.cs", "// Test0.cs: 42\r\n"),
                    },
                },
                FixedState =
                {
                    Sources = { fixedCode },
                    GeneratedSources =
                    {
                        (typeof(TreeLengthGenerator), "Generated.g.cs", "// Test0.cs: 43\r\n"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestErrorForMissingGeneratedSourcesInTestState()
        {
            var testCode = @"
class TestClass {
  int field = [|4|];
}
";
            var fixedCode = @"
class TestClass {
  int field =  5;
}
";

            // No error is reported if generated source validation is disabled
            await CreateTest(TestBehaviors.SkipGeneratedSourcesCheck).RunAsync();

            // Generated sources are validated by the default behaviors
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await CreateTest(TestBehaviors.None).RunAsync();
            });

            var expectedMessage = @"Context: Generated sources of test state
Context: Source generator application
Context: Verifying source generated files
Expected source file list to match
+Microsoft.CodeAnalysis.CodeFix.Testing.UnitTests\Microsoft.CodeAnalysis.Testing.SourceGeneratorTests+TreeLengthGenerator\Generated.g.cs
";
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message);

            CSharpTest<TreeLengthGenerator> CreateTest(TestBehaviors testBehaviors)
            {
                return new CSharpTest<TreeLengthGenerator>
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
                            (typeof(TreeLengthGenerator), "Generated.g.cs", "// Test0.cs: 43\r\n"),
                        },
                    },
                };
            }
        }

        [Fact]
        public async Task TestErrorForMissingGeneratedSourcesInFixedState()
        {
            var testCode = @"
class TestClass {
  int field = [|4|];
}
";
            var fixedCode = @"
class TestClass {
  int field =  5;
}
";

            // No error is reported if generated source validation is disabled
            await CreateTest(TestBehaviors.SkipGeneratedSourcesCheck).RunAsync();

            // Generated sources are validated by the default behaviors
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await CreateTest(TestBehaviors.None).RunAsync();
            });

            var expectedMessage = @"Context: Iterative code fix application
Context: Generated sources of fixed state
Context: Source generator application
Context: Verifying source generated files
Expected source file list to match
+Microsoft.CodeAnalysis.CodeFix.Testing.UnitTests\Microsoft.CodeAnalysis.Testing.SourceGeneratorTests+TreeLengthGenerator\Generated.g.cs
";
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message);

            CSharpTest<TreeLengthGenerator> CreateTest(TestBehaviors testBehaviors)
            {
                return new CSharpTest<TreeLengthGenerator>
                {
                    TestBehaviors = testBehaviors,
                    TestState =
                    {
                        Sources = { testCode },
                        GeneratedSources =
                        {
                            (typeof(TreeLengthGenerator), "Generated.g.cs", "// Test0.cs: 42\r\n"),
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
        internal class TreeNameGenerator : ISourceGenerator
        {
            private const string CSharpCommentPrefix = @"//";
            private const string VisualBasicCommentPrefix = @"'";

            public void Execute(GeneratorExecutionContext context)
            {
                var prefix = context.Compilation.Language == LanguageNames.CSharp ? CSharpCommentPrefix : VisualBasicCommentPrefix;
                var sourceBuilder = new StringBuilder();
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    sourceBuilder.AppendLine($"{prefix} {Path.GetFileName(tree.FilePath)}");
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
        internal class TreeLengthGenerator : ISourceGenerator
        {
            private const string CSharpCommentPrefix = @"//";
            private const string VisualBasicCommentPrefix = @"'";

            public void Execute(GeneratorExecutionContext context)
            {
                var prefix = context.Compilation.Language == LanguageNames.CSharp ? CSharpCommentPrefix : VisualBasicCommentPrefix;
                var sourceBuilder = new StringBuilder();
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    sourceBuilder.AppendLine($"{prefix} {Path.GetFileName(tree.FilePath)}: {tree.Length}");
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
