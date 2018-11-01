// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMergeConsecutiveIfStatements)]
    public sealed class MergeConsecutiveIfStatementsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
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
        public async Task MergedOnElseIfSpans(string elseIfLine)
        {
            await TestInRegularAndScriptAsync(
$@"class C
{{
    void M(bool a, bool b)
    {{
        if (a)
        {{
        }}
        {elseIfLine}
        {{
        }}
    }}
}}",
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
        public async Task MergedOnElseIfExtendedHeaderSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
        public async Task MergedOnElseIfFullSelectionWithoutElseClause()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
        public async Task MergedOnElseIfExtendedFullSelectionWithoutElseClause()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
        public async Task NotMergedOnElseIfFullSelectionWithElseClause()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedOnElseIfExtendedFullSelectionWithElseClause()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

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
        public async Task NotMergedOnElseIfSpans(string elseIfLine)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class C
{{
    void M(bool a, bool b)
    {{
        if (a)
        {{
        }}
        {elseIfLine}
        {{
        }}
    }}
}}");
        }

        [Fact]
        public async Task NotMergedOnElseIfOverreachingSelection1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedOnElseIfOverreachingSelection2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }
        
        [Fact]
        public async Task NotMergedOnElseIfBodySelection()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedOnElseIfBodyCaret1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
}");
        }

        [Fact]
        public async Task NotMergedOnElseIfBodyCaret2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
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
        public async Task MergedIntoParentWithStatementInsideBlock()
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
        public async Task MergedIntoParentWithStatementWithoutBlock()
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
        public async Task MergedIntoParentWithDifferenceInBlocks1()
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
        public async Task MergedIntoParentWithDifferenceInBlocks2()
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
        public async Task MergedIntoParentWithDifferenceInBlocks3()
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
        public async Task NotMergedIntoParentWithUnmatchingStatements1()
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
        public async Task NotMergedIntoParentWithUnmatchingStatements2()
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
        public async Task NotMergedIntoParentWithUnmatchingStatements3()
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
        public async Task NotMergedIntoParentWithUnmatchingStatements4()
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
        public async Task MergedIntoParentWithElseStatementInsideBlock()
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
        public async Task MergedIntoParentWithElseStatementWithoutBlock()
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
        public async Task MergedIntoParentWithElseNestedIfStatement()
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
        public async Task MergedIntoParentWithElseIfElse()
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
        public async Task MergedIntoParentPartOfElseIf()
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
        public async Task MergedWithPreservedSingleLineFormatting()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a) System.Console.WriteLine();
        else [||]if (b) System.Console.WriteLine();
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b) System.Console.WriteLine();
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
