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

[Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoConsecutiveIfStatements)]
public sealed class SplitIntoConsecutiveIfStatementsTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider();

    [Theory]
    [InlineData("a [||]|| b")]
    [InlineData("a |[||]| b")]
    [InlineData("a ||[||] b")]
    [InlineData("a [||||] b")]
    public Task SplitOnOrOperatorSpans(string condition)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (
            """ + condition + """
            )
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Theory]
    [InlineData("a [|||]| b")]
    [InlineData("a[| |||] b")]
    [InlineData("a[||] || b")]
    public Task NotSplitOnOrOperatorSpans(string condition)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (
            """ + condition + """
            )
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnIfKeyword()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [||]if (a || b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnAndOperator()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]&& b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnBitwiseOrOperator()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]| b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnOrOperatorOutsideIfStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    var v = a [||]|| b;
                }
            }
            """);

    [Fact]
    public Task NotSplitOnOrOperatorInIfStatementBody()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        a [||]|| b;
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedOrExpression1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a [||]|| b || c || d)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a)
                    {
                    }
                    else if (b || c || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedOrExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b [||]|| c || d)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b)
                    {
                    }
                    else if (c || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedOrExpression3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b || c [||]|| d)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b || c)
                    {
                    }
                    else if (d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitInsideParentheses1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if ((a [||]|| b) || c || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitInsideParentheses2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b || (c [||]|| d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitInsideParentheses3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if ((a || b [||]|| c || d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithOtherExpressionInsideParentheses1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a [||]|| (b || c) || d)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a)
                    {
                    }
                    else if ((b || c) || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithOtherExpressionInsideParentheses2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || (b || c) [||]|| d)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || (b || c))
                    {
                    }
                    else if (d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedAndExpression1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]|| b && c)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                    }
                    else if (b && c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedAndExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a && b [||]|| c)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a && b)
                    {
                    }
                    else if (c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedConditionalExpression1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]|| b ? c : c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedConditionalExpression2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a ? b : b [||]|| c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedConditionalExpressionInsideParentheses1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]|| (b ? c : c))
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                    }
                    else if ((b ? c : c))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedConditionalExpressionInsideParentheses2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if ((a ? b : b) [||]|| c)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if ((a ? b : b))
                    {
                    }
                    else if (c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a || b);
                    }
                    else if (b)
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine(a || b);
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine(a || b);
                    else if (b)
                        System.Console.WriteLine(a || b);
                }
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        if (true) { }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (true) { }
                    }
                    else if (b)
                        if (true) { }
                }
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatementInWhileLoop()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        while (a)
                            if (true)
                                using (null) { }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        while (a)
                            if (true)
                                using (null) { }
                    }
                    else if (b)
                        while (a)
                            if (true)
                                using (null) { }
                }
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatementInsideBlockInWhileLoop()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        while (a)
                        {
                            if (true)
                                using (null) { }
                        }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        while (a)
                        {
                            if (true)
                                using (null) { }
                        }
                    else if (b)
                        while (a)
                        {
                            if (true)
                                using (null) { }
                        }
                }
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                        if (true) { }
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (true) { }
                    }
                    else if (b)
                    {
                        if (true) { }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMissingStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
            else if (b)
                }
            }
            """);

    [Fact]
    public Task SplitWithElseStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithElseStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else
                        System.Console.WriteLine(a || b);
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else
                        System.Console.WriteLine(a || b);
                }
            }
            """);

    [Fact]
    public Task SplitWithElseNestedIfStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else if (true) { }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else if (true) { }
                }
            }
            """);

    [Fact]
    public Task SplitWithElseIfElse()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else if (a)
                        System.Console.WriteLine(a);
                    else
                        System.Console.WriteLine(b);
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else if (a)
                        System.Console.WriteLine(a);
                    else
                        System.Console.WriteLine(b);
                }
            }
            """);

    [Fact]
    public Task SplitAsPartOfElseIfElse()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                        System.Console.WriteLine();
                    else if (a [||]|| b)
                        System.Console.WriteLine(a);
                    else
                        System.Console.WriteLine(b);
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                        System.Console.WriteLine();
                    else if (a)
                        System.Console.WriteLine(a);
                    else if (b)
                        System.Console.WriteLine(a);
                    else
                        System.Console.WriteLine(b);
                }
            }
            """);

    [Fact]
    public Task SplitWithMissingElseStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else
                }
            }
            """);

    [Fact]
    public Task SplitWithPreservedSingleLineFormatting()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b) System.Console.WriteLine();
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a) System.Console.WriteLine();
                    else if (b) System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    if (b)
                        return;
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        throw new System.Exception();
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        throw new System.Exception();
                    if (b)
                        throw new System.Exception();
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a [||]|| b)
                            continue;
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a)
                            continue;
                        if (b)
                            continue;
                    }
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a [||]|| b)
                        {
                            if (a)
                                continue;
                            else
                                break;
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a)
                        {
                            if (a)
                                continue;
                            else
                                break;
                        }

                        if (b)
                        {
                            if (a)
                                continue;
                            else
                                break;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a [||]|| b)
                            switch (a)
                            {
                                default:
                                    continue;
                            }
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a)
                            switch (a)
                            {
                                default:
                                    continue;
                            }

                        if (b)
                            switch (a)
                            {
                                default:
                                    continue;
                            }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuitsInSwitchSection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    switch (a)
                    {
                        case true:
                            if (a [||]|| b)
                                break;
                            break;
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    switch (a)
                    {
                        case true:
                            if (a)
                                break;
                            if (b)
                                break;
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuitsWithNestedIfStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        if (true)
                            return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        if (true)
                            return;
                    if (b)
                        if (true)
                            return;
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuitsWithPreservedSingleLineFormatting()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b) return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a) return;
                    if (b) return;
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                        if (a)
                            return;
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (a)
                            return;
                    }
                    else if (b)
                    {
                        if (a)
                            return;
                    }
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        while (a)
                        {
                            break;
                        }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        while (a)
                        {
                            break;
                        }
                    else if (b)
                        while (a)
                        {
                            break;
                        }
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a [||]|| b)
                            switch (a)
                            {
                                default:
                                    break;
                            }
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a)
                            switch (a)
                            {
                                default:
                                    break;
                            }
                        else if (b)
                            switch (a)
                            {
                                default:
                                    break;
                            }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsWithElseIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        return;
                    else
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    else if (b)
                        return;
                    else
                        return;
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsAsEmbeddedStatementIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (a)
                        if (a [||]|| b)
                            return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (a)
                    {
                        if (a)
                            return;
                        if (b)
                            return;
                    }
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsAsElseIfIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    else if (a [||]|| b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    else if (a)
                        return;
                    else if (b)
                        return;
                }
            }
            """);
}
