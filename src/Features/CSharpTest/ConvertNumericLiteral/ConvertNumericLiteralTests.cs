// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertNumericLiteral;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertNumericLiteral;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertNumericLiteral)]
public sealed class ConvertNumericLiteralTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertNumericLiteralCodeRefactoringProvider();

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    private enum Refactoring { ChangeBase1, ChangeBase2, AddOrRemoveDigitSeparators }

    private async Task TestMissingOneAsync(string initial)
        => await TestMissingInRegularAndScriptAsync(CreateTreeText("[||]" + initial));

    private async Task TestFixOneAsync(string initial, string expected, Refactoring refactoring)
        => await TestInRegularAndScript1Async(CreateTreeText("[||]" + initial), CreateTreeText(expected), (int)refactoring);

    private static string CreateTreeText(string initial)
        => @"class X { void F() { var x = " + initial + @"; } }";

    [Fact]
    public async Task TestRemoveDigitSeparators()
        => await TestFixOneAsync("0b1_0_01UL", "0b1001UL", Refactoring.AddOrRemoveDigitSeparators);

    [Fact]
    public async Task TestConvertToBinary()
        => await TestFixOneAsync("5", "0b101", Refactoring.ChangeBase1);

    [Fact]
    public async Task TestConvertToDecimal()
        => await TestFixOneAsync("0b101", "5", Refactoring.ChangeBase1);

    [Fact]
    public async Task TestConvertToHex()
        => await TestFixOneAsync("10", "0xA", Refactoring.ChangeBase2);

    [Fact]
    public async Task TestSeparateThousands()
        => await TestFixOneAsync("100000000", "100_000_000", Refactoring.AddOrRemoveDigitSeparators);

    [Fact]
    public async Task TestSeparateWords()
        => await TestFixOneAsync("0x1111abcd1111", "0x1111_abcd_1111", Refactoring.AddOrRemoveDigitSeparators);

    [Fact]
    public async Task TestSeparateNibbles()
        => await TestFixOneAsync("0b10101010", "0b1010_1010", Refactoring.AddOrRemoveDigitSeparators);

    [Fact]
    public async Task TestMissingOnFloatingPoint()
        => await TestMissingOneAsync("1.1");

    [Fact]
    public async Task TestMissingOnScientificNotation()
        => await TestMissingOneAsync("1e5");

    [Fact]
    public async Task TestConvertToDecimal_02()
        => await TestFixOneAsync("0x1e5", "485", Refactoring.ChangeBase1);

    [Fact]
    public async Task TestTypeCharacter()
        => await TestFixOneAsync("0x1e5UL", "0b111100101UL", Refactoring.ChangeBase2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19225")]
    public async Task TestPreserveWhitespaces()
    {
        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                void M()
                {
                    var numbers = new int[] {
                        [||]0x1, 0x2
                    };
                }
            }
            """,
            """
            class Program
            {
                void M()
                {
                    var numbers = new int[] {
                        0b1, 0x2
                    };
                }
            }
            """, index: (int)Refactoring.ChangeBase2);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19369")]
    public async Task TestCaretPositionAtTheEnd()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                int a = 42[||];
            }
            """,
            """
            class C
            {
                int a = 0b101010;
            }
            """, index: (int)Refactoring.ChangeBase1);
    }

    [Fact]
    public async Task TestSelectionMatchesToken()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                int a = [|42|];
            }
            """,
            """
            class C
            {
                int a = 0b101010;
            }
            """, index: (int)Refactoring.ChangeBase1);
    }

    [Fact]
    public async Task TestSelectionDoesNotMatchToken()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int a = [|42 * 2|];
            }
            """);
    }
}
