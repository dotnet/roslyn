// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

[Trait(Traits.Feature, Traits.Features.CodeActionsMergeNestedIfStatements)]
public sealed partial class MergeNestedIfStatementsTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpMergeNestedIfStatementsCodeRefactoringProvider();

    [Theory]
    [InlineData("[||]if (b)")]
    [InlineData("i[||]f (b)")]
    [InlineData("if[||] (b)")]
    [InlineData("if [||](b)")]
    [InlineData("if (b)[||]")]
    [InlineData("[|if|] (b)")]
    [InlineData("[|if (b)|]")]
    public async Task MergedOnNestedIfSpans(string ifLine)
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            " + ifLine + @"
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedOnNestedIfExtendedHeaderSelection()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
[|            if (b)
|]            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedOnNestedIfFullSelection()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
[|            if (b)
            {
            }
|]        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedOnNestedIfFullSelectionWithElseClause()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [|if (b)
            {
            }
            else
            {
            }|]
        }
        else
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
        }
        else
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnNestedIfFullSelectionWithoutElseClause()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [|if (b)
            {
            }|]
            else
            {
            }
        }
        else
        {
        }
    }
}");
    }

    [Theory]
    [InlineData("if ([||]b)")]
    [InlineData("[|i|]f (b)")]
    [InlineData("[|if (|]b)")]
    [InlineData("if [|(|]b)")]
    [InlineData("if (b[|)|]")]
    [InlineData("if ([|b|])")]
    [InlineData("if [|(b)|]")]
    public async Task NotMergedOnNestedIfSpans(string ifLine)
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            " + ifLine + @"
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnNestedIfOverreachingSelection1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [|if (b)
            |]{
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnNestedIfOverreachingSelection2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [|if (b)
            {|]
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnNestedIfBodySelection()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
            [|{
            }|]
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnNestedIfBodyCaret1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
            [||]{
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnNestedIfBodyCaret2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
            {
            }[||]
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnSingleIfInsideBlock()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        {
            [||]if (b)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnSingleIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [||]if (b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithAndExpressions()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b)
        {
            [||]if (c && d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b && c && d)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithOrExpressionParenthesized1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b)
        {
            [||]if (c && d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((a || b) && c && d)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithOrExpressionParenthesized2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b)
        {
            [||]if (c || d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b && (c || d))
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithBitwiseOrExpressionNotParenthesized1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a | b)
        {
            [||]if (c && d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a | b && c && d)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithBitwiseOrExpressionNotParenthesized2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b)
        {
            [||]if (c | d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b && c | d)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithMixedExpressions1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b && c)
        {
            [||]if (c == d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((a || b && c) && c == d)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithMixedExpressions2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a == b)
        {
            [||]if (b && c || d)
            {
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a == b && (b && c || d))
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithNestedIfInsideWhileLoop()
    {
        // Do not consider the while loop to be a simple block (as might be suggested by some language-agnostic helpers).
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            while (true)
                [||]if (b)
                {
                }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithNestedIfInsideBlockInsideUsingStatement()
    {
        // Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            using (null)
            {
                [||]if (b)
                {
                }
            }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithNestedIfInsideUsingStatementInsideBlock()
    {
        // Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            using (null)
                [||]if (b)
                {
                }
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithNestedIfInsideNestedBlockStatementInsideBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            {
                [||]if (b)
                {
                    System.Console.WriteLine(a && b);
                }
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
            System.Console.WriteLine(a && b);
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithNestedIfInsideNestedBlockStatementWithoutBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
    }
}");
    }

    [Fact]
    public async Task MergedWithNestedIfInsideBlockStatementInsideBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
            {
                System.Console.WriteLine(a && b);
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
            System.Console.WriteLine(a && b);
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithNestedIfInsideBlockStatementWithoutBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
    }
}");
    }

    [Fact]
    public async Task MergedWithNestedIfWithoutBlockStatementInsideBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            [||]if (b)
            {
                System.Console.WriteLine(a && b);
            }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
        {
            System.Console.WriteLine(a && b);
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithNestedIfWithoutBlockStatementWithoutBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            [||]if (b)
                System.Console.WriteLine(a && b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauseOnNestedIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine();
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfClauseOnNestedIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine(a);
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfElseClausesOnNestedIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine(a);
            else
                System.Console.WriteLine();
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauseOnOuterIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
        }
        else
            System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfClauseOnOuterIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
        }
        else if (a)
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfElseClausesOnOuterIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
        }
        else if (a)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfElseClauses1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine();
        }
        else
            System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfElseClauses2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine();
        }
        else if (a)
            System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfClauses1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine();
        }
        else if (b)
            System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfClauses2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine(a);
        }
        else if (a)
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfClauses3()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
            {
                System.Console.WriteLine(a);
            }
        }
        else if (a)
        {
            System.Console.WriteLine(b);
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseIfClauses4()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
            {
                System.Console.WriteLine(a);
            }
        }
        else if (a)
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauses1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
        {
            System.Console.WriteLine(b);
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauses2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauses3()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauses4()
    {
        // Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
            using (null)
                System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task NotMergedIntoElseIfWithUnmatchingElseClauses1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
        {
            System.Console.WriteLine(b);
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedIntoElseIfWithUnmatchingElseClauses2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedIntoElseIfWithUnmatchingElseClauses3()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedIntoElseIfWithUnmatchingElseClauses4()
    {
        // Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
            using (null)
                System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseClauses1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseClauses2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
        {
            System.Console.WriteLine(a);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
        {
            System.Console.WriteLine(a);
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseIfClauses()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine(a);
        }
        else if (a)
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else if (a)
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseIfElseClauses()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
                System.Console.WriteLine(a);
            else
                System.Console.WriteLine(a);
        }
        else if (a)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else if (a)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseClausesWithDifferenceInBlocks1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseClausesWithDifferenceInBlocks2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
        {
            System.Console.WriteLine(a);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
        {
            System.Console.WriteLine(a);
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseClausesWithDifferenceInBlocks3()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
        {
            {
                System.Console.WriteLine(a);
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
        {
            {
                System.Console.WriteLine(a);
            }
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseIfClausesWithDifferenceInBlocks()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
            {
                System.Console.WriteLine(a);
            }
        }
        else if (a)
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else if (a)
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseIfElseClausesWithDifferenceInBlocks()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else if (a)
            {
                System.Console.WriteLine(a);
            }
            else
                System.Console.WriteLine();
        }
        else if (a)
            System.Console.WriteLine(a);
        else
        {
            System.Console.WriteLine();
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else if (a)
            System.Console.WriteLine(a);
        else
        {
            System.Console.WriteLine();
        }
    }
}");
    }

    [Fact]
    public async Task MergedIntoElseIfWithMatchingElseClauses1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedIntoElseIfWithMatchingElseClauses2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
            {
                System.Console.WriteLine(a);
            }
        }
        else
        {
            System.Console.WriteLine(a);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (a && b)
            System.Console.WriteLine(a && b);
        else
        {
            System.Console.WriteLine(a);
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClauseOnNestedIfWithoutBlock()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithUnmatchingElseClausesForNestedIfWithoutBlock()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task MergedWithMatchingElseClausesForNestedIfWithoutBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        else
            System.Console.WriteLine(a);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithSingleLineFormatting()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b) System.Console.WriteLine();
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraUnmatchingStatementBelowNestedIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            System.Console.WriteLine(b);
        }
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraUnmatchingStatementBelowOuterIf()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine(b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraUnmatchingStatementsIfControlFlowContinues()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            System.Console.WriteLine(a);
            System.Console.WriteLine(b);
        }
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine(b);
        System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraUnmatchingStatementsIfControlFlowQuits()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            throw new System.Exception();
        }
        else
            System.Console.WriteLine(a);

        return;
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraPrecedingMatchingStatementsIfControlFlowQuits()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        return;

        if (a)
        {
            return;

            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);
        }
        else
            System.Console.WriteLine(a);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            System.Console.WriteLine(a);
            System.Console.WriteLine(b);
        }
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine(a);
        System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            if (a)
                return;
        }
        else
            System.Console.WriteLine(a);

        if  (a)
            return;
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues3()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
                else
                    System.Console.WriteLine(a);

                switch (a)
                {
                    default:
                        break;
                }
            }
            else
                System.Console.WriteLine(a);

            switch (a)
            {
                default:
                    break;
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues4()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
                else
                    System.Console.WriteLine(a);

                while (a != b)
                    continue;
            }
            else
                System.Console.WriteLine(a);

            while (a != b)
                continue;
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedIntoElseIfWithExtraMatchingStatementsIfControlFlowContinues()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a == b)
        {
        }
        else if (a || b)
        {
        }
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            System.Console.WriteLine();
        }
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine();
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuits1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            return;
        }
        else
            System.Console.WriteLine(a);

        return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);

        return;
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuits2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            System.Console.WriteLine(a);
            throw new System.Exception();
        }
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine(a);
        throw new System.Exception();
        System.Console.WriteLine(b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);

        System.Console.WriteLine(a);
        throw new System.Exception();
        System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuits3()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
                else
                    System.Console.WriteLine(a);

                switch (a)
                {
                    default:
                        continue;
                }
            }
            else
                System.Console.WriteLine(a);

            switch (a)
            {
                default:
                    continue;
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            if (a && b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            switch (a)
            {
                default:
                    continue;
            }
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuits4()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            System.Console.WriteLine();

            if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
                else
                    System.Console.WriteLine(a);

                if (a)
                    continue;
                else
                    break;
            }
            else
                System.Console.WriteLine(a);

            if (a)
                continue;
            else
                break;
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            System.Console.WriteLine();

            if (a && b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            if (a)
                continue;
            else
                break;
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuits5()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            System.Console.WriteLine();

            if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
                else
                    System.Console.WriteLine(a);

                switch (a)
                {
                    default:
                        continue;
                }
            }
            else
                System.Console.WriteLine(a);

            switch (a)
            {
                default:
                    continue;
            }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        while (a != b)
        {
            System.Console.WriteLine();

            if (a && b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            switch (a)
            {
                default:
                    continue;
            }
        }
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuits6()
    {
        await TestInRegularAndScriptAsync(
@"bool a = bool.Parse("""");
bool b = bool.Parse("""");

start:
System.Console.WriteLine();

if (a)
{
    [||]if (b)
        System.Console.WriteLine(a && b);
    else
        System.Console.WriteLine(a);

    switch (a)
    {
        default:
            goto start;
    }
}
else
    System.Console.WriteLine(a);

switch (a)
{
    default:
        goto start;
}
",
@"bool a = bool.Parse("""");
bool b = bool.Parse("""");

start:
System.Console.WriteLine();

if (a && b)
    System.Console.WriteLine(a && b);
else
    System.Console.WriteLine(a);

switch (a)
{
    default:
        goto start;
}
");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsIfControlFlowQuitsInSwitchSection()
    {
        // Switch sections are interesting in that they are blocks of statements that aren't BlockSyntax.
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        switch (a)
        {
            case true:
                System.Console.WriteLine();

                if (a)
                {
                    [||]if (b)
                        System.Console.WriteLine(a && b);

                    break;
                }

                break;
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        switch (a)
        {
            case true:
                System.Console.WriteLine();

                if (a && b)
                    System.Console.WriteLine(a && b);

                break;
        }
    }
}");
    }

    [Fact]
    public async Task MergedIntoElseIfWithExtraMatchingStatementsIfControlFlowQuits()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a == b)
        {
        }
        else if (a || b)
        {
        }
        else if (a)
        {
            [||]if (b)
                System.Console.WriteLine(a && b);
            else
                System.Console.WriteLine(a);

            return;
        }
        else
            System.Console.WriteLine(a);

        return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a == b)
        {
        }
        else if (a || b)
        {
        }
        else if (a && b)
            System.Console.WriteLine(a && b);
        else
            System.Console.WriteLine(a);

        return;
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraMatchingStatementsInsideExtraBlockIfControlFlowQuits()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);
            }

            return;
        }

        return;
    }
}");
    }

    [Fact]
    public async Task MergedWithExtraMatchingStatementsInsideInnermostBlockIfControlFlowQuits()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);

                return;
            }
        }

        return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            System.Console.WriteLine(a && b);

        return;
    }
}");
    }

    [Fact]
    public async Task NotMergedWithExtraMatchingStatementInOuterScopeOfEmbeddedStatementIfControlFlowQuits()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        using (null)
            if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);

                return;
            }

        return;
    }
}");
    }

    [Fact]
    public async Task NotMergedIntoElseIfWithExtraMatchingStatementInOuterScopeOfEmbeddedStatementIfControlFlowQuits()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        using (null)
            if (a || b)
                System.Console.WriteLine(a);
            else if (a)
            {
                [||]if (b)
                    System.Console.WriteLine(a && b);

                return;
            }

        return;
    }
}");
    }
}
