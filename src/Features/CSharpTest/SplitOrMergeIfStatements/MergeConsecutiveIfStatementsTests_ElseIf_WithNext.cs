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
    public async Task MergedOnIfSpans(string ifLine)
    {
        await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        " + ifLine + @"
        {
        }
        else if (b)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedOnIfExtendedHeaderSelection()
    {
        await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task MergedOnIfFullSelectionWithoutElseIfClause()
    {
        await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
    }

    [Fact]
    public async Task MergedOnIfExtendedFullSelectionWithoutElseIfClause()
    {
        await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfFullSelectionWithElseIfClause()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfExtendedFullSelectionWithElseIfClause()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfFullSelectionWithElseIfElseClauses()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfExtendedFullSelectionWithElseIfElseClauses()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Theory]
    [InlineData("if ([||]a)")]
    [InlineData("[|i|]f (a)")]
    [InlineData("[|if (|]a)")]
    [InlineData("if [|(|]a)")]
    [InlineData("if (a[|)|]")]
    [InlineData("if ([|a|])")]
    [InlineData("if [|(a)|]")]
    public async Task NotMergedOnIfSpans(string ifLine)
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        " + ifLine + @"
        {
        }
        else if (b)
        {
        }
    }
}");
    }

    [Fact]
    public async Task NotMergedOnIfOverreachingSelection1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfOverreachingSelection2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfBodySelection()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfBodyCaret1()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }

    [Fact]
    public async Task NotMergedOnIfBodyCaret2()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
    }
}
