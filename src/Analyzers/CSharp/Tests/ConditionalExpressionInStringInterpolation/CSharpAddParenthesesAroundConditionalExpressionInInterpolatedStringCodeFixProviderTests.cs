// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConditionalExpressionInStringInterpolation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConditionalExpressionInStringInterpolation;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
public sealed class CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProviderTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider());

    private async Task TestInMethodAsync(string initialMethodBody, string expectedMethodBody)
    {
        var template = """
            class Application
            {{
                public void M()
                {{
                    {0}
                }}
            }}
            """;
        await TestInRegularAndScriptAsync(
            string.Format(template, initialMethodBody),
            string.Format(template, expectedMethodBody)).ConfigureAwait(false);
    }

    [Fact]
    public Task TestAddParenthesesSimpleConditionalExpression()
        => TestInMethodAsync(
            """var s = $"{ true ? 1 [|:|] 2}";""",
            """var s = $"{ (true ? 1 : 2)}";""");

    [Fact]
    public Task TestAddParenthesesMultiLineConditionalExpression1()
        => TestInMethodAsync("""
            var s = $@"{ true
                        [|? 1|]
                        : 2}";
            """, """
            var s = $@"{ (true
                        ? 1
                        : 2)}";
            """);

    [Fact]
    public Task TestAddParenthesesMultiLineConditionalExpression2()
        => TestInMethodAsync("""
            var s = $@"{
                        true
                        ?
                        [|1|]
                        : 
                        2
                        }";
            """, """
            var s = $@"{
                        (true
                        ?
                        1
                        : 
                        2
            )            }";
            """);

    [Fact]
    public Task TestAddParenthesesWithTrivia()
        => TestInMethodAsync(
            """var s = $"{ /* Leading1 */ true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */[|:|] /* FalsePart1 */ 2 /* FalsePart2 */ }";""",
            """var s = $"{ /* Leading1 */ (true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */: /* FalsePart1 */ 2) /* FalsePart2 */ }";""");

    [Fact]
    public Task TestAddParenthesesClosingBracketInFalseCondition()
        => TestInMethodAsync(
            """var s = $"{ true ? new int[0] [|:|] new int[] {} }";""",
            """var s = $"{ (true ? new int[0] : new int[] {}) }";""");

    [Fact]
    public Task TestAddParenthesesStringLiteralInFalseCondition()
        => TestInMethodAsync(
            """var s = $"{ true ? "1" [|:|] "2" }";""",
            """var s = $"{ (true ? "1" : "2") }";""");

    [Fact]
    public Task TestAddParenthesesVerbatimStringLiteralInFalseCondition()
        => TestInMethodAsync(
            """"var s = $"{ true ? "1" [|:|] @"""2""" }";"""",
            """"var s = $"{ (true ? "1" : @"""2""") }";"""");

    [Fact]
    public Task TestAddParenthesesStringLiteralInFalseConditionWithClosingParenthesisInLiteral()
        => TestInMethodAsync(
            """var s = $"{ true ? "1" [|:|] "2)" }";""",
            """var s = $"{ (true ? "1" : "2)") }";""");

    [Fact]
    public Task TestAddParenthesesStringLiteralInFalseConditionWithEscapedDoubleQuotes()
        => TestInMethodAsync(
            """var s = $"{ true ? "1" [|:|] "2\"" }";""",
            """var s = $"{ (true ? "1" : "2\"") }";""");

    [Fact]
    public Task TestAddParenthesesStringLiteralInFalseConditionWithCodeLikeContent()
        => TestInMethodAsync(
            """var s = $"{ true ? "1" [|:|] "M(new int[] {}, \"Parameter\");" }";""",
            """var s = $"{ (true ? "1" : "M(new int[] {}, \"Parameter\");") }";""");

    [Fact]
    public Task TestAddParenthesesNestedConditionalExpression1()
        => TestInMethodAsync(
            """var s2 = $"{ true ? "1" [|:|] (false ? "2" : "3") };""",
            """var s2 = $"{ (true ? "1" : (false ? "2" : "3")) };""");

    [Fact]
    public Task TestAddParenthesesNestedConditionalExpression2()
        => TestInMethodAsync(
            """var s2 = $"{ true ? "1" [|:|] false ? "2" : "3" };""",
            """var s2 = $"{ (true ? "1" : false ? "2" : "3") };""");

    [Fact]
    public Task TestAddParenthesesNestedConditionalWithNestedInterpolatedString()
        => TestInMethodAsync(
            """
            var s2 = $"{ (true ? "1" : false ? $"{ true ? "2" [|:|] "3"}" : "4") }"
            """,
            """
            var s2 = $"{ (true ? "1" : false ? $"{ (true ? "2" : "3")}" : "4") }"
            """);

    [Fact]
    public Task TestAddParenthesesMultipleInterpolatedSections1()
        => TestInMethodAsync(
            """var s3 = $"Text1 { true ? "Text2" [|:|] "Text3"} Text4 { (true ? "Text5" : "Text6")} Text7";""",
            """var s3 = $"Text1 { (true ? "Text2" : "Text3")} Text4 { (true ? "Text5" : "Text6")} Text7";""");

    [Fact]
    public Task TestAddParenthesesMultipleInterpolatedSections2()
        => TestInMethodAsync(
            """var s3 = $"Text1 { (true ? "Text2" : "Text3")} Text4 { true ? "Text5" [|:|] "Text6"} Text7";""",
            """var s3 = $"Text1 { (true ? "Text2" : "Text3")} Text4 { (true ? "Text5" : "Text6")} Text7";""");

    [Fact]
    public Task TestAddParenthesesMultipleInterpolatedSections3()
        => TestInMethodAsync(
            """var s3 = $"Text1 { true ? "Text2" [|:|] "Text3"} Text4 { true ? "Text5" : "Text6"} Text7";""",
            """var s3 = $"Text1 { (true ? "Text2" : "Text3")} Text4 { true ? "Text5" : "Text6"} Text7";""");

    [Fact]
    public Task TestAddParenthesesWhileTyping1()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { true ? "Text2" [|:|]
            NextLineOfCode();
            """,
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { (true ? "Text2" :)
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesWhileTyping2()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { true ? "Text2" [|:|] "
            NextLineOfCode();
            """,
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { (true ? "Text2" : ")
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesWhileTyping3()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { true ? "Text2" [|:|] "Text3
            NextLineOfCode();
            """,
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { (true ? "Text2" : "Text3)
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesWhileTyping4()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { true ? "Text2" [|:|] "Text3"
            NextLineOfCode();
            """,
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { (true ? "Text2" : "Text3")
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesWhileTyping5()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { true ? "Text2" [|:|] "Text3" }
            NextLineOfCode();
            """,
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { (true ? "Text2" : "Text3") }
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesWithCS1026PresentBeforeFixIsApplied1()
        => TestInMethodAsync(
            """
            (
            var s3 = $"Text1 { true ? "Text2" [|:|] "Text3" }
            NextLineOfCode();
            """,
            """
            (
            var s3 = $"Text1 { (true ? "Text2" : "Text3") }
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesWithCS1026PresentBeforeFixIsApplied2()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { true ? "Text2" [|:|] "Text3" }
            NextLineOfCode(
            """,
            """
            PreviousLineOfCode();
            var s3 = $"Text1 { (true ? "Text2" : "Text3") }
            NextLineOfCode(
            """);

    [Fact]
    public Task TestAddParenthesesWithCS1026PresentBeforeFixIsApplied3()
        => TestInMethodAsync(
            """
            PreviousLineOfCode();
            var s3 = ($"Text1 { true ? "Text2" [|:|] "Text3" }
            NextLineOfCode();
            """,
            """
            PreviousLineOfCode();
            var s3 = ($"Text1 { (true ? "Text2" : "Text3") }
            NextLineOfCode();
            """);

    [Fact]
    public Task TestAddParenthesesAddOpeningParenthesisOnly()
        => TestInMethodAsync(
            """
            var s3 = $"{ true ? 1 [|:|] 2 )}"
            """,
            """
            var s3 = $"{ (true ? 1 : 2 )}"
            """);
}
