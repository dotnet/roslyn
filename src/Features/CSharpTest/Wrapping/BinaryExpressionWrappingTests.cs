// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

[Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
public sealed class BinaryExpressionWrappingTests : AbstractWrappingTests
{
    private TestParameters EndOfLine
        => new(options: Option(CodeStyleOptions2.OperatorPlacementWhenWrapping, OperatorPlacementWhenWrappingPreference.EndOfLine));

    private TestParameters BeginningOfLine
        => new(options: Option(CodeStyleOptions2.OperatorPlacementWhenWrapping, OperatorPlacementWhenWrappingPreference.BeginningOfLine));

    private Task TestEndOfLine(string markup, string expected)
        => TestInRegularAndScriptAsync(markup, expected, EndOfLine);

    private Task TestBeginningOfLine(string markup, string expected)
        => TestInRegularAndScriptAsync(markup, expected, BeginningOfLine);

    [Fact]
    public Task TestMissingWithSyntaxError()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && (j && )
                }
            }
            """);

    [Fact]
    public Task TestMissingWithSelection()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    if ([|i|] && j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingBeforeExpr()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    [||]if (i && j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithSingleExpr()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    if ([||]i) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMultiLineExpression()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && (j +
                        k)) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMultiLineExpr2()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && @"
                    ") {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInIf()
        => TestEndOfLine(
            """
            class C {
                void Bar() {
                    if ([||]i && j) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i &&
                        j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInIf_IncludingOp()
        => TestBeginningOfLine(
            """
            class C {
                void Bar() {
                    if ([||]i && j) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i
                        && j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInIf2()
        => TestEndOfLine(
            """
            class C {
                void Bar() {
                    if (i[||] && j) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i &&
                        j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInIf3()
        => TestEndOfLine(
            """
            class C {
                void Bar() {
                    if (i [||]&& j) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i &&
                        j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInIf4()
        => TestEndOfLine(
            """
            class C {
                void Bar() {
                    if (i &&[||] j) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i &&
                        j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInIf5()
        => TestEndOfLine(
            """
            class C {
                void Bar() {
                    if (i && [||]j) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i &&
                        j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestTwoExprWrappingCases_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && j) {
                    }
                }
            }
            """,
            EndOfLine,
            """
            class C {
                void Bar() {
                    if (i &&
                        j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestTwoExprWrappingCases_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && j) {
                    }
                }
            }
            """,
            BeginningOfLine,
            """
            class C {
                void Bar() {
                    if (i
                        && j) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestThreeExprWrappingCases_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && j || k) {
                    }
                }
            }
            """,
            EndOfLine,
            """
            class C {
                void Bar() {
                    if (i &&
                        j ||
                        k) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestThreeExprWrappingCases_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]i && j || k) {
                    }
                }
            }
            """,
            BeginningOfLine,
            """
            class C {
                void Bar() {
                    if (i
                        && j
                        || k) {
                    }
                }
            }
            """);

    [Fact]
    public Task Test_AllOptions_NoInitialMatches_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if (
                        [||]i   &&
                            j
                             ||   k) {
                    }
                }
            }
            """,
            EndOfLine,
            """
            class C {
                void Bar() {
                    if (
                        i &&
                        j ||
                        k) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (
                        i && j || k) {
                    }
                }
            }
            """);

    [Fact]
    public Task Test_AllOptions_NoInitialMatches_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if (
                        [||]i   &&
                            j
                             ||   k) {
                    }
                }
            }
            """,
            BeginningOfLine,
            """
            class C {
                void Bar() {
                    if (
                        i
                        && j
                        || k) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (
                        i && j || k) {
                    }
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption1()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]a &&
                        b) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (a
                        && b) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (a && b) {
                    }
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption2_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]a
                        && b) {
                    }
                }
            }
            """,
            EndOfLine,
            """
            class C {
                void Bar() {
                    if (a &&
                        b) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (a && b) {
                    }
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption2_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    if ([||]a
                        && b) {
                    }
                }
            }
            """,
            BeginningOfLine,
            """
            class C {
                void Bar() {
                    if (a && b) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInLocalInitializer_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo() {
                    var v = [||]a && b && c;
                }
            }
            """,
            BeginningOfLine,
            """
            class C {
                void Goo() {
                    var v = a
                        && b
                        && c;
                }
            }
            """,
            """
            class C {
                void Goo() {
                    var v = a
                            && b
                            && c;
                }
            }
            """);

    [Fact]
    public Task TestInLocalInitializer_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo() {
                    var v = [||]a && b && c;
                }
            }
            """,
            EndOfLine,
            """
            class C {
                void Goo() {
                    var v = a &&
                        b &&
                        c;
                }
            }
            """,
            """
            class C {
                void Goo() {
                    var v = a &&
                            b &&
                            c;
                }
            }
            """);

    [Fact]
    public Task TestInField_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                bool v = [||]a && b && c;
            }
            """,
            BeginningOfLine,
            """
            class C {
                bool v = a
                    && b
                    && c;
            }
            """,
            """
            class C {
                bool v = a
                         && b
                         && c;
            }
            """);

    [Fact]
    public Task TestInField_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                bool v = [||]a && b && c;
            }
            """,
            EndOfLine,
            """
            class C {
                bool v = a &&
                    b &&
                    c;
            }
            """,
            """
            class C {
                bool v = a &&
                         b &&
                         c;
            }
            """);

    [Fact]
    public Task TestAddition_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var goo = [||]"now" + "is" + "the" + "time";
                }
            }
            """,
            EndOfLine,
            """
            class C {
                void Bar() {
                    var goo = "now" +
                        "is" +
                        "the" +
                        "time";
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var goo = "now" +
                              "is" +
                              "the" +
                              "time";
                }
            }
            """);

    [Fact]
    public Task TestAddition_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    var goo = [||]"now" + "is" + "the" + "time";
                }
            }
            """,
            BeginningOfLine,
            """
            class C {
                void Bar() {
                    var goo = "now"
                        + "is"
                        + "the"
                        + "time";
                }
            }
            """,
            """
            class C {
                void Bar() {
                    var goo = "now"
                              + "is"
                              + "the"
                              + "time";
                }
            }
            """);

    [Fact]
    public Task TestUnderscoreName_End()
        => TestEndOfLine(
            """
            class C {
                void Bar() {
                    if ([||]i is var _ && _ != null) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i is var _ &&
                        _ != null) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestUnderscoreName_Beginning()
        => TestBeginningOfLine(
            """
            class C {
                void Bar() {
                    if ([||]i is var _ && _ != null) {
                    }
                }
            }
            """,
            """
            class C {
                void Bar() {
                    if (i is var _
                        && _ != null) {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInField_Already_Wrapped_Beginning()
        => TestAllWrappingCasesAsync(
            """
            class C {
                bool v =
                    [||]a && b && c;
            }
            """,
            BeginningOfLine,
            """
            class C {
                bool v =
                    a
                    && b
                    && c;
            }
            """);

    [Fact]
    public Task TestInField_Already_Wrapped_End()
        => TestAllWrappingCasesAsync(
            """
            class C {
                bool v =
                    [||]a && b && c;
            }
            """,
            EndOfLine,
            """
            class C {
                bool v =
                    a &&
                    b &&
                    c;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34127")]
    public Task TestWrapLowerPrecedenceInLargeBinary()
        => TestAllWrappingCasesAsync(
            """
            class C
            {
                bool v = [||]a + b + c + d == x * y * z;
            }
            """,
            EndOfLine,
            """
            class C
            {
                bool v = a + b + c + d ==
                    x * y * z;
            }
            """,
            """
            class C
            {
                bool v = a + b + c + d ==
                         x * y * z;
            }
            """);
}
