// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertNumericLiteral
{
    public class ConvertNumericLiteralTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertNumericLiteralCodeRefactoringProvider();

        private enum Refactoring { ChangeBase1, ChangeBase2, AddOrRemoveDigitSeparators }

        private async Task TestMissingOneAsync(string initial)
        {
            await TestMissingInRegularAndScriptAsync(CreateTreeText("[||]" + initial));
        }

        private async Task TestFixOneAsync(string initial, string expected, Refactoring refactoring)
        {
            await TestInRegularAndScriptAsync(CreateTreeText("[||]" + initial), CreateTreeText(expected), index: (int)refactoring);
        }

        private static string CreateTreeText(string initial)
        {
            return @"class X { void F() { var x = " + initial + @"; } }";
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestRemoveDigitSeparators()
        {
            await TestFixOneAsync("0b1_0_01UL", "0b1001UL", Refactoring.AddOrRemoveDigitSeparators);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestConvertToBinary()
        {
            await TestFixOneAsync("5", "0b101", Refactoring.ChangeBase1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestConvertToDecimal()
        {
            await TestFixOneAsync("0b101", "5", Refactoring.ChangeBase1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestConvertToHex()
        {
            await TestFixOneAsync("10", "0xA", Refactoring.ChangeBase2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestSeparateThousands()
        {
            await TestFixOneAsync("100000000", "100_000_000", Refactoring.AddOrRemoveDigitSeparators);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestSeparateWords()
        {
            await TestFixOneAsync("0x1111abcd1111", "0x1111_abcd_1111", Refactoring.AddOrRemoveDigitSeparators);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestSeparateNibbles()
        {
            await TestFixOneAsync("0b10101010", "0b1010_1010", Refactoring.AddOrRemoveDigitSeparators);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestMissingOnFloatingPoint()
        {
            await TestMissingOneAsync("1.1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestMissingOnScientificNotation()
        {
            await TestMissingOneAsync("1e5");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestConvertToDecimal_02()
        {
            await TestFixOneAsync("0x1e5", "485", Refactoring.ChangeBase1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestTypeCharacter()
        {
            await TestFixOneAsync("0x1e5UL", "0b111100101UL", Refactoring.ChangeBase2);
        }

        [WorkItem(19225, "https://github.com/dotnet/roslyn/issues/19225")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestPreserveWhitespaces()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void M()
    {
        var numbers = new int[] {
            [||]0x1, 0x2
        };
    }
}",
@"class Program
{
    void M()
    {
        var numbers = new int[] {
            0b1, 0x2
        };
    }
}", index: (int)Refactoring.ChangeBase2);
        }

        [WorkItem(19369, "https://github.com/dotnet/roslyn/issues/19369")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestCaretPositionAtTheEnd()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int a = 42[||];
}",
@"class C
{
    int a = 0b101010;
}", index: (int)Refactoring.ChangeBase1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestSelectionMatchesToken()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int a = [|42|];
}",
@"class C
{
    int a = 0b101010;
}", index: (int)Refactoring.ChangeBase1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
        public async Task TestSelectionDoesntMatchToken()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int a = [|42 * 2|];
}");
        }
    }
}
