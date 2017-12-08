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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesis)]
        public async Task TestAddParenthesisSimpleConditionalExpression()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? 1 [|:|] 2}"";",
                @"var s = $""{ (true ? 1 : 2)}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesis)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesis)]
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
            2)
            }"";
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesis)]
        public async Task TestAddParenthesisWithTrivia()
        {
            await TestInMethodAsync(
                @"var s = $""{ /* Leading1 */ true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */[|:|] /* FalsePart1 */ 2 /* FalsePart2 */ }"";",
                @"var s = $""{ /* Leading1 */ (true /* Leading2 */ ? /* TruePart1 */ 1 /* TruePart2 */: /* FalsePart1 */ 2 /* FalsePart2 */ )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesis)]
        public async Task TestAddParenthesis_ClosingBracketInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? new int[0] [|:|] new int[] {} }"";",
                @"var s = $""{ (true ? new int[0] : new int[] {} )}"";");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParenthesis)]
        public async Task TestAddParenthesis_StringLiteralInFalseCondition()
        {
            await TestInMethodAsync(
                @"var s = $""{ true ? ""1"" [|:|] ""2"" }"";",
                @"var s = $""{ (true ? ""1"" : ""2"" )}"";");
        }
    }
}
