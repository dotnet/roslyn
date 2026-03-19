// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

public sealed partial class MergeNestedIfStatementsTests
{
    [Fact]
    public Task MergedOnOuterIf()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [||]if (a)
                    {
                        if (b)
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55563")]
    public Task MergedOnOuterIf_TopLevelStatements()
        => TestInRegularAndScriptAsync(
            """
            var a = true;
            var b = true;

            [||]if (a)
            {
                if (b)
                {
                }
            }

            """,
            """
            var a = true;
            var b = true;

            if (a && b)
            {
            }

            """);

    [Theory]
    [InlineData("[||]else if (a)")]
    [InlineData("el[||]se if (a)")]
    [InlineData("else[||] if (a)")]
    [InlineData("else [||]if (a)")]
    [InlineData("else i[||]f (a)")]
    [InlineData("else if[||] (a)")]
    [InlineData("else if [||](a)")]
    [InlineData("else if (a)[||]")]
    [InlineData("else [|if|] (a)")]
    [InlineData("else [|if (a)|]")]
    [InlineData("[|else if (a)|]")]
    public Task MergedOnOuterElseIfSpans(string elseIfLine)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    
            """ + elseIfLine + """

                    {
                        if (b)
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
                    if (true)
                    {
                    }
                    else if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnOuterElseIfExtendedHeaderSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
            [|        else if (a)
            |]        {
                        if (b)
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
                    if (true)
                    {
                    }
                    else if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnOuterElseIfFullSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
            [|        else if (a)
                    {
                        if (b)
                        {
                        }
                    }
            |]    }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    else if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedOnOuterElseIfFullSelectionWithElseClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    [|else if (a)
                    {
                        if (b)
                        {
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }|]
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    else if (a && b)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfFullSelectionWithoutElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    [|else if (a)
                    {
                        if (b)
                        {
                        }
                        else
                        {
                        }
                    }|]
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfFullSelectionWithParentIf()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    [|if (true)
                    {
                    }
                    else if (a)
                    {
                        if (b)
                        {
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }|]
                }
            }
            """);

    [Theory]
    [InlineData("else if ([||]a)")]
    [InlineData("[|el|]se if (a)")]
    [InlineData("[|else|] if (a)")]
    [InlineData("[|else if|] (a)")]
    [InlineData("else [|i|]f (a)")]
    [InlineData("else [|if (|]a)")]
    [InlineData("else if [|(|]a)")]
    [InlineData("else if (a[|)|]")]
    [InlineData("else if ([|a|])")]
    [InlineData("else if [|(a)|]")]
    public Task NotMergedOnOuterElseIfSpans(string elseIfLine)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    
            """ + elseIfLine + """

                    {
                        if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfOverreachingSelection1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    [|else if (a)
                    |]{
                        if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfOverreachingSelection2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    [|else if (a)
                    {|]
                        if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfBodySelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    else if (a)
                    [|{
                        if (b)
                        {
                        }
                    }|]
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfBodyCaret1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    else if (a)
                    [||]{
                        if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnOuterElseIfBodyCaret2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (true)
                    {
                    }
                    else if (a)
                    {
                        if (b)
                        {
                        }
                    }[||]
                }
            }
            """);

    [Fact]
    public async Task MergedOnMiddleIfMergableWithNestedOnly()
    {
        const string Initial =
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                        [||]if (b)
                        {
                            if (c)
                            {
                                System.Console.WriteLine();
                            }
                        }

                        return;
                    }
                }
            }
            """;
        await TestActionCountAsync(Initial, 1);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                        if (b && c)
                        {
                            System.Console.WriteLine();
                        }

                        return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task MergedOnMiddleIfMergableWithOuterOnly()
    {
        const string Initial =
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                        [||]if (b)
                        {
                            if (c)
                            {
                                System.Console.WriteLine();
                            }

                            return;
                        }
                    }
                }
            }
            """;
        await TestActionCountAsync(Initial, 1);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a && b)
                    {
                        if (c)
                        {
                            System.Console.WriteLine();
                        }

                        return;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task MergedOnMiddleIfMergableWithBoth()
    {
        const string Initial =
            """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                        [||]if (b)
                        {
                            if (c)
                            {
                                System.Console.WriteLine();
                            }
                        }
                    }
                }
            }
            """;
        await TestActionCountAsync(Initial, 2);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a && b)
                    {
                        if (c)
                        {
                            System.Console.WriteLine();
                        }
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(Initial, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                        if (b && c)
                        {
                            System.Console.WriteLine();
                        }
                    }
                }
            }
            """, index: 1);
    }
}
