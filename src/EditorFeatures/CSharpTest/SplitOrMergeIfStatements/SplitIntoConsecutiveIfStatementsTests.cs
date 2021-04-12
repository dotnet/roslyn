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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoConsecutiveIfStatements)]
    public sealed class SplitIntoConsecutiveIfStatementsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider();

        [Theory]
        [InlineData("a [||]|| b")]
        [InlineData("a |[||]| b")]
        [InlineData("a ||[||] b")]
        [InlineData("a [||||] b")]
        public async Task SplitOnOrOperatorSpans(string condition)
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (" + condition + @")
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else if (b)
        {
        }
    }
}");
        }

        [Theory]
        [InlineData("a [|||]| b")]
        [InlineData("a[| |||] b")]
        [InlineData("a[||] || b")]
        public async Task NotSplitOnOrOperatorSpans(string condition)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (" + condition + @")
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitOnIfKeyword()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        [||]if (a || b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitOnAndOperator()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]&& b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitOnBitwiseOrOperator()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]| b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitOnOrOperatorOutsideIfStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        var v = a [||]|| b;
    }
}");
        }

        [Fact]
        public async Task NotSplitOnOrOperatorInIfStatementBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a || b)
            a [||]|| b;
    }
}");
        }

        [Fact]
        public async Task SplitWithChainedOrExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a [||]|| b || c || d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a)
        {
        }
        else if (b || c || d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithChainedOrExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b [||]|| c || d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b)
        {
        }
        else if (c || d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithChainedOrExpression3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b || c [||]|| d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b || c)
        {
        }
        else if (d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitInsideParentheses1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((a [||]|| b) || c || d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitInsideParentheses2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || b || (c [||]|| d))
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitInsideParentheses3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if ((a || b [||]|| c || d))
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithOtherExpressionInsideParentheses1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a [||]|| (b || c) || d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a)
        {
        }
        else if ((b || c) || d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithOtherExpressionInsideParentheses2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || (b || c) [||]|| d)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c, bool d)
    {
        if (a || (b || c))
        {
        }
        else if (d)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithMixedAndExpression1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a [||]|| b && c)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
        {
        }
        else if (b && c)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithMixedAndExpression2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a && b [||]|| c)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a && b)
        {
        }
        else if (c)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitWithMixedConditionalExpression1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a [||]|| b ? c : c)
        {
        }
    }
}");
        }

        [Fact]
        public async Task NotSplitWithMixedConditionalExpression2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a ? b : b [||]|| c)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithMixedConditionalExpressionInsideParentheses1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a [||]|| (b ? c : c))
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if (a)
        {
        }
        else if ((b ? c : c))
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithMixedConditionalExpressionInsideParentheses2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if ((a ? b : b) [||]|| c)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b, bool c)
    {
        if ((a ? b : b))
        {
        }
        else if (c)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithStatementInsideBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
        {
            System.Console.WriteLine(a || b);
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            System.Console.WriteLine(a || b);
        }
        else if (b)
        {
            System.Console.WriteLine(a || b);
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithStatementWithoutBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            System.Console.WriteLine(a || b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine(a || b);
        else if (b)
            System.Console.WriteLine(a || b);
    }
}");
        }

        [Fact]
        public async Task SplitWithNestedIfStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            if (true) { }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (true) { }
        }
        else if (b)
            if (true) { }
    }
}");
        }

        [Fact]
        public async Task SplitWithNestedIfStatementInWhileLoop()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            while (a)
                if (true)
                    using (null) { }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            while (a)
                if (true)
                    using (null) { }
        }
        else if (b)
            while (a)
                if (true)
                    using (null) { }
    }
}");
        }

        [Fact]
        public async Task SplitWithNestedIfStatementInsideBlockInWhileLoop()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            while (a)
            {
                if (true)
                    using (null) { }
            }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            while (a)
            {
                if (true)
                    using (null) { }
            }
        else if (b)
            while (a)
            {
                if (true)
                    using (null) { }
            }
    }
}");
        }

        [Fact]
        public async Task SplitWithNestedIfStatementInsideBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
        {
            if (true) { }
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (true) { }
        }
        else if (b)
        {
            if (true) { }
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithMissingStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
else if (b)
    }
}");
        }

        [Fact]
        public async Task SplitWithElseStatementInsideBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
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
        if (a)
            System.Console.WriteLine();
        else if (b)
            System.Console.WriteLine();
        else
        {
            System.Console.WriteLine(a || b);
        }
    }
}");
        }

        [Fact]
        public async Task SplitWithElseStatementWithoutBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            System.Console.WriteLine();
        else
            System.Console.WriteLine(a || b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine();
        else if (b)
            System.Console.WriteLine();
        else
            System.Console.WriteLine(a || b);
    }
}");
        }

        [Fact]
        public async Task SplitWithElseNestedIfStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            System.Console.WriteLine();
        else if (true) { }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine();
        else if (b)
            System.Console.WriteLine();
        else if (true) { }
    }
}");
        }

        [Fact]
        public async Task SplitWithElseIfElse()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
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
        if (a)
            System.Console.WriteLine();
        else if (b)
            System.Console.WriteLine();
        else if (a)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}");
        }

        [Fact]
        public async Task SplitAsPartOfElseIfElse()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (true)
            System.Console.WriteLine();
        else if (a [||]|| b)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (true)
            System.Console.WriteLine();
        else if (a)
            System.Console.WriteLine(a);
        else if (b)
            System.Console.WriteLine(a);
        else
            System.Console.WriteLine(b);
    }
}");
        }

        [Fact]
        public async Task SplitWithMissingElseStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            System.Console.WriteLine();
        else
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            System.Console.WriteLine();
        else if (b)
            System.Console.WriteLine();
        else
    }
}");
        }

        [Fact]
        public async Task SplitWithPreservedSingleLineFormatting()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b) System.Console.WriteLine();
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a) System.Console.WriteLine();
        else if (b) System.Console.WriteLine();
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsIfControlFlowQuits1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        if (b)
            return;
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsIfControlFlowQuits2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            throw new System.Exception();
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            throw new System.Exception();
        if (b)
            throw new System.Exception();
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsIfControlFlowQuits3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (true)
        {
            if (a [||]|| b)
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
            if (a)
                continue;
            if (b)
                continue;
        }
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsIfControlFlowQuits4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (true)
        {
            if (a [||]|| b)
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
            if (a)
            {
                if (a)
                    continue;
                else
                    break;
            }

            if (b)
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
        public async Task SplitIntoSeparateStatementsIfControlFlowQuits5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (true)
        {
            if (a [||]|| b)
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
            if (a)
                switch (a)
                {
                    default:
                        continue;
                }

            if (b)
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
        public async Task SplitIntoSeparateStatementsIfControlFlowQuitsInSwitchSection()
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
                if (a [||]|| b)
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
                if (a)
                    break;
                if (b)
                    break;
                break;
        }
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsIfControlFlowQuitsWithNestedIfStatement()
        {
            // No need to create a block if we're not adding an else clause.
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            if (true)
                return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            if (true)
                return;
        if (b)
            if (true)
                return;
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsIfControlFlowQuitsWithPreservedSingleLineFormatting()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b) return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a) return;
        if (b) return;
    }
}");
        }

        [Fact]
        public async Task SplitNotIntoSeparateStatementsIfControlFlowContinues1()
        {
            // Even though there are no statements inside, we still can't split this into separate statements
            // because it would change the semantics from short-circuiting to always evaluating the second condition,
            // breaking code like 'if (a == null || a.InstanceMethod())'.
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
        {
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        else if (b)
        {
        }
    }
}");
        }

        [Fact]
        public async Task SplitNotIntoSeparateStatementsIfControlFlowContinues2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
        {
            if (a)
                return;
        }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
            if (a)
                return;
        }
        else if (b)
        {
            if (a)
                return;
        }
    }
}");
        }

        [Fact]
        public async Task SplitNotIntoSeparateStatementsIfControlFlowContinues3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            while (a)
            {
                break;
            }
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            while (a)
            {
                break;
            }
        else if (b)
            while (a)
            {
                break;
            }
    }
}");
        }

        [Fact]
        public async Task SplitNotIntoSeparateStatementsIfControlFlowContinues4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (true)
        {
            if (a [||]|| b)
                switch (a)
                {
                    default:
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
            if (a)
                switch (a)
                {
                    default:
                        break;
                }
            else if (b)
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
        public async Task SplitNotIntoSeparateStatementsWithElseIfControlFlowQuits()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a [||]|| b)
            return;
        else
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        else if (b)
            return;
        else
            return;
    }
}");
        }

        [Fact]
        public async Task SplitIntoSeparateStatementsAsEmbeddedStatementIfControlFlowQuits()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        while (a)
            if (a [||]|| b)
                return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        while (a)
        {
            if (a)
                return;
            if (b)
                return;
        }
    }
}");
        }

        [Fact]
        public async Task SplitNotIntoSeparateStatementsAsElseIfIfControlFlowQuits()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        else if (a [||]|| b)
            return;
    }
}",
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
            return;
        else if (a)
            return;
        else if (b)
            return;
    }
}");
        }
    }
}
