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
    [Trait(Traits.Feature, Traits.Features.CodeActionsMergeConsecutiveIfStatements)]
    public sealed partial class MergeConsecutiveIfStatementsTests : AbstractCSharpCodeActionTest
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
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        " + elseIfLine + @"
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
@"class C
{
    void M(bool a, bool b)
    {
        if (a)
        {
        }
        " + elseIfLine + @"
        {
        }
    }
}");
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
    }
}
