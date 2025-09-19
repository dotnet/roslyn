// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

public sealed partial class MergeConsecutiveIfStatementsTests
{
    [Theory]
    [InlineData("[||]if (b)")]
    [InlineData("i[||]f (b)")]
    [InlineData("if[||] (b)")]
    [InlineData("if [||](b)")]
    [InlineData("if (b)[||]")]
    [InlineData("[|if|] (b)")]
    [InlineData("[|if (b)|]")]
    public Task MergedIntoPreviousStatementOnIfSpans(string ifLine)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
            """ + ifLine + """
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementOnIfExtendedHeaderSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
            [|        if (b)
            |]            return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementOnIfFullSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [|if (b)
                        return;|]
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementOnIfExtendedFullSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
            [|        if (b)
                        return;
            |]    }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementOnIfFullSelectionWithoutElseClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [|if (b)
                        return;|]
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
                    if (a || b)
                        return;
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementOnIfExtendedFullSelectionWithoutElseClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
            [|        if (b)
                        return;
            |]        else
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
                    if (a || b)
                        return;
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementOnIfFullSelectionWithElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [|if (b)
                        return;
                    else
                    {
                    }|]
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementOnIfExtendedFullSelectionWithElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
            [|        if (b)
                        return;
                    else
                    {
                    }
            |]    }
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
    public Task NotMergedIntoPreviousStatementOnIfSpans(string ifLine)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
            """ + ifLine + """
                        return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementOnIfOverreachingSelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [|if (b)
                      |]return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementOnIfBodySelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    if (b)
                        [|return;|]
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIfControlFlowQuits1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [||]if (b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIfControlFlowQuits2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        throw new System.Exception();
                    [||]if (b)
                        throw new System.Exception();
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        throw new System.Exception();
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIfControlFlowQuits3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a)
                            continue;
                        [||]if (b)
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
                        if (a || b)
                            continue;
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIfControlFlowQuits4()
        => TestInRegularAndScriptAsync(
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

                        [||]if (b)
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
                        if (a || b)
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
    public Task MergedIntoPreviousStatementIfControlFlowQuits5()
        => TestInRegularAndScriptAsync(
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

                        [||]if (b)
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
                        if (a || b)
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
    public Task MergedIntoPreviousStatementIfControlFlowQuitsInSwitchSection()
        => TestInRegularAndScriptAsync(
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
                            [||]if (b)
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
                            if (a || b)
                                break;
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIfControlFlowQuitsWithDifferenceInBlocks()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        {
                            return;
                        }
                    }

                    [||]if (b)
                        return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                    {
                        {
                            return;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIncludingElseClauseIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [||]if (b)
                        return;
                    else
                        System.Console.WriteLine();
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                    else
                        System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIncludingElseIfClauseIfControlFlowQuits()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [||]if (b)
                        return;
                    else if (a && b)
                        System.Console.WriteLine();
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        return;
                    else if (a && b)
                        System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task MergedIntoPreviousStatementIfControlFlowQuitsWithPreservedSingleLineFormatting()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a) return;
                    [||]if (b) return;
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b) return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementIfControlFlowContinues1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }

                    [||]if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementIfControlFlowContinues2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    [||]if (b)
                        System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementIfControlFlowContinues3()
        => TestMissingInRegularAndScriptAsync(
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

                    [||]if (b)
                    {
                        if (a)
                            return;
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementIfControlFlowContinues4()
        => TestMissingInRegularAndScriptAsync(
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

                    [||]if (b)
                        while (a)
                        {
                            break;
                        }
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementIfControlFlowContinues5()
        => TestMissingInRegularAndScriptAsync(
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

                        [||]if (b)
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
    public Task NotMergedIntoPreviousStatementWithUnmatchingStatementsIfControlFlowQuits()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    [||]if (b)
                        throw new System.Exception();
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementThatHasElseClauseIfControlFlowQuits1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    else
                        return;

                    [||]if (b)
                        return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementThatHasElseClauseIfControlFlowQuits2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    else
                        return;

                    [||]if (b)
                        return;
                    else
                        return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementAsEmbeddedStatementIfControlFlowQuits1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;

                    while (a)
                        [||]if (b)
                            return;
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoPreviousStatementAsEmbeddedStatementIfControlFlowQuits2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;

                    if (a)
                    {
                    }
                    else [||]if (b)
                        return;
                }
            }
            """);
}
