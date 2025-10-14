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
    public Task SplitOnAndOperatorSpans(string condition)
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
                        if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Theory]
    [InlineData("a [|&|]& b")]
    [InlineData("a[| &&|] b")]
    [InlineData("a[||] && b")]
    public Task NotSplitOnAndOperatorSpans(string condition)
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
                    [||]if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnOrOperator()
        => TestMissingInRegularAndScriptAsync(
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
            """);

    [Fact]
    public Task NotSplitOnBitwiseAndOperator()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]& b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnAndOperatorOutsideIfStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    var v = a [||]&& b;
                }
            }
            """);

    [Fact]
    public Task NotSplitOnAndOperatorInIfStatementBody()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        a [||]&& b;
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedAndExpression1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a [||]&& b && c && d)
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
                        if (b && c && d)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedAndExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b [||]&& c && d)
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
                    if (a && b)
                    {
                        if (c && d)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedAndExpression3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b && c [||]&& d)
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
                    if (a && b && c)
                    {
                        if (d)
                        {
                        }
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
                    if ((a [||]&& b) && c && d)
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
                    if (a && b && (c [||]&& d))
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
                    if ((a && b [||]&& c && d))
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
                    if (a [||]&& (b && c) && d)
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
                        if ((b && c) && d)
                        {
                        }
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
                    if (a && (b && c) [||]&& d)
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
                    if (a && (b && c))
                    {
                        if (d)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedOrExpression1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]&& b || c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedOrExpression2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a || b [||]&& c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedOrExpressionInsideParentheses1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]&& (b || c))
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
                        if ((b || c))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedOrExpressionInsideParentheses2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if ((a || b) [||]&& c)
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
                    if ((a || b))
                    {
                        if (c)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedBitwiseOrExpression1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]&& b | c)
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
                        if (b | c)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedBitwiseOrExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a | b [||]&& c)
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
                    if (a | b)
                    {
                        if (c)
                        {
                        }
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
                    if (a [||]&& b)
                    {
                        System.Console.WriteLine(a && b);
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
                        if (b)
                        {
                            System.Console.WriteLine(a && b);
                        }
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
                    if (a [||]&& b)
                        System.Console.WriteLine(a && b);
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
                        if (b)
                            System.Console.WriteLine(a && b);
                    }
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
                    if (a [||]&& b)
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
                        if (b)
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
                    if (a [||]&& b)
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
                        if (b)
            }
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
                    if (a [||]&& b)
                        System.Console.WriteLine();
                    else
                    {
                        System.Console.WriteLine(a && b);
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
                    if (a [||]&& b)
                        System.Console.WriteLine();
                    else
                        System.Console.WriteLine(a && b);
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
                        if (b)
                            System.Console.WriteLine();
                        else
                            System.Console.WriteLine(a && b);
                    }
                    else
                        System.Console.WriteLine(a && b);
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
                    if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine();
                        else if (true) { }
                    }
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
                    if (a [||]&& b)
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
                    else if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine(a);
                        else
                            System.Console.WriteLine(b);
                    }
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
                    if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine();
                        else
            }
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
                    if (a [||]&& b) System.Console.WriteLine();
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
                        if (b) System.Console.WriteLine();
                    }
                }
            }
            """);
}
