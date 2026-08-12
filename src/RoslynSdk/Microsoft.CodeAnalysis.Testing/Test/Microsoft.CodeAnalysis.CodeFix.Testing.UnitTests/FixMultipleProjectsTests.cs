// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestFixes.CSharpCodeFixTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.LiteralUnderFiveAnalyzer,
    Microsoft.CodeAnalysis.Testing.TestFixes.IncrementFix>;
using VisualBasicTest = Microsoft.CodeAnalysis.Testing.TestFixes.VisualBasicCodeFixTest<
    Microsoft.CodeAnalysis.Testing.TestAnalyzers.LiteralUnderFiveAnalyzer,
    Microsoft.CodeAnalysis.Testing.TestFixes.IncrementFix>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class FixMultipleProjectsTests
    {
        [Fact]
        public async Task TwoCSharpProjects_Independent()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Type1 { int field = [|4|]; }",
                        @"public class Type2 { int field = [|4|]; }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Type3 { int field = [|4|]; }",
                                @"public class Type4 { int field = [|4|]; }",
                            },
                        },
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"public class Type1 { int field =  5; }",
                        @"public class Type2 { int field =  5; }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"public class Type3 { int field =  5; }",
                                @"public class Type4 { int field =  5; }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoVisualBasicProjects_Independent()
        {
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Type1 : Private field = [|4|] : End Class",
                        @"Public Class Type2 : Private field = [|4|] : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"Public Class Type3 : Private field = [|4|] : End Class",
                                @"Public Class Type4 : Private field = [|4|] : End Class",
                            },
                        },
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"Public Class Type1 : Private field =  5 : End Class",
                        @"Public Class Type2 : Private field =  5 : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary"] =
                        {
                            Sources =
                            {
                                @"Public Class Type3 : Private field =  5 : End Class",
                                @"Public Class Type4 : Private field =  5 : End Class",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneCSharpProjectOneVisualBasicProject_Independent()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"public class Type1 { int field = [|4|]; }",
                        @"public class Type2 { int field = [|4|]; }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"Public Class Type3 : Private field = [|4|] : End Class",
                                @"Public Class Type4 : Private field = [|4|] : End Class",
                            },
                        },
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"public class Type1 { int field =  5; }",
                        @"public class Type2 { int field =  5; }",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.VisualBasic] =
                        {
                            Sources =
                            {
                                @"Public Class Type3 : Private field =  5 : End Class",
                                @"Public Class Type4 : Private field =  5 : End Class",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task OneVisualBasicProjectOneCSharpProject_Independent()
        {
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources =
                    {
                        @"Public Class Type1 : Private field = [|4|] : End Class",
                        @"Public Class Type2 : Private field = [|4|] : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.CSharp] =
                        {
                            Sources =
                            {
                                @"public class Type3 { int field = [|4|]; }",
                                @"public class Type4 { int field = [|4|]; }",
                            },
                        },
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"Public Class Type1 : Private field =  5 : End Class",
                        @"Public Class Type2 : Private field =  5 : End Class",
                    },
                    AdditionalProjects =
                    {
                        ["Secondary", LanguageNames.CSharp] =
                        {
                            Sources =
                            {
                                @"public class Type3 { int field =  5; }",
                                @"public class Type4 { int field =  5; }",
                            },
                        },
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TwoCSharpProjects_Independent_UnexpectedDiagnostic()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources =
                        {
                            @"public class Type1 { int field = [|4|]; }",
                            @"public class Type2 { int field = [|4|]; }",
                        },
                        AdditionalProjects =
                        {
                            ["Secondary"] =
                            {
                                Sources =
                                {
                                    @"public class Type3 { int field = [|4|]; }",
                                    @"public class Type4 { int field = [|4|]; }",
                                },
                            },
                        },
                    },
                    FixedState =
                    {
                        Sources =
                        {
                            @"public class Type1 { int field =  5; }",
                            @"public class Type2 { int field =  5; }",
                        },
                        AdditionalProjects =
                        {
                            ["Secondary"] =
                            {
                                Sources =
                                {
                                    @"public class Type3 { int field =  [|5|]; }",
                                    @"public class Type4 { int field =  5; }",
                                },
                            },
                        },
                        MarkupHandling = MarkupMode.Allow,
                    },
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff(
                """
                Context: Diagnostics of fixed state
                Mismatch between number of diagnostics returned, expected "1" actual "0"

                Diagnostics:
                    NONE.

                """.ReplaceLineEndings(),
                exception.Message);
        }

        [Fact]
        public async Task TwoCSharpProjects_Independent_UnexpectedContent()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestState =
                    {
                        Sources =
                        {
                            @"public class Type1 { int field = [|4|]; }",
                            @"public class Type2 { int field = [|4|]; }",
                        },
                        AdditionalProjects =
                        {
                            ["Secondary"] =
                            {
                                Sources =
                                {
                                    @"public class Type3 { int field = [|4|]; }",
                                    @"public class Type4 { int field = [|4|]; }",
                                },
                            },
                        },
                    },
                    FixedState =
                    {
                        Sources =
                        {
                            @"public class Type1 { int field =  5; }",
                            @"public class Type2 { int field =  5; }",
                        },
                        AdditionalProjects =
                        {
                            ["Secondary"] =
                            {
                                Sources =
                                {
                                    @"public class Type3 { int field = 5; }",
                                    @"public class Type4 { int field =  5; }",
                                },
                            },
                        },
                    },
                }.RunAsync();
            });

            new DefaultVerifier().EqualOrDiff(
                """
                Context: Iterative code fix application
                content of '/Secondary/Test0.cs' did not match. Diff shown with expected as baseline:
                -public class Type3 { int field = 5; }
                +public class Type3 { int field =  5; }

                """.ReplaceLineEndings(),
                exception.Message);
        }
    }
}
