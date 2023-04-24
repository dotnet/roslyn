// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
    public class ChainedExpressionWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        [Fact]
        public async Task TestMissingWithSyntaxError()
        {
            await TestMissingAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick().brown.fox(,);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutEnoughChunks()
        {
            await TestMissingAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestWithEnoughChunks()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown().fox.jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown().fox
                            .jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown().fox
                                 .jumped();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestGenericNames()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown<int>().fox.jumped<string, bool>();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown<int>().fox
                            .jumped<string, bool>();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown<int>().fox
                                 .jumped<string, bool>();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestElementAccess()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown[1, 2, 3].fox.jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox
                            .jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox
                                 .jumped[1][2][3];
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUnwrap()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown[1, 2, 3].fox
                                 .jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox
                            .jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox.jumped[1][2][3];
                    }
                }
                """);
        }

        [Fact]
        public async Task TestWrapAndUnwrap()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.
                                brown[1, 2, 3]
                           .fox.jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox
                            .jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox
                                 .jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown[1, 2, 3].fox.jumped[1][2][3];
                    }
                }
                """);
        }

        [Fact]
        public async Task TestChunkMustHaveDottedSection()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the().quick.brown[1, 2, 3].fox.jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the().quick.brown[1, 2, 3].fox
                            .jumped[1][2][3];
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the().quick.brown[1, 2, 3].fox
                                   .jumped[1][2][3];
                    }
                }
                """);
        }

        [Fact]
        public async Task TrailingNonCallIsNotWrapped()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown().fox.jumped().over;
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown().fox
                            .jumped().over;
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the.quick.brown().fox
                                 .jumped().over;
                    }
                }
                """);
        }

        [Fact]
        public async Task TrailingLongWrapping1()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown().fox.jumped().over.the().lazy().dog();
                    }
                }
                """,
GetIndentionColumn(35),
"""
class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over
            .the()
            .lazy()
            .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the()
                 .lazy()
                 .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over.the()
            .lazy().dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the().lazy()
                 .dog();
    }
}
""");
        }

        [Fact]
        public async Task TrailingLongWrapping2()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown().fox.jumped().over.the().lazy().dog();
                    }
                }
                """,
GetIndentionColumn(40),
"""
class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over
            .the()
            .lazy()
            .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the()
                 .lazy()
                 .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over.the().lazy()
            .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over.the()
                 .lazy().dog();
    }
}
""");
        }

        [Fact]
        public async Task TrailingLongWrapping3()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the.quick.brown().fox.jumped().over.the().lazy().dog();
                    }
                }
                """,
GetIndentionColumn(60),
"""
class C {
    void Bar() {
        the.quick.brown().fox
            .jumped().over
            .the()
            .lazy()
            .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox
                 .jumped().over
                 .the()
                 .lazy()
                 .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox.jumped().over.the().lazy()
            .dog();
    }
}
""",
"""
class C {
    void Bar() {
        the.quick.brown().fox.jumped().over.the().lazy()
                 .dog();
    }
}
""");
        }

        [Fact]
        public async Task TestInConditionalAccess()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        the?.[||]quick.brown().fox.jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick.brown().fox
                            .jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick.brown().fox
                                  .jumped();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInConditionalAccess2()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        the?.[||]quick.brown()?.fox.jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick.brown()?.fox
                            .jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick.brown()?.fox
                                  .jumped();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInConditionalAccess3()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        the?.[||]quick.brown()?.fox().jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick.brown()?.fox()
                            .jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick.brown()?.fox()
                                  .jumped();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInConditionalAccess4()
        {
            await TestAllWrappingCasesAsync(
                """
                class C {
                    void Bar() {
                        [||]the?.quick().brown()?.fox().jumped();
                    }
                }
                """,
                """
                class C {
                    void Bar() {
                        the?.quick()
                            .brown()?.fox()
                            .jumped();
                    }
                }
                """);
        }
    }
}
