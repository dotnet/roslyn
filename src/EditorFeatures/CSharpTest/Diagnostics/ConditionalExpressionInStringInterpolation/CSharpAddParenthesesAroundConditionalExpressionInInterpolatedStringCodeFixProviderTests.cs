﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ConditionalExpressionInStringInterpolation
{
    public class CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProviderTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddParenthesesAroundConditionalExpressionInInterpolatedStringCodeFixProvider());

        private async Task TestInMethodAsync(string initialMethodBody, string expectedMethodBody)
        {
            var template = @"
class Application
{{
    public void M()
    {{
        {0}
    }}
}}";
            await TestInRegularAndScriptAsync(
                string.Format(template, initialMethodBody),
                string.Format(template, expectedMethodBody)).ConfigureAwait(false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesSimpleConditionalExpression()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? 1 [|:|] 2}"";",
                @"var s = $""{ (true ? 1 : 2)}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesMultiLineConditionalExpression1()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesMultiLineConditionalExpression2()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWithTrivia()
        {
            await TestInMethodAsync(
                @"var s = $""{ /* Leading1 */ true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */[|:|] /* FalsePart1 */ 2 /* FalsePart2 */ }"";",
                @"var s = $""{ /* Leading1 */ (true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */: /* FalsePart1 */ 2 /* FalsePart2 */ )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesClosingBracketInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? new int[0] [|:|] new int[] {} }"";",
                @"var s = $""{ (true ? new int[0] : new int[] {} )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesStringLiteralInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2"" }"";",
                @"var s = $""{ (true ? ""1"" : ""2"" )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesVerbatimStringLiteralInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] @""""""2"""""" }"";",
                @"var s = $""{ (true ? ""1"" : @""""""2"""""" )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesStringLiteralInFalseConditionWithClosingParenthesisInLiteral()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2)"" }"";",
                @"var s = $""{ (true ? ""1"" : ""2)"" )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesStringLiteralInFalseConditionWithEscapedDoubleQuotes()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2\"""" }"";",
                @"var s = $""{ (true ? ""1"" : ""2\"""" )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesStringLiteralInFalseConditionWithCodeLikeContent()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""M(new int[] {}, \""Parameter\"");"" }"";",
                @"var s = $""{ (true ? ""1"" : ""M(new int[] {}, \""Parameter\"");"" )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesNestedConditionalExpression1()
        {
            await TestInMethodAsync(
                @"var s2 = $""{ true ? ""1"" [|:|] (false ? ""2"" : ""3"") };",
                @"var s2 = $""{ (true ? ""1"" : (false ? ""2"" : ""3"") )};");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesNestedConditionalExpression2()
        {
            await TestInMethodAsync(
                @"var s2 = $""{ true ? ""1"" [|:|] false ? ""2"" : ""3"" };",
                @"var s2 = $""{ (true ? ""1"" : false ? ""2"" : ""3"" )};");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesNestedConditionalWithNestedInterpolatedString()
        {
            await TestInMethodAsync(
                @"var s2 = $""{ (true ? ""1"" : false ? $""{ true ? ""2"" [|:|] ""3""}"" : ""4"") }""",
                @"var s2 = $""{ (true ? ""1"" : false ? $""{ (true ? ""2"" : ""3"")}"" : ""4"") }""");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesMultipleInterpolatedSections1()
        {
            await TestInMethodAsync(
                @"var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3""} Text4 { (true ? ""Text5"" : ""Text6"")} Text7"";",
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { (true ? ""Text5"" : ""Text6"")} Text7"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesMultipleInterpolatedSections2()
        {
            await TestInMethodAsync(
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { true ? ""Text5"" [|:|] ""Text6""} Text7"";",
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { (true ? ""Text5"" : ""Text6"")} Text7"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesMultipleInterpolatedSections3()
        {
            await TestInMethodAsync(
                @"var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3""} Text4 { true ? ""Text5"" : ""Text6""} Text7"";",
                @"var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"")} Text4 { true ? ""Text5"" : ""Text6""} Text7"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWhileTyping1()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWhileTyping2()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWhileTyping3()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWhileTyping4()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWhileTyping5()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"" )}
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWithCS1026PresentBeforeFixIsApplied1()
        {
            await TestInMethodAsync(
                @"
                (
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode();",
                @"
                (
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"" )}
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWithCS1026PresentBeforeFixIsApplied2()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode(",
                @"
                PreviousLineOfCode();
                var s3 = $""Text1 { (true ? ""Text2"" : ""Text3"" )}
                NextLineOfCode(");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesWithCS1026PresentBeforeFixIsApplied3()
        {
            await TestInMethodAsync(
                @"
                PreviousLineOfCode();
                var s3 = ($""Text1 { true ? ""Text2"" [|:|] ""Text3"" }
                NextLineOfCode();",
                @"
                PreviousLineOfCode();
                var s3 = ($""Text1 { (true ? ""Text2"" : ""Text3"" )}
                NextLineOfCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesesAroundConditionalExpressionInInterpolatedString)]
        public async Task TestAddParenthesesAddOpeningParenthesisOnly()
        {
            await TestInMethodAsync(
                @"var s3 = $""{ true ? 1 [|:|] 2 )}""",
                @"var s3 = $""{ (true ? 1 : 2 )}""");
        }
    }
}
