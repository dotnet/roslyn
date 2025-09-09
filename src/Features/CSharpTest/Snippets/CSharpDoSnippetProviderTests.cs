// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpDoSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "do";

    [Fact]
    public Task InsertDoSnippetInMethodTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    do
                    {
                        $$
                    }
                    while ({|0:true|});
                }
            }
            """);

    [Fact]
    public Task InsertDoSnippetInGlobalContextTest()
        => VerifySnippetAsync("""
            $$
            """, """
            do
            {
                $$
            }
            while ({|0:true|});
            """);

    [Fact]
    public Task NoDoSnippetInBlockNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);

    [Fact]
    public Task NoDoSnippetInFileScopedNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace;

            $$
            """);

    [Fact]
    public Task InsertDoSnippetInConstructorTest()
        => VerifySnippetAsync("""
            class Program
            {
                public Program()
                {
                    var x = 5;
                    $$
                }
            }
            """, """
            class Program
            {
                public Program()
                {
                    var x = 5;
                    do
                    {
                        $$
                    }
                    while ({|0:true|});
                }
            }
            """);

    [Fact]
    public Task InsertDoSnippetInLocalFunctionTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    var x = 5;
                    void LocalMethod()
                    {
                        $$
                    }
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    var x = 5;
                    void LocalMethod()
                    {
                        do
                        {
                            $$
                        }
                        while ({|0:true|});
                    }
                }
            }
            """);

    [Fact]
    public Task InsertDoSnippetInAnonymousFunctionTest()
        => VerifySnippetAsync("""
            public delegate void Print(int value);

            static void Main(string[] args)
            {
                Print print = delegate(int val)
                {
                    $$
                };

            }
            """, """
            public delegate void Print(int value);

            static void Main(string[] args)
            {
                Print print = delegate(int val)
                {
                    do
                    {
                        $$
                    }
                    while ({|0:true|});
                };

            }
            """);

    [Fact]
    public Task InsertDoSnippetInParenthesizedLambdaExpressionTest()
        => VerifySnippetAsync("""
            using System;

            Func<int, int, bool> testForEquality = (x, y) =>
            {
                $$
                return x == y;
            };
            """, """
            using System;

            Func<int, int, bool> testForEquality = (x, y) =>
            {
                do
                {
                    $$
                }
                while ({|0:true|});
                return x == y;
            };
            """);

    [Fact]
    public Task NoDoSnippetInSwitchExpression()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var operation = 2;
  
                    var result = operation switch
                    {
                        $$
                        1 => "Case 1",
                        2 => "Case 2",
                        3 => "Case 3",
                        4 => "Case 4",
                    };
                }
            }
            """);

    [Fact]
    public Task NoDoSnippetInSingleLambdaExpression()
        => VerifySnippetIsAbsentAsync("""
            using System;

            class Program
            {
                public void Method()
                {
                    Func<int, int> f = x => $$;
                }
            }
            """);

    [Fact]
    public Task NoDoSnippetInStringTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var str = "$$";
                }
            }
            """);

    [Fact]
    public Task NoDoSnippetInConstructorArgumentsTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var test = new Test($$);
                }
            }

            class Test
            {
                public Test(string val)
                {
                }
            }
            """);

    [Fact]
    public Task NoDoSnippetInParameterListTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method(int x, $$)
                {
                }
            }
            """);

    [Fact]
    public Task NoDoSnippetInRecordDeclarationTest()
        => VerifySnippetIsAbsentAsync("""
            public record Person
            {
                $$
                public string FirstName { get; init; }
                public string LastName { get; init; }
            };
            """);

    [Fact]
    public Task NoDoSnippetInVariableDeclarationTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var x = $$
                }
            }
            """);

    [Fact]
    public Task InsertInlineDoSnippetForCorrectTypeTest()
        => VerifySnippetAsync("""
            class Program
            {
                void M(bool arg)
                {
                    arg.$$
                }
            }
            """, """
            class Program
            {
                void M(bool arg)
                {
                    do
                    {
                        $$
                    }
                    while (arg);
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetForIncorrectTypeTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                void M(int arg)
                {
                    arg.$$
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetWhenNotDirectlyExpressionStatementTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                void M(bool arg)
                {
                    System.Console.WriteLine(arg.$$);
                }
            }
            """);

    [Theory]
    [InlineData("// comment")]
    [InlineData("/* comment */")]
    [InlineData("#region test")]
    public Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest1(string trivia)
        => VerifySnippetAsync($$"""
            class Program
            {
                void M(bool arg)
                {
                    {{trivia}}
                    arg.$$
                }
            }
            """, $$"""
            class Program
            {
                void M(bool arg)
                {
                    {{trivia}}
                    do
                    {
                        $$
                    }
                    while (arg);
                }
            }
            """);

    [Theory]
    [InlineData("#if true")]
    [InlineData("#pragma warning disable CS0108")]
    [InlineData("#nullable enable")]
    public Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest2(string trivia)
        => VerifySnippetAsync($$"""
            class Program
            {
                void M(bool arg)
                {
            {{trivia}}
                    arg.$$
                }
            }
            """, $$"""
            class Program
            {
                void M(bool arg)
                {
            {{trivia}}
                    do
                    {
                        $$
                    }
                    while (arg);
                }
            }
            """);

    [Theory]
    [InlineData("// comment")]
    [InlineData("/* comment */")]
    public Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest1(string trivia)
        => VerifySnippetAsync($"""
            {trivia}
            true.$$
            """, $$"""
            {{trivia}}
            do
            {
                $$
            }
            while (true);
            """);

    [Theory]
    [InlineData("#region test")]
    [InlineData("#if true")]
    [InlineData("#pragma warning disable CS0108")]
    [InlineData("#nullable enable")]
    public Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest2(string trivia)
        => VerifySnippetAsync($"""
            {trivia}
            true.$$
            """, $$"""

            {{trivia}}
            do
            {
                $$
            }
            while (true);
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public Task InsertInlineSnippetWhenDottingBeforeContextualKeywordTest1()
        => VerifySnippetAsync("""
            class C
            {
                void M(bool flag)
                {
                    flag.$$
                    var a = 0;
                }
            }
            """, """
            class C
            {
                void M(bool flag)
                {
                    do
                    {
                        $$
                    }
                    while (flag);
                    var a = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public Task InsertInlineSnippetWhenDottingBeforeContextualKeywordTest2()
        => VerifySnippetAsync("""
            class C
            {
                async void M(bool flag, Task t)
                {
                    flag.$$
                    await t;
                }
            }
            """, """
            class C
            {
                async void M(bool flag, Task t)
                {
                    do
                    {
                        $$
                    }
                    while (flag);
                    await t;
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("System.Threading.Tasks.Task<int>")]
    public Task InsertInlineSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
        => VerifySnippetAsync($$"""
            using System.Threading.Tasks;

            class C
            {
                void M(bool flag)
                {
                    flag.$$
                    {{nameSyntax}} t = null;
                }
            }
            """, $$"""
            using System.Threading.Tasks;

            class C
            {
                void M(bool flag)
                {
                    do
                    {
                        $$
                    }
                    while (flag);
                    {{nameSyntax}} t = null;
                }
            }
            """);

    [Fact]
    public Task InsertInlineDoSnippetWhenDottingBeforeMemberAccessExpressionOnTheNextLineTest()
        => VerifySnippetAsync("""
            using System;

            class C
            {
                void M(bool flag)
                {
                    flag.$$
                    Console.WriteLine();
                }
            }
            """, """
            using System;

            class C
            {
                void M(bool flag)
                {
                    do
                    {
                        $$
                    }
                    while (flag);
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetWhenDottingBeforeMemberAccessExpressionOnTheSameLineTest()
        => VerifySnippetIsAbsentAsync("""
            class C
            {
                void M(bool flag)
                {
                    flag.$$ToString();
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetWhenDottingBeforeContextualKeywordOnTheSameLineTest()
        => VerifySnippetIsAbsentAsync("""
            class C
            {
                void M(bool flag)
                {
                    flag.$$var a = 0;
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetForTypeItselfTest()
        => VerifySnippetIsAbsentAsync("""
            class C
            {
                void M()
                {
                    bool.$$
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetForTypeItselfTest_Parenthesized()
        => VerifySnippetIsAbsentAsync("""
            class C
            {
                void M()
                {
                    (bool).$$
                }
            }
            """);

    [Fact]
    public Task NoInlineDoSnippetForTypeItselfTest_BeforeContextualKeyword()
        => VerifySnippetIsAbsentAsync("""
            using System.Threading.Tasks;

            class C
            {
                async void M()
                {
                    bool.$$
                    await Task.Delay(10);
                }
            }
            """);
}
