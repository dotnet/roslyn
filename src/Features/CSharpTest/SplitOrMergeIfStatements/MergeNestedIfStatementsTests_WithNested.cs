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
    public async Task MergedOnOuterIf()
    {
        await TestInRegularAndScriptAsync(
@"class C
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55563")]
    public async Task MergedOnOuterIf_TopLevelStatements()
    {
        await TestInRegularAndScriptAsync(
@"var a = true;
var b = true;

[||]if (a)
{
    if (b)
    {
    }
}
",
@"var a = true;
var b = true;

if (a && b)
{
}
");
    }

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
    public async Task MergedOnOuterElseIfSpans(string elseIfLine)
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (true)
        {
        }
        " + elseIfLine + @"
        {
            if (b)
            {
            }
        }
    }
}",
@"class C
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
}");
    }

    [Fact]
    public async Task MergedOnOuterElseIfExtendedHeaderSelection()
    {
        await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
    }

    [Fact]
    public async Task MergedOnOuterElseIfFullSelection()
    {
        await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
    }

    [Fact]
    public async Task MergedOnOuterElseIfFullSelectionWithElseClause()
    {
        await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfFullSelectionWithoutElseClause()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfFullSelectionWithParentIf()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

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
    public async Task NotMergedOnOuterElseIfSpans(string elseIfLine)
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (true)
        {
        }
        " + elseIfLine + @"
        {
            if (b)
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfOverreachingSelection1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfOverreachingSelection2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfBodySelection()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfBodyCaret1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnOuterElseIfBodyCaret2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task MergedOnMiddleIfMergableWithNestedOnly()
    {
        const string Initial =
@"class C
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
}";
        const string Expected =
@"class C
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
}";

        await TestActionCountAsync(Initial, 1);
        await TestInRegularAndScriptAsync(Initial, Expected);
    }

    [Fact]
    public async Task MergedOnMiddleIfMergableWithOuterOnly()
    {
        const string Initial =
@"class C
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
}";
        const string Expected =
@"class C
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
}";

        await TestActionCountAsync(Initial, 1);
        await TestInRegularAndScriptAsync(Initial, Expected);
    }

    [Fact]
    public async Task MergedOnMiddleIfMergableWithBoth()
    {
        const string Initial =
@"class C
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
}";
        const string Expected1 =
@"class C
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
}";
        const string Expected2 =
@"class C
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
}";

        await TestActionCountAsync(Initial, 2);
        await TestInRegularAndScriptAsync(Initial, Expected1, index: 0);
        await TestInRegularAndScriptAsync(Initial, Expected2, index: 1);
    }
}
