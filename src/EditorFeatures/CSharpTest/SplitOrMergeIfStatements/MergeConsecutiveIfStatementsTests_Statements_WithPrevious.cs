// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements
{
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
        public async Task MergedIntoPreviousStatementOnIfSpans(string ifLine)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        " + ifLine + @"
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
        public async Task MergedIntoPreviousStatementOnIfExtendedHeaderSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
[|        if (b)
|]            return;
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
        public async Task MergedIntoPreviousStatementOnIfFullSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        [|if (b)
            return;|]
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
        public async Task MergedIntoPreviousStatementOnIfExtendedFullSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
[|        if (b)
            return;
|]    }
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
        public async Task MergedIntoPreviousStatementOnIfFullSelectionWithoutElseClause()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
        else
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementOnIfExtendedFullSelectionWithoutElseClause()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
        else
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementOnIfFullSelectionWithElseClause()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementOnIfExtendedFullSelectionWithElseClause()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Theory]
        [InlineData("if ([||]b)")]
        [InlineData("[|i|]f (b)")]
        [InlineData("[|if (|]b)")]
        [InlineData("if [|(|]b)")]
        [InlineData("if (b[|)|]")]
        [InlineData("if ([|b|])")]
        [InlineData("if [|(b)|]")]
        public async Task NotMergedIntoPreviousStatementOnIfSpans(string ifLine)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        " + ifLine + @"
            return;
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementOnIfOverreachingSelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        [|if (b)
          |]return;
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementOnIfBodySelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        if (b)
            [|return;|]
    }
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuits1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        [||]if (b)
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
        public async Task MergedIntoPreviousStatementIfControlFlowQuits2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            throw new System.Exception();
        [||]if (b)
            throw new System.Exception();
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            throw new System.Exception();
    }
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuits3()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        while (true)
        {
            if (a || b)
                continue;
        }
    }
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuits4()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuits5()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuitsInSwitchSection()
        {
            // Switch sections are interesting in that they are blocks of statements that aren't BlockSyntax.
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuitsWithDifferenceInBlocks()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIncludingElseClauseIfControlFlowQuits()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
        else
            System.Console.WriteLine();
    }
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIncludingElseIfClauseIfControlFlowQuits()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            return;
        else if (a && b)
            System.Console.WriteLine();
    }
}");
        }

        [Fact]
        public async Task MergedIntoPreviousStatementIfControlFlowQuitsWithPreservedSingleLineFormatting()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a) return;
        [||]if (b) return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b) return;
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementIfControlFlowContinues1()
        {
            // Even though there are no statements inside, we still can't merge these into one statement
            // because it would change the semantics from always evaluating the second condition to short-circuiting.
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementIfControlFlowContinues2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine();
        [||]if (b)
            System.Console.WriteLine();
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementIfControlFlowContinues3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementIfControlFlowContinues4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementIfControlFlowContinues5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementWithUnmatchingStatementsIfControlFlowQuits()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        [||]if (b)
            throw new System.Exception();
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementThatHasElseClauseIfControlFlowQuits1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementThatHasElseClauseIfControlFlowQuits2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementAsEmbeddedStatementIfControlFlowQuits1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;

        while (a)
            [||]if (b)
                return;
    }
}");
        }

        [Fact]
        public async Task NotMergedIntoPreviousStatementAsEmbeddedStatementIfControlFlowQuits2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }
    }
}
