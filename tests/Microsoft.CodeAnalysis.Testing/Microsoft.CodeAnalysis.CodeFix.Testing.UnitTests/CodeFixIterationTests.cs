// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Testing.TestFixes;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class CodeFixIterationTests
    {
        [Fact]
        public async Task TestOneIterationRequired()
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

            await new CSharpTest
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneIterationEachForTwoDiagnostics()
        {
            var testCode =
                """

                class TestClass {
                  int x = [|4|];
                  int y = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int x =  5;
                  int y =  5;
                }

                """;

            await new CSharpTest
            {
                TestCode = testCode,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneIterationEachForTwoDiagnosticsFixOnlyFirst()
        {
            var testCode =
                """

                class TestClass {
                  int x = [|4|];
                  int y = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int x =  5;
                  int y = [|4|];
                }

                """;
            var batchFixedCode =
                """

                class TestClass {
                  int x =  5;
                  int y =  5;
                }

                """;

            await new CSharpTest
            {
                TestCode = testCode,
                FixedState =
                {
                    Sources = { fixedCode },
                    MarkupHandling = MarkupMode.Allow,
                },
                BatchFixedCode = batchFixedCode,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                DiagnosticIndexToFix = 0,
            }.RunAsync();
        }

        [Fact]
        public async Task TestOneIterationEachForTwoDiagnosticsFixOnlySecond()
        {
            var testCode =
                """

                class TestClass {
                  int x = [|4|];
                  int y = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int x = [|4|];
                  int y =  5;
                }

                """;
            var batchFixedCode =
                """

                class TestClass {
                  int x =  5;
                  int y =  5;
                }

                """;

            await new CSharpTest
            {
                TestCode = testCode,
                FixedState =
                {
                    Sources = { fixedCode },
                    MarkupHandling = MarkupMode.Allow,
                },
                BatchFixedCode = batchFixedCode,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                DiagnosticIndexToFix = 1,
            }.RunAsync();
        }

        [Fact]
        public async Task TestThreeIterationsForTwoDiagnostics()
        {
            var testCode =
                """

                class TestClass {
                  int x = [|3|];
                  int y = [|4|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int x =   5;
                  int y =  5;
                }

                """;

            await new CSharpTest
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                NumberOfIncrementalIterations = 3,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(-2, 2)]
        [InlineData(-3, 2)]
        [InlineData(2, -2)]
        [InlineData(2, -3)]
        public async Task TestTwoIterationsRequired(int declaredIncrementalIterations, int declaredFixAllIterations)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =   5;
                }

                """;

            await new CSharpTest
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                NumberOfIncrementalIterations = declaredIncrementalIterations,
                NumberOfFixAllIterations = declaredFixAllIterations,
            }.RunAsync();
        }

        [Fact]
        public async Task TestTwoIterationsRequiredButIncrementalNotDeclared()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =   5;
                }

                """;
            var batchFixedCode =
                """

                class TestClass {
                  int field =   5;
                }

                """;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    FixedCode = fixedCode,
                    BatchFixedCode = batchFixedCode,
                    NumberOfFixAllIterations = 2,
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}Expected '1' iterations but found '2' iterations.", exception.Message);
        }

        [Theory]
        [InlineData(-1, "Expected '1' iterations but found '2' iterations.", "  5")]
        [InlineData(0, "The upper limit for the number of code fix iterations was exceeded", " [|4|]")]
        [InlineData(1, "Expected '1' iterations but found '2' iterations.", "  5")]
        public async Task TestTwoIterationsRequiredButIncrementalDeclaredIncorrectly(int declaredIncrementalIterations, string message, string replacement)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                $$"""

                class TestClass {
                  int field = {{replacement}};
                }

                """;
            var batchFixedCode =
                """

                class TestClass {
                  int field =   5;
                }

                """;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    FixedState = { Sources = { fixedCode }, MarkupHandling = MarkupMode.Allow },
                    BatchFixedCode = batchFixedCode,
                    NumberOfIncrementalIterations = declaredIncrementalIterations,
                    NumberOfFixAllIterations = 2,
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}{message}", exception.Message);
        }

        [Fact]
        public async Task TestTwoIterationsRequiredButFixAllNotDeclared()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =   5;
                }

                """;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    FixedCode = fixedCode,
                    NumberOfIncrementalIterations = 2,
                }.RunAsync();
            });

            Assert.Equal($"Context: Fix all in document{Environment.NewLine}Expected '1' iterations but found '2' iterations.", exception.Message);
        }

        [Theory]
        [InlineData(-1, "Expected '1' iterations but found '2' iterations.", "  5")]
        [InlineData(0, "The upper limit for the number of fix all iterations was exceeded", " [|4|]")]
        [InlineData(1, "Expected '1' iterations but found '2' iterations.", "  5")]
        public async Task TestTwoIterationsRequiredButFixAllDeclaredIncorrectly(int declaredFixAllIterations, string message, string replacement)
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =   5;
                }

                """;
            var fixAllCode =
                $$"""

                class TestClass {
                  int field = {{replacement}};
                }

                """;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    FixedCode = fixedCode,
                    BatchFixedState = { Sources = { fixAllCode }, MarkupHandling = MarkupMode.Allow },
                    NumberOfIncrementalIterations = 2,
                    NumberOfFixAllIterations = declaredFixAllIterations,
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Fix all in document{Environment.NewLine}{message}", exception.Message);
        }

        [Theory]
        [WorkItem(147, "https://github.com/dotnet/roslyn-sdk/issues/147")]
        [InlineData(CodeFixTestBehaviors.None)]
        [InlineData(CodeFixTestBehaviors.SkipFixAllInDocumentCheck)]
        [InlineData(CodeFixTestBehaviors.SkipFixAllInDocumentCheck | CodeFixTestBehaviors.SkipFixAllInProjectCheck)]
        [InlineData(CodeFixTestBehaviors.SkipFixAllInDocumentCheck | CodeFixTestBehaviors.SkipFixAllInSolutionCheck)]
        [InlineData(CodeFixTestBehaviors.SkipFixAllCheck)]
        public async Task TestOneIterationRequiredForEachOfTwoDocuments(CodeFixTestBehaviors codeFixTestBehaviors)
        {
            var testCode1 =
                """

                class TestClass1 {
                  int field = [|4|];
                }

                """;
            var testCode2 =
                """

                class TestClass2 {
                  int field = [|4|];
                }

                """;
            var fixedCode1 =
                """

                class TestClass1 {
                  int field =  5;
                }

                """;
            var fixedCode2 =
                """

                class TestClass2 {
                  int field =  5;
                }

                """;

            await new CSharpTest
            {
                TestState = { Sources = { testCode1, testCode2 } },
                FixedState = { Sources = { fixedCode1, fixedCode2 } },
                CodeFixTestBehaviors = codeFixTestBehaviors,
            }.RunAsync();
        }

        [Theory]
        [WorkItem(147, "https://github.com/dotnet/roslyn-sdk/issues/147")]
        [InlineData(CodeFixTestBehaviors.None, "Fix all in project")]
        [InlineData(CodeFixTestBehaviors.SkipFixAllInProjectCheck, "Fix all in solution")]
        public async Task TestOneIterationRequiredForEachOfTwoDocumentsButDeclaredForAll(CodeFixTestBehaviors codeFixTestBehaviors, string context)
        {
            var testCode1 =
                """

                class TestClass1 {
                  int field = [|4|];
                }

                """;
            var testCode2 =
                """

                class TestClass2 {
                  int field = [|4|];
                }

                """;
            var fixedCode1 =
                """

                class TestClass1 {
                  int field =  5;
                }

                """;
            var fixedCode2 =
                """

                class TestClass2 {
                  int field =  5;
                }

                """;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState = { Sources = { testCode1, testCode2 } },
                    FixedState = { Sources = { fixedCode1, fixedCode2 } },
                    NumberOfFixAllIterations = 2,
                    CodeFixTestBehaviors = codeFixTestBehaviors,
                }.RunAsync();
            });

            Assert.Equal($"Context: {context}{Environment.NewLine}Expected '2' iterations but found '1' iterations.", exception.Message);
        }

        [Fact]
        [WorkItem(874, "https://github.com/dotnet/roslyn-sdk/issues/874")]
        public async Task TestTwoIterationsRequiredButOneApplied()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =  [|4|];
                }

                """;

            await new CSharpTest
            {
                TestCode = testCode,
                FixedState =
                {
                    Sources = { fixedCode },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeActionEquivalenceKey = "IncrementFix:4",
                CodeActionIndex = 0,
            }.RunAsync();
        }

        [Fact]
        [WorkItem(874, "https://github.com/dotnet/roslyn-sdk/issues/874")]
        public async Task TestTwoIterationsRequiredButNoneApplied()
        {
            var testCode =
                """

                class TestClass {
                  int field = [|3|];
                }

                """;
            var fixedCode =
                """

                class TestClass {
                  int field =  [|4|];
                }

                """;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    FixedState =
                    {
                        Sources = { fixedCode },
                        MarkupHandling = MarkupMode.Allow,
                    },
                    CodeActionEquivalenceKey = "IncrementFix:3",
                    CodeActionIndex = 0,
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff($"Context: Iterative code fix application{Environment.NewLine}The code action equivalence key and index must be consistent when both are specified.", exception.Message);
        }

        private class CSharpTest : CSharpCodeFixTest<LiteralUnderFiveAnalyzer, IncrementFix>
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
    }
}
