// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ConditionalExpressionInStringInterpolation
{
    public class CSharpAddParenthesisAroundConditionalExpressionInInterpolatedStringCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddParenthesisAroundConditionalExpressionInInterpolatedStringCodeFixProvider());

        private async Task TestInMethodAsync(string initialMethodBody, string expectedMethodBody)
        {
            var template = @"
class Application
{{
    public static M()
    {{
        {0}
    }}
}}";
            await TestInRegularAndScriptAsync(
                string.Format(template, initialMethodBody), 
                string.Format(template, expectedMethodBody)).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisSimpleConditionalExpression()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? 1 [|:|] 2}"";",
                @"var s = $""{ (true ? 1 : 2)}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisMultiLineConditionalExpression1()
        {
            await TestInMethodAsync(@"
var s = $@""{ true
            [|? 1|]
            : 2}"";
", @"
var s = $@""{ (true
            ? 1
            : 2)}"";
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisMultiLineConditionalExpression2()
        {
            await TestInMethodAsync(@"
var s = $@""{
            true
            ?
            [|1|]
            : 
            2
            }"";
", @"
var s = $@""{
            (true
            ?
            1
            : 
            2
)            }"";
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWithTrivia()
        {
            await TestInMethodAsync(
                @"var s = $""{ /* Leading1 */ true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */[|:|] /* FalsePart1 */ 2 /* FalsePart2 */ }"";",
                @"var s = $""{ /* Leading1 */ (true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */: /* FalsePart1 */ 2 /* FalsePart2 */ )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisClosingBracketInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? new int[0] [|:|] new int[] {} }"";",
                @"var s = $""{ (true ? new int[0] : new int[] {})}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisStringLiteralInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2"" }"";",
                @"var s = $""{ (true ? ""1"" : ""2"")}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisVerbatimStringLiteralInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] @""""""2"""""" }"";",
                @"var s = $""{ (true ? ""1"" : @""""""2"""""")}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisStringLiteralInFalseConditionWithClosingParenthesisInLiteral()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2)"" }"";",
                @"var s = $""{ (true ? ""1"" : ""2)"")}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisStringLiteralInFalseConditionWithEscapedDoubleQuotes()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2\"""" }"";",
                @"var s = $""{ (true ? ""1"" : ""2\"""")}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisStringLiteralInFalseConditionWithCodeLikeContent()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""M(new int[] {}, \""Parameter\"");"" }"";",
                @"var s = $""{ (true ? ""1"" : ""M(new int[] {}, \""Parameter\"");"")}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisNestedConditionalExpression1()
        {
            await TestInMethodAsync(
                @"var s2 = $""{ true ? ""1"" [|:|] (false ? ""2"" : ""3"") };",
                @"var s2 = $""{ (true ? ""1"" : (false ? ""2"" : ""3""))};");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisNestedConditionalExpression2()
        {
            await TestInMethodAsync(
                @"var s2 = $""{ true ? ""1"" [|:|] false ? ""2"" : ""3"" };",
                @"var s2 = $""{ (true ? ""1"" : false ? ""2"" : ""3"")};");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisNestedConditionalWithNestedInterpolatedString()
        {
            await TestInMethodAsync(
                @"var s2 = $""{ (true ? ""1"" : false ? $""{ true ? ""2"" [|:|] ""3""}"" : ""4"") }""",
                @"var s2 = $""{ (true ? ""1"" : false ? $""{ (true ? ""2"" : ""3"")}"" : ""4"") }""");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisMultipleInterpolatedSections1()
        {
            await TestInMethodAsync(
                @"var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3""} Text4 { (true ? ""Text5"" : ""Text6"")} Text7"";",
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { (true ? ""Text5"" : ""Text6"")} Text7"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisMultipleInterpolatedSections2()
        {
            await TestInMethodAsync(
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { true ? ""Text5"" [|:|] ""Text6""} Text7"";",
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { (true ? ""Text5"" : ""Text6"")} Text7"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisMultipleInterpolatedSections3()
        {
            await TestInMethodAsync(
                @"var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3""} Text4 { true ? ""Text5"" : ""Text6""} Text7"";",
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { true ? ""Text5"" : ""Text6""} Text7"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWhileTyping1()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|]
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" :)
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWhileTyping2()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : "")
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWhileTyping3()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3)
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWhileTyping4()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3""
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWhileTyping5()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")}
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWithCS1026PresentBeforeFixIsApplied1()
        {
            await TestInMethodAsync(
                @"
                (
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode();",
                @"
                (
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")}
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWithCS1026PresentBeforeFixIsApplied2()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode(",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")}
                NextLineOfCode(");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisWithCS1026PresentBeforeFixIsApplied3()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = ($""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = ($""Text1 { (true ? ""Text2"" : ""Text3"")}
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesisAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesisAddOpeningParenthesisOnly()
        {
            await TestInMethodAsync(
                @"var s3 = $""{ true ? 1 [|:|] 2 )}""",
                @"var s3 = $""{ (true ? 1 : 2 )}""");
        }
    }
}
