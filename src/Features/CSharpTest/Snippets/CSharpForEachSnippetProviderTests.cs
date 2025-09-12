// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpForEachSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "foreach";

    [Fact]
    public Task InsertForEachSnippetInMethodTest()
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
                    foreach (var {|0:item|} in {|1:collection|})
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertForEachSnippetInMethodItemUsedTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    var item = 5;
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    var item = 5;
                    foreach (var {|0:item1|} in {|1:collection|})
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertForEachSnippetInGlobalContextTest()
        => VerifySnippetAsync("""
            $$
            """, """
            foreach (var {|0:item|} in {|1:collection|})
            {
                $$
            }
            """);

    [Fact]
    public Task InsertForEachSnippetInConstructorTest()
        => VerifySnippetAsync("""
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """, """
            class Program
            {
                public Program()
                {
                    foreach (var {|0:item|} in {|1:collection|})
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertForEachSnippetWithCollectionTest()
        => VerifySnippetAsync("""
            using System.Collections.Generic;

            class Program
            {
                public Program()
                {
                    var list = new List<int> { 1, 2, 3 };
                    $$
                }
            }
            """, """
            using System.Collections.Generic;

            class Program
            {
                public Program()
                {
                    var list = new List<int> { 1, 2, 3 };
                    foreach (var {|0:item|} in {|1:list|})
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task InsertForEachSnippetInLocalFunctionTest()
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
                        foreach (var {|0:item|} in {|1:collection|})
                        {
                            $$
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task InsertForEachSnippetInAnonymousFunctionTest()
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
                    foreach (var {|0:item|} in {|1:args|})
                    {
                        $$
                    }
                };
            }
            """);

    [Fact]
    public Task InsertForEachSnippetInParenthesizedLambdaExpressionTest()
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
                foreach (var {|0:item|} in {|1:args|})
                {
                    $$
                }
                return x == y;
            };
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.CommonEnumerableTypes), MemberType = typeof(CommonSnippetTestData))]
    public Task InsertInlineForEachSnippetForCorrectTypeTest(string collectionType)
        => VerifySnippetAsync($$"""
            class C
            {
                void M({{collectionType}} enumerable)
                {
                    enumerable.$$
                }
            }
            """, $$"""
            class C
            {
                void M({{collectionType}} enumerable)
                {
                    foreach (var {|0:item|} in enumerable)
                    {
                        $$
                    }
                }
            }
            """);

    [Fact]
    public Task NoInlineForEachSnippetForIncorrectTypeTest()
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
    public Task NoInlineForEachSnippetWhenNotDirectlyExpressionStatementTest()
        => VerifySnippetIsAbsentAsync("""
            using System;
            using System.Collections.Generic;

            class Program
            {
                void M(List<int> list)
                {
                    Console.WriteLine(list.$$);
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
                void M(int[] arr)
                {
                    {{trivia}}
                    arr.$$
                }
            }
            """, $$"""
            class Program
            {
                void M(int[] arr)
                {
                    {{trivia}}
                    foreach (var {|0:item|} in arr)
                    {
                        $$
                    }
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
                void M(int[] arr)
                {
            {{trivia}}
                    arr.$$
                }
            }
            """, $$"""
            class Program
            {
                void M(int[] arr)
                {
            {{trivia}}
                    foreach (var {|0:item|} in arr)
                    {
                        $$
                    }
                }
            }
            """);

    [Theory]
    [InlineData("// comment")]
    [InlineData("/* comment */")]
    public Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest1(string trivia)
        => VerifySnippetAsync($$"""
            {{trivia}}
            (new int[10]).$$
            """, $$"""
            {{trivia}}
            foreach (var {|0:item|} in new int[10])
            {
                $$
            }
            """);

    [Theory]
    [InlineData("#region test")]
    [InlineData("#if true")]
    [InlineData("#pragma warning disable CS0108")]
    [InlineData("#nullable enable")]
    public Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest2(string trivia)
        => VerifySnippetAsync($$"""
            {{trivia}}
            (new int[10]).$$
            """, $$"""

            {{trivia}}
            foreach (var {|0:item|} in new int[10])
            {
                $$
            }
            """);

    [Theory]
    [InlineData("")]
    [InlineData("async ")]
    public Task InsertForEachSnippetAfterSingleAwaitKeywordInMethodBodyTest(string asyncKeyword)
        => VerifySnippetAsync($$"""
            class C
            {
                {{asyncKeyword}}void M()
                {
                    await $$
                }
            }
            """, $$"""
            class C
            {
                {{asyncKeyword}}void M()
                {
                    await foreach (var {|0:item|} in {|1:collection|})
                    {
                        $$
                    }
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Fact]
    public Task InsertForEachSnippetAfterSingleAwaitKeywordInGlobalStatementTest()
        => VerifySnippetAsync("""
            await $$
            """, """
            await foreach (var {|0:item|} in {|1:collection|})
            {
                $$
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Fact]
    public Task NoForEachStatementAfterAwaitKeywordWhenWontResultInStatementTest()
        => VerifySnippetIsAbsentAsync("""
            var result = await $$
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Theory]
    [InlineData("")]
    [InlineData("async ")]
    public Task PreferAsyncEnumerableVariableInScopeForAwaitForEachTest(string asyncKeyword)
        => VerifySnippetAsync($$"""
            using System.Collections.Generic;
            
            class C
            {
                {{asyncKeyword}}void M()
                {
                    IEnumerable<int> enumerable;
                    IAsyncEnumerable<int> asyncEnumerable;
            
                    await $$
                }
            }
            """, $$"""
            using System.Collections.Generic;
            
            class C
            {
                {{asyncKeyword}}void M()
                {
                    IEnumerable<int> enumerable;
                    IAsyncEnumerable<int> asyncEnumerable;
            
                    await foreach (var {|0:item|} in {|1:asyncEnumerable|})
                    {
                        $$
                    }
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Theory]
    [InlineData("")]
    [InlineData("async ")]
    public Task InsertAwaitForEachSnippetForPostfixAsyncEnumerableTest(string asyncKeyword)
        => VerifySnippetAsync($$"""
            using System.Collections.Generic;
            
            class C
            {
                {{asyncKeyword}}void M(IAsyncEnumerable<int> asyncEnumerable)
                {
                    asyncEnumerable.$$
                }
            }
            """, $$"""
            using System.Collections.Generic;
            
            class C
            {
                {{asyncKeyword}}void M(IAsyncEnumerable<int> asyncEnumerable)
                {
                    await foreach (var {|0:item|} in asyncEnumerable)
                    {
                        $$
                    }
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public Task InsertInlineForEachSnippetWhenDottingBeforeContextualKeywordTest1()
        => VerifySnippetAsync("""
            using System.Collections.Generic;

            class C
            {
                void M(IEnumerable<int> ints)
                {
                    ints.$$
                    var a = 0;
                }
            }
            """, """
            using System.Collections.Generic;

            class C
            {
                void M(IEnumerable<int> ints)
                {
                    foreach (var {|0:item|} in ints)
                    {
                        $$
                    }
                    var a = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public Task InsertInlineForEachSnippetWhenDottingBeforeContextualKeywordTest2()
        => VerifySnippetAsync("""
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                async void M(IEnumerable<int> ints, Task t)
                {
                    ints.$$
                    await t;
                }
            }
            """, """
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                async void M(IEnumerable<int> ints, Task t)
                {
                    foreach (var {|0:item|} in ints)
                    {
                        $$
                    }
                    await t;
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("System.Threading.Tasks.Task<int>")]
    public Task InsertInlineForEachSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
        => VerifySnippetAsync($$"""
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void M(IEnumerable<int> ints)
                {
                    ints.$$
                    {{nameSyntax}} t = null;
                }
            }
            """, $$"""
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void M(IEnumerable<int> ints)
                {
                    foreach (var {|0:item|} in ints)
                    {
                        $$
                    }
                    {{nameSyntax}} t = null;
                }
            }
            """);

    [Fact]
    public Task InsertInlineForEachSnippetWhenDottingBeforeMemberAccessExpressionOnTheNextLineTest()
        => VerifySnippetAsync("""
            using System;

            class C
            {
                void M(int[] ints)
                {
                    ints.$$
                    Console.WriteLine();
                }
            }
            """, """
            using System;

            class C
            {
                void M(int[] ints)
                {
                    foreach (var {|0:item|} in ints)
                    {
                        $$
                    }
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task NoInlineForEachSnippetWhenDottingBeforeMemberAccessExpressionOnTheSameLineTest()
        => VerifySnippetIsAbsentAsync("""
            using System;

            class C
            {
                void M(int[] ints)
                {
                    ints.$$ToString();
                }
            }
            """);

    [Fact]
    public Task NoInlineForEachSnippetWhenDottingBeforeContextualKeywordOnTheSameLineTest()
        => VerifySnippetIsAbsentAsync("""
            using System;

            class C
            {
                void M(int[] ints)
                {
                    ints.$$var a = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public Task InsertInlineAwaitForEachSnippetWhenDottingBeforeContextualKeywordTest1()
        => VerifySnippetAsync("""
            using System.Collections.Generic;
            
            class C
            {
                void M(IAsyncEnumerable<int> asyncInts)
                {
                    asyncInts.$$
                    var a = 0;
                }
            }
            """, """
            using System.Collections.Generic;

            class C
            {
                void M(IAsyncEnumerable<int> asyncInts)
                {
                    await foreach (var {|0:item|} in asyncInts)
                    {
                        $$
                    }
                    var a = 0;
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    public Task InsertInlineAwaitForEachSnippetWhenDottingBeforeContextualKeywordTest2()
        => VerifySnippetAsync("""
            using System.Threading.Tasks;
            using System.Collections.Generic;
            
            class C
            {
                async void M(IAsyncEnumerable<int> asyncInts, Task t)
                {
                    asyncInts.$$
                    await t;
                }
            }
            """, """
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                async void M(IAsyncEnumerable<int> asyncInts, Task t)
                {
                    await foreach (var {|0:item|} in asyncInts)
                    {
                        $$
                    }
                    await t;
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("System.Threading.Tasks.Task<int>")]
    public Task InsertInlineAwaitForEachSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
        => VerifySnippetAsync($$"""
            using System.Threading.Tasks;
            using System.Collections.Generic;
            
            class C
            {
                void M(IAsyncEnumerable<int> asyncInts)
                {
                    asyncInts.$$
                    {{nameSyntax}} t = null;
                }
            }
            """, $$"""
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void M(IAsyncEnumerable<int> asyncInts)
                {
                    await foreach (var {|0:item|} in asyncInts)
                    {
                        $$
                    }
                    {{nameSyntax}} t = null;
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net70);

    [Fact]
    public Task InsertInlineAwaitForEachSnippetWhenDottingBeforeMemberAccessExpressionOnTheNextLineTest()
        => VerifySnippetAsync("""
            using System;
            using System.Collections.Generic;

            class C
            {
                void M(IAsyncEnumerable<int> ints)
                {
                    ints.$$
                    Console.WriteLine();
                }
            }
            """, """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M(IAsyncEnumerable<int> ints)
                {
                    await foreach (var {|0:item|} in ints)
                    {
                        $$
                    }
                    Console.WriteLine();
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net80);

    [Fact]
    public Task NoInlineAwaitForEachSnippetWhenDottingBeforeMemberAccessExpressionOnTheSameLineTest()
        => VerifySnippetIsAbsentAsync("""
            using System.Collections.Generic;

            class C
            {
                void M(IAsyncEnumerable<int> ints)
                {
                    ints.$$ToString();
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net80);

    [Fact]
    public Task NoInlineAwaitForEachSnippetWhenDottingBeforeContextualKeywordOnTheSameLineTest()
        => VerifySnippetIsAbsentAsync("""
            using System.Collections.Generic;

            class C
            {
                void M(IAsyncEnumerable<int> ints)
                {
                    ints.$$var a = 0;
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net80);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.CommonEnumerableTypes), MemberType = typeof(CommonSnippetTestData))]
    public Task NoInlineForEachSnippetForTypeItselfTest(string collectionType)
        => VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M()
                {
                    {{collectionType}}.$$
                }
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.CommonEnumerableTypes), MemberType = typeof(CommonSnippetTestData))]
    public Task NoInlineForEachSnippetForTypeItselfTest_Parenthesized(string collectionType)
        => VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M()
                {
                    ({{collectionType}}).$$
                }
            }
            """);

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.CommonEnumerableTypes), MemberType = typeof(CommonSnippetTestData))]
    public Task NoInlineForEachSnippetForTypeItselfTest_BeforeContextualKeyword(string collectionType)
        => VerifySnippetIsAbsentAsync($$"""
            using System.Threading.Tasks;

            class C
            {
                async void M()
                {
                    {{collectionType}}.$$
                    await Task.Delay(10);
                }
            }
            """);

    [Theory]
    [InlineData("ArrayList")]
    [InlineData("IEnumerable")]
    [InlineData("MyCollection")]
    public Task InsertInlineForEachSnippetForVariableNamedLikeTypeTest(string typeAndVariableName)
        => VerifySnippetAsync($$"""
            using System.Collections;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    {{typeAndVariableName}} {{typeAndVariableName}} = default;
                    {{typeAndVariableName}}.$$
                }
            }

            class MyCollection : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() = null;
            }
            """, $$"""
            using System.Collections;
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    {{typeAndVariableName}} {{typeAndVariableName}} = default;
                    foreach (var {|0:item|} in {{typeAndVariableName}})
                    {
                        $$
                    }
                }
            }
            
            class MyCollection : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() = null;
            }
            """);
}
