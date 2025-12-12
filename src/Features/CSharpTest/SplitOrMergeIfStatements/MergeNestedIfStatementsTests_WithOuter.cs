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
    public Task MergedOnNestedIfSpans(string ifLine)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
            """ + ifLine + """
                        {
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
                    if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnNestedIfExtendedHeaderSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnNestedIfFullSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnNestedIfFullSelectionWithElseClause()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedOnNestedIfFullSelectionWithoutElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Theory]
    [InlineData("if ([||]b)")]
    [InlineData("[|i|]f (b)")]
    [InlineData("[|if (|]b)")]
    [InlineData("if [|(|]b)")]
    [InlineData("if (b[|)|]")]
    [InlineData("if ([|b|])")]
    [InlineData("if [|(b)|]")]
    public Task NotMergedOnNestedIfSpans(string ifLine)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
            """ + ifLine + """
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnNestedIfOverreachingSelection1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedOnNestedIfOverreachingSelection2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedOnNestedIfBodySelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedOnNestedIfBodyCaret1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedOnNestedIfBodyCaret2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedOnSingleIfInsideBlock()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    {
                        [||]if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnSingleIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [||]if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithAndExpressions()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b && c && d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithOrExpressionParenthesized1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if ((a || b) && c && d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithOrExpressionParenthesized2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b && (c || d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithBitwiseOrExpressionNotParenthesized1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a | b && c && d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithBitwiseOrExpressionNotParenthesized2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b && c | d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithMixedExpressions1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if ((a || b && c) && c == d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithMixedExpressions2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a == b && (b && c || d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedWithNestedIfInsideWhileLoop()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        while (true)
                            [||]if (b)
                            {
                            }
                }
            }
            """);

    [Fact]
    public Task NotMergedWithNestedIfInsideBlockInsideUsingStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithNestedIfInsideUsingStatementInsideBlock()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithNestedIfInsideNestedBlockStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                    {
                        System.Console.WriteLine(a && b);
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithNestedIfInsideNestedBlockStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                }
            }
            """);

    [Fact]
    public Task MergedWithNestedIfInsideBlockStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                    {
                        System.Console.WriteLine(a && b);
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithNestedIfInsideBlockStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        [||]if (b)
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
                    if (a && b)
                        System.Console.WriteLine(a && b);
                }
            }
            """);

    [Fact]
    public Task MergedWithNestedIfWithoutBlockStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        [||]if (b)
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
                    if (a && b)
                    {
                        System.Console.WriteLine(a && b);
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithNestedIfWithoutBlockStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        [||]if (b)
                            System.Console.WriteLine(a && b);
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                }
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauseOnNestedIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfClauseOnNestedIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfElseClausesOnNestedIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauseOnOuterIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfClauseOnOuterIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfElseClausesOnOuterIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfElseClauses1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfElseClauses2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfClauses1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfClauses2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfClauses3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseIfClauses4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauses1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauses2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauses3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauses4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedIntoElseIfWithUnmatchingElseClauses1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedIntoElseIfWithUnmatchingElseClauses2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedIntoElseIfWithUnmatchingElseClauses3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedIntoElseIfWithUnmatchingElseClauses4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseClauses1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else
                        System.Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseClauses2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseIfClauses()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else if (a)
                        System.Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseIfElseClauses()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseClausesWithDifferenceInBlocks1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else
                        System.Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseClausesWithDifferenceInBlocks2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseClausesWithDifferenceInBlocks3()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseIfClausesWithDifferenceInBlocks()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else if (a)
                        System.Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseIfElseClausesWithDifferenceInBlocks()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedIntoElseIfWithMatchingElseClauses1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedIntoElseIfWithMatchingElseClauses2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClauseOnNestedIfWithoutBlock()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        [||]if (b)
                            System.Console.WriteLine(a && b);
                        else
                            System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task NotMergedWithUnmatchingElseClausesForNestedIfWithoutBlock()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithMatchingElseClausesForNestedIfWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else
                        System.Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task MergedWithSingleLineFormatting()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        [||]if (b) System.Console.WriteLine();
                    }
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task NotMergedWithExtraUnmatchingStatementBelowNestedIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraUnmatchingStatementBelowOuterIf()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else
                        System.Console.WriteLine(a);

                    System.Console.WriteLine(b);
                }
            }
            """);

    [Fact]
    public Task NotMergedWithExtraUnmatchingStatementsIfControlFlowContinues()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraUnmatchingStatementsIfControlFlowQuits()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraPrecedingMatchingStatementsIfControlFlowQuits()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraMatchingStatementsIfControlFlowContinues4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedIntoElseIfWithExtraMatchingStatementsIfControlFlowContinues()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuits1()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);
                    else
                        System.Console.WriteLine(a);

                    return;
                }
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuits2()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuits3()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuits4()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuits5()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuits6()
        => TestInRegularAndScriptAsync(
            """
            bool a = bool.Parse("");
            bool b = bool.Parse("");

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
            """,
            """
            bool a = bool.Parse("");
            bool b = bool.Parse("");

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
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsIfControlFlowQuitsInSwitchSection()
        => TestInRegularAndScriptAsync(
            """
            class C
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
                            System.Console.WriteLine();

                            if (a && b)
                                System.Console.WriteLine(a && b);

                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoElseIfWithExtraMatchingStatementsIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedWithExtraMatchingStatementsInsideExtraBlockIfControlFlowQuits()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task MergedWithExtraMatchingStatementsInsideInnermostBlockIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        System.Console.WriteLine(a && b);

                    return;
                }
            }
            """);

    [Fact]
    public Task NotMergedWithExtraMatchingStatementInOuterScopeOfEmbeddedStatementIfControlFlowQuits()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task NotMergedIntoElseIfWithExtraMatchingStatementInOuterScopeOfEmbeddedStatementIfControlFlowQuits()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);
}
