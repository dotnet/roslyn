// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

[Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
public sealed class CollectionExpressionWrappingTests : AbstractWrappingTests
{
    [Fact]
    public Task TestNoWrappingSuggestions()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59624")]
    public Task TestNoWrappingSuggestions_TrailingComma()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1,];
                }
            }
            """);

    [Fact]
    public Task TestWrappingShortInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1, 2];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1,
                        2
                    ];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1, 2
                    ];
                }
            }
            """);

    [Fact]
    public Task TestSpreads1()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1, 2, .. c];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1,
                        2,
                        .. c
                    ];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1, 2, .. c
                    ];
                }
            }
            """);

    [Fact]
    public Task TestSpreads2()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1, 2, .. c,];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1,
                        2,
                        .. c,
                    ];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1, 2, .. c,
                    ];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59624")]
    public Task TestWrappingShortInitializerExpression_TrailingComma1()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1, 2,];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1,
                        2,
                    ];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1, 2,
                    ];
                }
            }
            """);

    [Fact]
    public Task TestWrappingLongInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||]["the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        "the",
                        "quick",
                        "brown",
                        "fox",
                        "jumps",
                        "over",
                        "the",
                        "lazy",
                        "dog"
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"
                    ];
                 }
            }
            """);

    [Fact]
    public Task TestWrappingMultiLineLongInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||]["the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog", "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        "the",
                        "quick",
                        "brown",
                        "fox",
                        "jumps",
                        "over",
                        "the",
                        "lazy",
                        "dog",
                        "the",
                        "quick",
                        "brown",
                        "fox",
                        "jumps",
                        "over",
                        "the",
                        "lazy",
                        "dog"
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog", "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog", "the", "quick", "brown", "fox",
                        "jumps", "over", "the", "lazy", "dog"
                    ];
                 }
            }
            """);

    [Fact]
    public Task TestShortInitializerExpressionRefactorings()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test =
                    [||][
                        1,
                        2
                    ];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test = [1, 2];
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        1, 2
                    ];
                }
            }
            """);

    [Fact]
    public Task TestLongInitializerExpressionRefactorings()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test =
                    [||][
                        "the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        "the",
                        "quick",
                        "brown",
                        "fox",
                        "jumps",
                        "over",
                        "the",
                        "lazy",
                        "dog"
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test = ["the", "quick", "brown", "fox", "jumps", "over", "the", "lazy", "dog"];
                 }
            }
            """);

    [Fact]
    public Task TestObjectWrappingInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test = [||][new A { B = 1, C = 1 }, new A { B = 2, C = 2 }, new A { B = 3, C = 3 }];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        new A { B = 1, C = 1 },
                        new A { B = 2, C = 2 },
                        new A { B = 3, C = 3 }
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        new A { B = 1, C = 1 }, new A { B = 2, C = 2 }, new A { B = 3, C = 3 }
                    ];
                 }
            }
            """);

    [Fact]
    public Task TestWrappedObjectInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var test =
                    [||][
                        new A { B = 1, C = 1 },
                        new A { B = 2, C = 2 },
                        new A { B = 3, C = 3 }
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test = [new A { B = 1, C = 1 }, new A { B = 2, C = 2 }, new A { B = 3, C = 3 }];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    var test =
                    [
                        new A { B = 1, C = 1 }, new A { B = 2, C = 2 }, new A { B = 3, C = 3 }
                    ];
                 }
            }
            """);

    [Fact]
    public Task TestReturnInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    return [||][0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    return
                    [
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        9
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    return
                    [
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                    ];
                 }
            }
            """);

    [Fact]
    public Task TestWrappedReturnInitializerExpression()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    return
                    [||][
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        7,
                        8,
                        9
                    ];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    return [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
                 }
            }
            """,
            """
            class C {
                void Bar() {
                    return
                    [
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                    ];
                 }
            }
            """);

    [Fact]
    public Task TestClassPropertyInitializerExpressionRefactorings()
        => TestAllWrappingCasesAsync(
            """
            public class C {
                public List<int> B => [||][0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            }
            """,
            """
            public class C {
                public List<int> B =>
                [
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9
                ];
            }
            """,
            """
            public class C {
                public List<int> B =>
                [
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                ];
            }
            """);

    [Fact]
    public Task TestWrappedClassPropertyInitializerExpressionRefactorings()
        => TestAllWrappingCasesAsync(
            """
            public class C {
                public List<int> B =>
                [||][
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9
                ];
            }
            """,
            """
            public class C {
                public List<int> B => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
            }
            """,
            """
            public class C {
                public List<int> B =>
                [
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                ];
            }
            """);

    [Fact]
    public Task TestArgumentInitializerExpressionRefactorings()
        => TestAllWrappingCasesAsync(
            """
            public void F() {
                var result = fakefunction([||][0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
            }
            """,
            """
            public void F() {
                var result = fakefunction(
                [
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9
                ]);
            }
            """,
            """
            public void F() {
                var result = fakefunction(
                [
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                ]);
            }
            """);

    [Fact]
    public Task TestWrappedArgumentInitializerExpressionRefactorings()
        => TestAllWrappingCasesAsync(
            """
            public void F() {
                var result = fakefunction(
                [||][
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9
                ]);
            }
            """,
            """
            public void F() {
                var result = fakefunction([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
            }
            """,
            """
            public void F() {
                var result = fakefunction(
                [
                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9
                ]);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingStartToken()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    var test = [||] 1, 2];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingEndToken1()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1, 2
                    return;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingEndToken2()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    var test = [||][1, 2 ;
                }
            }
            """);
}
