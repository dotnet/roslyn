// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

[Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
public sealed class ChainedExpressionWrappingTests : AbstractWrappingTests
{
    [Fact]
    public Task TestMissingWithSyntaxError()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    [||]the.quick().brown.fox(,);
                }
            }
            """);

    [Fact]
    public Task TestMissingWithoutEnoughChunks()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    [||]the.quick();
                }
            }
            """);

    [Fact]
    public Task TestWithEnoughChunks()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestGenericNames()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestElementAccess()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestUnwrap()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestWrapAndUnwrap()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestChunkMustHaveDottedSection()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TrailingNonCallIsNotWrapped()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TrailingLongWrapping1()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TrailingLongWrapping2()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TrailingLongWrapping3()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestInConditionalAccess()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestInConditionalAccess2()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestInConditionalAccess3()
        => TestAllWrappingCasesAsync(
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

    [Fact]
    public Task TestInConditionalAccess4()
        => TestAllWrappingCasesAsync(
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
