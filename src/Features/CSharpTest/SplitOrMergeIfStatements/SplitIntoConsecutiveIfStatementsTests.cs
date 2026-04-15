// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpSplitIntoConsecutiveIfStatementsCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoConsecutiveIfStatements)]
public sealed class SplitIntoConsecutiveIfStatementsTests
{
    [Theory]
    [InlineData("a [||]|| b")]
    [InlineData("a |[||]| b")]
    [InlineData("a ||[||] b")]
    [InlineData("a [||||] b")]
    public Task SplitOnOrOperatorSpans(string condition)
        => VerifyCS.VerifyRefactoringAsync($$"""
            class C
            {
                void M(bool a, bool b)
                {
                    if ({{condition}})
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Theory]
    [InlineData("a [|||]| b")]
    [InlineData("a[| |||] b")]
    [InlineData("a[||] || b")]
    public Task NotSplitOnOrOperatorSpans(string condition)
        => VerifyCS.VerifyRefactoringAsync($$"""
            class C
            {
                void M(bool a, bool b)
                {
                    if ({{condition}})
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnIfKeyword()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    [||]if (a || b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnAndOperator()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]&& b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnBitwiseOrOperator()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]| b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnOrOperatorOutsideIfStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    var v = a [||]|| b;
                }
            }
            """);

    [Fact]
    public Task NotSplitOnOrOperatorInIfStatementBody()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a || b)
                        {|CS0201:a [||]|| b|};
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedOrExpression1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a [||]|| b || c || d)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithChainedOrExpression2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b [||]|| c || d)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithChainedOrExpression3()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b || c [||]|| d)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task NotSplitInsideParentheses1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if ((a [||]|| b) || c || d)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitInsideParentheses2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || b || (c [||]|| d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitInsideParentheses3()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if ((a || b [||]|| c || d))
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithOtherExpressionInsideParentheses1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a [||]|| (b || c) || d)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithOtherExpressionInsideParentheses2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a || (b || c) [||]|| d)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithMixedAndExpression1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]|| b && c)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithMixedAndExpression2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a && b [||]|| c)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task NotSplitWithMixedConditionalExpression1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]|| b ? c : c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedConditionalExpression2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a ? b : b [||]|| c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedConditionalExpressionInsideParentheses1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]|| (b ? c : c))
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithMixedConditionalExpressionInsideParentheses2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if ((a ? b : b) [||]|| c)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithStatementInsideBlock()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                        System.Console.WriteLine(a || b);
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithStatementWithoutBlock()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine(a || b);
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine(a || b);
                    else if (b)
                        System.Console.WriteLine(a || b);
                }
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        if (true) { }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatementInWhileLoop()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        while (a)
                            if (true)
                                using (null) { }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatementInsideBlockInWhileLoop()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithNestedIfStatementInsideBlock()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                        if (true) { }
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithMissingStatement()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(bool a, bool b)
                    {
                        if (a [||]|| b)
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(bool a, bool b)
                    {
                        if (a)
                else if (b)
                    }
                }
                """,
            CompilerDiagnostics = CompilerDiagnostics.None,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();

    [Fact]
    public Task SplitWithElseStatementInsideBlock()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithElseStatementWithoutBlock()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else
                        System.Console.WriteLine(a || b);
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithElseNestedIfStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else if (true) { }
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else if (true) { }
                }
            }
            """);

    [Fact]
    public Task SplitWithElseIfElse()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitAsPartOfElseIfElse()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitWithMissingElseStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        System.Console.WriteLine();
                    else{|CS1002:|}{|CS1525:|}
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        System.Console.WriteLine();
                    else if (b)
                        System.Console.WriteLine();
                    else{|CS1002:|}{|CS1525:|}
                }
            }
            """);

    [Fact]
    public Task SplitWithPreservedSingleLineFormatting()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b) System.Console.WriteLine();
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a) System.Console.WriteLine();
                    else if (b) System.Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        return;
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    if (b)
                        return;
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        throw new System.Exception();
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        throw new System.Exception();
                    if (b)
                        throw new System.Exception();
                }
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits3()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    while (true)
                    {
                        if (a [||]|| b)
                            continue;
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits4()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuits5()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuitsInSwitchSection()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuitsWithNestedIfStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        if (true)
                            return;
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsIfControlFlowQuitsWithPreservedSingleLineFormatting()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b) return;
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a) return;
                    if (b) return;
                }
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                    {
                        if (a)
                            return;
                    }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues3()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        while (a)
                        {
                            break;
                        }
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsIfControlFlowContinues4()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
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
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsWithElseIfControlFlowQuits()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]|| b)
                        return;
                    else
                        return;
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoSeparateStatementsAsEmbeddedStatementIfControlFlowQuits()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    while (a)
                        if (a [||]|| b)
                            return;
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitNotIntoSeparateStatementsAsElseIfIfControlFlowQuits()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                        return;
                    else if (a [||]|| b)
                        return;
                }
            }
            """, """
            class C
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
            }
            """);

    [Fact]
    public Task SplitIntoConsecutiveIfStatements_TopLevelStatement()
        => new VerifyCS.Test
        {
            TestCode = """
                var a = true;
                var b = true;

                if (a [||]|| b)
                {
                }
                """,
            FixedCode = """
                var a = true;
                var b = true;

                if (a)
                {
                }
                else if (b)
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication }
        }.RunAsync();
}
