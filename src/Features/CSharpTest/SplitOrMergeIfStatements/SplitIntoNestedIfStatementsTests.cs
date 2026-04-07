// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitOrMergeIfStatements;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpSplitIntoNestedIfStatementsCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoNestedIfStatements)]
public sealed class SplitIntoNestedIfStatementsTests
{
    [Theory]
    [InlineData("a [||]&& b")]
    [InlineData("a &[||]& b")]
    [InlineData("a &&[||] b")]
    [InlineData("a [|&&|] b")]
    public Task SplitOnAndOperatorSpans(string condition)
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
                        if (b)
                        {
                        }
                    }
                }
            }
            """);

    [Theory]
    [InlineData("a [|&|]& b")]
    [InlineData("a[| &&|] b")]
    [InlineData("a[||] && b")]
    public Task NotSplitOnAndOperatorSpans(string condition)
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
                    [||]if (a && b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnOrOperator()
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
            """);

    [Fact]
    public Task NotSplitOnBitwiseAndOperator()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]& b)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitOnAndOperatorOutsideIfStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    var v = a [||]&& b;
                }
            }
            """);

    [Fact]
    public Task NotSplitOnAndOperatorInIfStatementBody()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a && b)
                        {|CS0201:a [||]&& b|};
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedAndExpression1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a [||]&& b && c && d)
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
                        if (b && c && d)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedAndExpression2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b [||]&& c && d)
                    {
                    }
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b)
                    {
                        if (c && d)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithChainedAndExpression3()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b && c [||]&& d)
                    {
                    }
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && b && c)
                    {
                        if (d)
                        {
                        }
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
                    if ((a [||]&& b) && c && d)
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
                    if (a && b && (c [||]&& d))
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
                    if ((a && b [||]&& c && d))
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
                    if (a [||]&& (b && c) && d)
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
                        if ((b && c) && d)
                        {
                        }
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
                    if (a && (b && c) [||]&& d)
                    {
                    }
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b, bool c, bool d)
                {
                    if (a && (b && c))
                    {
                        if (d)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedOrExpression1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]&& b || c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task NotSplitWithMixedOrExpression2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a || b [||]&& c)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedOrExpressionInsideParentheses1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]&& (b || c))
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
                        if ((b || c))
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedOrExpressionInsideParentheses2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if ((a || b) [||]&& c)
                    {
                    }
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if ((a || b))
                    {
                        if (c)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedBitwiseOrExpression1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a [||]&& b | c)
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
                        if (b | c)
                        {
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMixedBitwiseOrExpression2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a | b [||]&& c)
                    {
                    }
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b, bool c)
                {
                    if (a | b)
                    {
                        if (c)
                        {
                        }
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
                    if (a [||]&& b)
                    {
                        System.Console.WriteLine(a && b);
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
                        if (b)
                        {
                            System.Console.WriteLine(a && b);
                        }
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
                    if (a [||]&& b)
                        System.Console.WriteLine(a && b);
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (b)
                            System.Console.WriteLine(a && b);
                    }
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
                    if (a [||]&& b)
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
                        if (b)
                            if (true) { }
                    }
                }
            }
            """);

    [Fact]
    public Task SplitWithMissingStatement()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]&& b){|CS1002:|}{|CS1525:|}
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (b){|CS1002:|}{|CS1525:|}
            }
                }
            }
            """);

    [Fact]
    public Task SplitWithElseStatementInsideBlock()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a [||]&& b)
                        System.Console.WriteLine();
                    else
                    {
                        System.Console.WriteLine(a && b);
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
                        if (b)
                            System.Console.WriteLine();
                        else
                        {
                            System.Console.WriteLine(a && b);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine(a && b);
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
                    if (a [||]&& b)
                        System.Console.WriteLine();
                    else
                        System.Console.WriteLine(a && b);
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (b)
                            System.Console.WriteLine();
                        else
                            System.Console.WriteLine(a && b);
                    }
                    else
                        System.Console.WriteLine(a && b);
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
                    if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine();
                        else if (true) { }
                    }
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
                    if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine();
                        else if (a)
                            System.Console.WriteLine(a);
                        else
                            System.Console.WriteLine(b);
                    }
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
                    else if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine(a);
                        else
                            System.Console.WriteLine(b);
                    }
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
                    if (a [||]&& b)
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
                    {
                        if (b)
                            System.Console.WriteLine();
                        else{|CS1002:|}{|CS1525:|}
            }
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
                    if (a [||]&& b) System.Console.WriteLine();
                }
            }
            """, """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (b) System.Console.WriteLine();
                    }
                }
            }
            """);

    [Fact]
    public Task SplitIntoNestedIfStatements_TopLevelStatement()
        => new VerifyCS.Test
        {
            TestCode = """
                var a = true;
                var b = true;

                if (a [||]&& b)
                {
                }
                """,
            FixedCode = """
                var a = true;
                var b = true;

                if (a)
                {
                    if (b)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication }
        }.RunAsync();
}
