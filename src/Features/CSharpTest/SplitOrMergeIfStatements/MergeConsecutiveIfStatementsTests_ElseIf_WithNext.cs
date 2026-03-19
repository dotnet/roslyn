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
    [InlineData("[||]if (a)")]
    [InlineData("i[||]f (a)")]
    [InlineData("if[||] (a)")]
    [InlineData("if [||](a)")]
    [InlineData("if (a)[||]")]
    [InlineData("[|if|] (a)")]
    [InlineData("[|if (a)|]")]
    public Task MergedOnIfSpans(string ifLine)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    
            """ + ifLine + """

                    {
                    }
                    else if (b)
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
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnIfExtendedHeaderSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
            [|        if (a)
            |]        {
                    }
                    else if (b)
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
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnIfFullSelectionWithoutElseIfClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                    {
                    }|]
                    else if (b)
                    {
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
                    if (a || b)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnIfExtendedFullSelectionWithoutElseIfClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
            [|        if (a)
                    {
                    }
            |]        else if (b)
                    {
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
                    if (a || b)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfFullSelectionWithElseIfClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                    {
                    }
                    else if (b)
                    {
                    }|]
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfExtendedFullSelectionWithElseIfClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
            [|        if (a)
                    {
                    }
                    else if (b)
                    {
                    }
            |]        else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfFullSelectionWithElseIfElseClauses()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                    {
                    }
                    else if (b)
                    {
                    }
                    else
                    {
                    }|]
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfExtendedFullSelectionWithElseIfElseClauses()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
            [|        if (a)
                    {
                    }
                    else if (b)
                    {
                    }
                    else
                    {
                    }
            |]    }
            }
            """);

    [Theory]
    [InlineData("if ([||]a)")]
    [InlineData("[|i|]f (a)")]
    [InlineData("[|if (|]a)")]
    [InlineData("if [|(|]a)")]
    [InlineData("if (a[|)|]")]
    [InlineData("if ([|a|])")]
    [InlineData("if [|(a)|]")]
    public Task NotMergedOnIfSpans(string ifLine)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    
            """ + ifLine + """

                    {
                    }
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfOverreachingSelection1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                    |]{
                    }
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfOverreachingSelection2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (a)
                    {|]
                    }
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfBodySelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    [|{
                    }|]
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfBodyCaret1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    [||]{
                    }
                    else if (b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnIfBodyCaret2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }[||]
                    else if (b)
                    {
                    }
                }
            }
            """);
}
