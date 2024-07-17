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

[Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoNestedIfStatements)]
public sealed class SplitIntoNestedIfStatementsTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpSplitIntoNestedIfStatementsCodeRefactoringProvider();

    [Theory]
    [InlineData("a [||]&& b")]
    [InlineData("a &[||]& b")]
    [InlineData("a &&[||] b")]
    [InlineData("a [|&&|] b")]
    public async Task SplitOnAndOperatorSpans(string condition)
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (" + condition + @")
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
            {
            }
        }
    }
}");
    }

    [Theory]
    [InlineData("a [|&|]& b")]
    [InlineData("a[| &&|] b")]
    [InlineData("a[||] && b")]
    public async Task NotSplitOnAndOperatorSpans(string condition)
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (" + condition + @")
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitOnIfKeyword()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [||]if (a && b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitOnOrOperator()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitOnBitwiseAndOperator()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]& b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitOnAndOperatorOutsideIfStatement()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        var v = a [||]&& b;
    }
}");
    }

    [Fact]
    public async Task NotSplitOnAndOperatorInIfStatementBody()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a && b)
            a [||]&& b;
    }
}");
    }

    [Fact]
    public async Task SplitWithChainedAndExpression1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a [||]&& b && c && d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a)
        {
            if (b && c && d)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithChainedAndExpression2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b [||]&& c && d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b)
        {
            if (c && d)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithChainedAndExpression3()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b && c [||]&& d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b && c)
        {
            if (d)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitInsideParentheses1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((a [||]&& b) && c && d)
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitInsideParentheses2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b && (c [||]&& d))
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitInsideParentheses3()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((a && b [||]&& c && d))
        {
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithOtherExpressionInsideParentheses1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a [||]&& (b && c) && d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a)
        {
            if ((b && c) && d)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithOtherExpressionInsideParentheses2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && (b && c) [||]&& d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && (b && c))
        {
            if (d)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitWithMixedOrExpression1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a [||]&& b || c)
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotSplitWithMixedOrExpression2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a || b [||]&& c)
        {
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithMixedOrExpressionInsideParentheses1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a [||]&& (b || c))
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
        {
            if ((b || c))
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithMixedOrExpressionInsideParentheses2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if ((a || b) [||]&& c)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if ((a || b))
        {
            if (c)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithMixedBitwiseOrExpression1()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a [||]&& b | c)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
        {
            if (b | c)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithMixedBitwiseOrExpression2()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a | b [||]&& c)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a | b)
        {
            if (c)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithStatementInsideBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
        {
            System.Console.WriteLine(a && b);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
            {
                System.Console.WriteLine(a && b);
            }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithStatementWithoutBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            System.Console.WriteLine(a && b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                System.Console.WriteLine(a && b);
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithNestedIfStatement()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            if (true) { }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                if (true) { }
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithMissingStatement()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
}
    }
}");
    }

    [Fact]
    public async Task SplitWithElseStatementInsideBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            System.Console.WriteLine();
        else
        {
            System.Console.WriteLine(a && b);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                System.Console.WriteLine();
            else
            {
                System.Console.WriteLine(a && b);
            }
        }
        else
        {
            System.Console.WriteLine(a && b);
        }
    }
}");
    }

    [Fact]
    public async Task SplitWithElseStatementWithoutBlock()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            System.Console.WriteLine();
        else
            System.Console.WriteLine(a && b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                System.Console.WriteLine();
            else
                System.Console.WriteLine(a && b);
        }
        else
            System.Console.WriteLine(a && b);
    }
}");
    }

    [Fact]
    public async Task SplitWithElseNestedIfStatement()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            System.Console.WriteLine();
        else if (true) { }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                System.Console.WriteLine();
            else if (true) { }
        }
        else if (true) { }
    }
}");
    }

    [Fact]
    public async Task SplitWithElseIfElse()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            System.Console.WriteLine();
        else if (a)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                System.Console.WriteLine();
            else if (a)
                System.Console.WriteLine(a);
            else
                System.Console.WriteLine(b);
        }
        else if (a)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task SplitAsPartOfElseIfElse()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (true)
            System.Console.WriteLine();
        else if (a [||]&& b)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (true)
            System.Console.WriteLine();
        else if (a)
        {
            if (b)
                System.Console.WriteLine(a);
            else
                System.Console.WriteLine(b);
        }
        else
            System.Console.WriteLine(b);
    }
}");
    }

    [Fact]
    public async Task SplitWithMissingElseStatement()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
            System.Console.WriteLine();
        else
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b)
                System.Console.WriteLine();
            else
}
        else
    }
}");
    }

    [Fact]
    public async Task SplitWithPreservedSingleLineFormatting()
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b) System.Console.WriteLine();
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (b) System.Console.WriteLine();
        }
    }
}");
    }
}
