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
    public async Task InsertDoSnippetInMethodTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertDoSnippetInGlobalContextTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            do
            {
                $$
            }
            while ({|0:true|});
            """);
    }

    [Fact]
    public async Task NoDoSnippetInBlockNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoDoSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace;

            $$
            """);
    }

    [Fact]
    public async Task InsertDoSnippetInConstructorTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertDoSnippetInLocalFunctionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertDoSnippetInAnonymousFunctionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertDoSnippetInParenthesizedLambdaExpressionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task NoDoSnippetInSwitchExpression()
    {
        await VerifySnippetIsAbsentAsync("""
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
    }

    [Fact]
    public async Task NoDoSnippetInSingleLambdaExpression()
    {
        await VerifySnippetIsAbsentAsync("""
            using System;

            class Program
            {
                public void Method()
                {
                    Func<int, int> f = x => $$;
                }
            }
            """);
    }

    [Fact]
    public async Task NoDoSnippetInStringTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var str = "$$";
                }
            }
            """);
    }

    [Fact]
    public async Task NoDoSnippetInConstructorArgumentsTest()
    {
        await VerifySnippetIsAbsentAsync("""
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
    }

    [Fact]
    public async Task NoDoSnippetInParameterListTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method(int x, $$)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task NoDoSnippetInRecordDeclarationTest()
    {
        await VerifySnippetIsAbsentAsync("""
            public record Person
            {
                $$
                public string FirstName { get; init; }
                public string LastName { get; init; }
            };
            """);
    }

    [Fact]
    public async Task NoDoSnippetInVariableDeclarationTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var x = $$
                }
            }
            """);
    }

    [Fact]
    public async Task InsertInlineDoSnippetForCorrectTypeTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task NoInlineDoSnippetForIncorrectTypeTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                void M(int arg)
                {
                    arg.$$
                }
            }
            """);
    }

    [Fact]
    public async Task NoInlineDoSnippetWhenNotDirectlyExpressionStatementTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                void M(bool arg)
                {
                    System.Console.WriteLine(arg.$$);
                }
            }
            """);
    }

    [Theory]
    [InlineData("// comment")]
    [InlineData("/* comment */")]
    [InlineData("#region test")]
    public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest1(string trivia)
    {
        await VerifySnippetAsync($$"""
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
    }

    [Theory]
    [InlineData("#if true")]
    [InlineData("#pragma warning disable CS0108")]
    [InlineData("#nullable enable")]
    public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest2(string trivia)
    {
        await VerifySnippetAsync($$"""
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
    }

    [Theory]
    [InlineData("// comment")]
    [InlineData("/* comment */")]
    public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest1(string trivia)
    {
        await VerifySnippetAsync($"""
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
    }

    [Theory]
    [InlineData("#region test")]
    [InlineData("#if true")]
    [InlineData("#pragma warning disable CS0108")]
    [InlineData("#nullable enable")]
    public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest2(string trivia)
    {
        await VerifySnippetAsync($"""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public async Task InsertInlineSnippetWhenDottingBeforeContextualKeywordTest1()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public async Task InsertInlineSnippetWhenDottingBeforeContextualKeywordTest2()
    {
        await VerifySnippetAsync("""
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
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("System.Threading.Tasks.Task<int>")]
    public async Task InsertInlineSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
    {
        await VerifySnippetAsync($$"""
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
    }
}
