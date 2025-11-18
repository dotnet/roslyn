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

[Trait(Traits.Feature, Traits.Features.CodeActionsMergeConsecutiveIfStatements)]
public sealed partial class MergeConsecutiveIfStatementsTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpMergeConsecutiveIfStatementsCodeRefactoringProvider();

    [Theory]
    [InlineData("[||]else if (b)")]
    [InlineData("el[||]se if (b)")]
    [InlineData("else[||] if (b)")]
    [InlineData("else [||]if (b)")]
    [InlineData("else i[||]f (b)")]
    [InlineData("else if[||] (b)")]
    [InlineData("else if [||](b)")]
    [InlineData("else if (b)[||]")]
    [InlineData("else [|if|] (b)")]
    [InlineData("else [|if (b)|]")]
    [InlineData("[|else if (b)|]")]
    public Task MergedOnElseIfSpans(string elseIfLine)
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    
            """ + elseIfLine + """

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
    public Task MergedOnElseIfExtendedHeaderSelection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
            [|        else if (b)
            |]        {
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
    public Task MergedOnElseIfFullSelectionWithoutElseClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else [|if (b)
                    {
                    }|]
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
    public Task MergedOnElseIfExtendedFullSelectionWithoutElseClause()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
            [|        else if (b)
                    {
                    }
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
                    {
                    }
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfFullSelectionWithElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else [|if (b)
                    {
                    }
                    else
                    {
                    }|]
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfExtendedFullSelectionWithElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
            [|        else if (b)
                    {
                    }
                    else
                    {
                    }
            |]    }
            }
            """);

    [Theory]
    [InlineData("else if ([||]b)")]
    [InlineData("[|else|] if (b)")]
    [InlineData("[|else if|] (b)")]
    [InlineData("else [|i|]f (b)")]
    [InlineData("else [|if (|]b)")]
    [InlineData("else if [|(|]b)")]
    [InlineData("else if (b[|)|]")]
    [InlineData("else if ([|b|])")]
    [InlineData("else if [|(b)|]")]
    public Task NotMergedOnElseIfSpans(string elseIfLine)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    
            """ + elseIfLine + """

                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfOverreachingSelection1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else [|if (b)
                    |]{
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfOverreachingSelection2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    [|else if (b)
                    {|]
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfBodySelection()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else if (b)
                    [|{
                    }|]
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfBodyCaret1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                    }
                    else if (b)
                    [||]{
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedOnElseIfBodyCaret2()
        => TestMissingInRegularAndScriptAsync(
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
                    }[||]
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
    public Task MergedWithOrExpressions()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b)
                    {
                    }
                    else [||]if (c || d)
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
                    if (a || b || c || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithAndExpressionNotParenthesized1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b)
                    {
                    }
                    else [||]if (c || d)
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
                    if (a && b || c || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithAndExpressionNotParenthesized2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b)
                    {
                    }
                    else [||]if (c && d)
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
                    if (a || b || c && d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithConditionalExpressionParenthesized1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (true ? a : b)
                    {
                    }
                    else [||]if (c == d)
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
                    if ((true ? a : b) || c == d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedWithConditionalExpressionParenthesized2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a == b)
                    {
                    }
                    else [||]if (true ? c : d)
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
                    if (a == b || (true ? c : d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a || b);
                    }
                    else [||]if (b)
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
                    if (a || b)
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine(a || b);
                    else [||]if (b)
                        System.Console.WriteLine(a || b);
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        System.Console.WriteLine(a || b);
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithDifferenceInBlocks1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine(a || b);
                    else [||]if (b)
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
                    if (a || b)
                        System.Console.WriteLine(a || b);
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithDifferenceInBlocks2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a || b);
                    }
                    else [||]if (b)
                        System.Console.WriteLine(a || b);
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
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithDifferenceInBlocks3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a || b);
                    }
                    else [||]if (b)
                    {
                        {
                            System.Console.WriteLine(a || b);
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
                    if (a || b)
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoParentWithUnmatchingStatements1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a || b);
                    }
                    else [||]if (b)
                    {
                        System.Console.WriteLine(a || a);
                    }
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoParentWithUnmatchingStatements2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine(a || b);
                    else [||]if (b)
                        System.Console.WriteLine(a || a);
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoParentWithUnmatchingStatements3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a);
                    }
                    else [||]if (b)
                        System.Console.WriteLine(b);
                }
            }
            """);

    [Fact]
    public Task NotMergedIntoParentWithUnmatchingStatements4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        System.Console.WriteLine(a);
                    }
                    else [||]if (b)
                        using (null)
                            System.Console.WriteLine(a);
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithElseStatementInsideBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else [||]if (b)
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
                    if (a || b)
                        System.Console.WriteLine();
                    else
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithElseStatementWithoutBlock()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else [||]if (b)
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
                    if (a || b)
                        System.Console.WriteLine();
                    else
                        System.Console.WriteLine(a || b);
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithElseNestedIfStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else [||]if (b)
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
                    if (a || b)
                        System.Console.WriteLine();
                    else if (true) { }
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentWithElseIfElse()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else [||]if (b)
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
                    if (a || b)
                        System.Console.WriteLine();
                    else if (a)
                        System.Console.WriteLine(a);
                    else
                        System.Console.WriteLine(b);
                }
            }
            """);

    [Fact]
    public Task MergedIntoParentPartOfElseIf()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine(a);
                    else [||]if (a)
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
                    else if (b || a)
                        System.Console.WriteLine(a);
                    else
                        System.Console.WriteLine(b);
                }
            }
            """);

    [Fact]
    public Task MergedWithPreservedSingleLineFormatting()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a) System.Console.WriteLine();
                    else [||]if (b) System.Console.WriteLine();
                }
            }
            """,
            """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b) System.Console.WriteLine();
                }
            }
            """);
}
