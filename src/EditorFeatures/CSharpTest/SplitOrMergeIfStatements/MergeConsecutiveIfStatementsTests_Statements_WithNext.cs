// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements
{
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
        public async Task MergedIntoNextStatementOnIfSpans(string ifLine)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        " + ifLine + @"
            return;
        if (b)
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
    }
}");
        }

        [Fact]
        public async Task MergedIntoNextStatementOnIfExtendedHeaderSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
[|        if (a)
            return;
|]        if (b)
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
    }
}");
        }

        [Fact]
        public async Task MergedIntoNextStatementOnIfFullSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [|if (a)
            return;|]
        if (b)
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
    }
}");
        }

        [Fact]
        public async Task MergedIntoNextStatementOnIfExtendedFullSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
[|        if (a)
            return;
|]        if (b)
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
    }
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
        public async Task NotMergedIntoNextStatementOnIfSpans(string ifLine)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        " + ifLine + @"
            return;
        if (b)
            return;
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoNextStatementOnIfOverreachingSelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [|if (a)
        |]    return;
        if (b)
          return;
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoNextStatementOnIfBodySelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            [|return;|]
        if (b)
            return;
    }
}");
        }
    }
}
