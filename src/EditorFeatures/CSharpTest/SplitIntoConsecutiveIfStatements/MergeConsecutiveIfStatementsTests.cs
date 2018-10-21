// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.SplitIntoConsecutiveIfStatements;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitIntoConsecutiveIfStatements
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMergeConsecutiveIfStatements)]
    public sealed class MergeConsecutiveIfStatementsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpMergeConsecutiveIfStatementsCodeRefactoringProvider();

        [Fact]
        public async Task MergedOnElseIfCaret1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else [||]if (b)
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
        public async Task MergedOnElseIfCaret2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else i[||]f (b)
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
        public async Task MergedOnElseIfCaret3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else if[||] (b)
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
        public async Task MergedOnElseIfCaret4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else if [||](b)
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
        public async Task MergedOnElseIfCaret5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else if (b)[||]
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
        public async Task MergedOnElseIfSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else [|if|] (b)
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
        public async Task NotMergedOnElseIfPartialSelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else [|i|]f (b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotMergedOnElseIfOverreachingSelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else [|if |](b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotMergedOnElseIfConditionCaret()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else if ([||]b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotMergedOnParentIf()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [||]if (a)
        {
        }
        else if (b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotMergedOnSingleIf()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [||]if (b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithOrExpressions()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b || c || d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithAndExpressionNotParenthesized1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a && b || c || d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithAndExpressionNotParenthesized2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b || c && d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithConditionalExpressionParenthesized1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((true ? a : b) || c == d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithConditionalExpressionParenthesized2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a == b || (true ? c : d))
        {
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithStatementInsideBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
        {
            System.Console.WriteLine(a || b);
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithStatementWithoutBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine(a || b);
        else [||]if (b)
            System.Console.WriteLine(a || b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine(a || b);
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithDifferenceInBlocks1()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine(a || b);
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithDifferenceInBlocks2()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
        {
            System.Console.WriteLine(a || b);
        }
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithDifferenceInBlocks3()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
        {
            System.Console.WriteLine(a || b);
        }
    }
}");
        }

        [Fact]
        public async Task NotMergedWithParentWithUnmatchingStatements1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedWithParentWithUnmatchingStatements2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine(a || b);
        else [||]if (b)
            System.Console.WriteLine(a || a);
    }
}");
        }

        [Fact]
        public async Task NotMergedWithParentWithUnmatchingStatements3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedWithParentWithUnmatchingStatements4()
        {
            // Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task MergedWithParentWithElseStatementInsideBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedWithParentWithElseStatementWithoutBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else
            System.Console.WriteLine(a || b);
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithElseNestedIfStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine();
        else [||]if (b)
            System.Console.WriteLine();
        else if (true) { }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            System.Console.WriteLine();
        else if (true) { }
    }
}");
        }

        [Fact]
        public async Task MergedWithParentWithElseIfElse()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedWithParentPartOfElseIf()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task MergedWithPreviousStatementIfContainsNoStatements()
        {
            await TestInRegularAndScriptAsync(
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
        public async Task MergedWithPreviousStatementIfControlFlowQuits1()
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
        public async Task MergedWithPreviousStatementIfControlFlowQuits2()
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
        public async Task MergedWithPreviousStatementIfControlFlowQuits3()
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
        public async Task MergedWithPreviousStatementIfControlFlowQuits4()
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
        public async Task MergedWithPreviousStatementIfControlFlowQuits5()
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
        public async Task MergedWithPreviousStatementIfControlFlowQuitsInSwitchSection()
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
        public async Task MergedWithPreviousStatementIfControlFlowQuitsWithDifferenceInBlocks()
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
        public async Task NotMergedWithPreviousStatementIfControlFlowContinues1()
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
        public async Task NotMergedWithPreviousStatementIfControlFlowContinues2()
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
        public async Task NotMergedWithPreviousStatementIfControlFlowContinues3()
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
        public async Task NotMergedWithPreviousStatementIfControlFlowContinues4()
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
        public async Task NotMergedWithPreviousStatementWithUnmatchingStatementsIfControlFlowQuits()
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
        public async Task NotMergedWithPreviousStatementWithElseClauseIfControlFlowQuits1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;

        [||]if (b)
            return;
        else
            return;
    }
}");
        }

        [Fact]
        public async Task NotMergedWithPreviousStatementWithElseClauseIfControlFlowQuits2()
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
        public async Task NotMergedWithPreviousStatementWithElseClauseIfControlFlowQuits3()
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
        public async Task NotMergedWithPreviousStatementAsEmbeddedStatementIfControlFlowQuits1()
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
        public async Task NotMergedWithPreviousStatementAsEmbeddedStatementIfControlFlowQuits2()
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
