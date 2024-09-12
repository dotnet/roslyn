// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpForEachSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "foreach";

        [WpfFact]
        public async Task InsertForEachSnippetInMethodTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        Ins$$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        foreach (var item in collection)
                        {
                            $$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInMethodItemUsedTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var item = 5;
                        Ins$$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var item = 5;
                        foreach (var item1 in collection)
                        {
                            $$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
                """
                Ins$$
                """;

            var expectedCodeAfterCommit =
                """
                foreach (var item in collection)
                {
                    $$
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInConstructorTest()
        {
            var markupBeforeCommit =
                """
                class Program
                {
                    public Program()
                    {
                        $$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                class Program
                {
                    public Program()
                    {
                        foreach (var item in collection)
                        {
                            $$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetWithCollectionTest()
        {
            var markupBeforeCommit =
                """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    public Program()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        $$
                    }
                }
                """;

            var expectedCodeAfterCommit =
                """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    public Program()
                    {
                        var list = new List<int> { 1, 2, 3 };
                        foreach (var item in list)
                        {
                            $$
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
                """
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
                """;

            var expectedCodeAfterCommit =
                """
                class Program
                {
                    public void Method()
                    {
                        var x = 5;
                        void LocalMethod()
                        {
                            foreach (var item in collection)
                            {
                                $$
                            }
                        }
                    }
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit =
                """
                public delegate void Print(int value);
                static void Main(string[] args)
                {
                    Print print = delegate(int val) {
                        $$
                    };
                }
                """;

            var expectedCodeAfterCommit =
                """
                public delegate void Print(int value);
                static void Main(string[] args)
                {
                    Print print = delegate(int val) {
                        foreach (var item in args)
                        {
                            $$
                        }
                    };
                }
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionRegularTest()
        {
            var markupBeforeCommit =
                """
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    $$
                    return x == y;
                };
                """;

            var expectedCodeAfterCommit =
                """
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    foreach (var item in args)
                    {
                        $$
                    }
                    return x == y;
                };
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionScriptTest()
        {
            var markupBeforeCommit =
                """
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    $$
                    return x == y;
                };
                """;

            var expectedCodeAfterCommit =
                """
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    foreach (var item in collection)
                    {
                        $$
                    }
                    return x == y;
                };
                """;
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfTheory]
        [InlineData("List<int>")]
        [InlineData("int[]")]
        [InlineData("IEnumerable<int>")]
        [InlineData("ArrayList")]
        [InlineData("IEnumerable")]
        public async Task InsertInlineForEachSnippetForCorrectTypeTest(string collectionType)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;
                using System.Collections;

                class C
                {
                    void M({{collectionType}} enumerable)
                    {
                        enumerable.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;
                using System.Collections;
                
                class C
                {
                    void M({{collectionType}} enumerable)
                    {
                        foreach (var item in enumerable)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoInlineForEachSnippetForIncorrectTypeTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    void M(int arg)
                    {
                        arg.$$
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoInlineForEachSnippetWhenNotDirectlyExpressionStatementTest()
        {
            var markupBeforeCommit = """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    void M(List<int> list)
                    {
                        Console.WriteLine(list.$$);
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("// comment")]
        [InlineData("/* comment */")]
        [InlineData("#region test")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest1(string trivia)
        {
            var markupBeforeCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                        {{trivia}}
                        arr.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                        {{trivia}}
                        foreach (var item in arr)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("#if true")]
        [InlineData("#pragma warning disable CS0108")]
        [InlineData("#nullable enable")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest2(string trivia)
        {
            var markupBeforeCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                {{trivia}}
                        arr.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                {{trivia}}
                        foreach (var item in arr)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("// comment")]
        [InlineData("/* comment */")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest1(string trivia)
        {
            var markupBeforeCommit = $$"""
                {{trivia}}
                (new int[10]).$$
                """;

            var expectedCodeAfterCommit = $$"""
                {{trivia}}
                foreach (var item in new int[10])
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("#region test")]
        [InlineData("#if true")]
        [InlineData("#pragma warning disable CS0108")]
        [InlineData("#nullable enable")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest2(string trivia)
        {
            var markupBeforeCommit = $$"""
                {{trivia}}
                (new int[10]).$$
                """;

            var expectedCodeAfterCommit = $$"""

                {{trivia}}
                foreach (var item in new int[10])
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("async ")]
        public async Task InsertForEachSnippetAfterSingleAwaitKeywordInMethodBodyTest(string asyncKeyword)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>class C
                {
                    {{asyncKeyword}}void M()
                    {
                        await $$
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        await foreach (var item in collection)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetAfterSingleAwaitKeywordInGlobalStatementTest()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>await $$</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                await foreach (var item in collection)
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoForEachStatementAfterAwaitKeywordWhenWontResultInStatementTest()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>var result = await $$</Document>
                    </Project>
                </Workspace>
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("async ")]
        public async Task PreferAsyncEnumerableVariableInScopeForAwaitForEachTest(string asyncKeyword)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System.Collections.Generic;
                
                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        IEnumerable&lt;int&gt; enumerable;
                        IAsyncEnumerable&lt;int&gt; asyncEnumerable;
                
                        await $$
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;
                
                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        IEnumerable<int> enumerable;
                        IAsyncEnumerable<int> asyncEnumerable;
                
                        await foreach (var item in asyncEnumerable)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("async ")]
        public async Task InsertAwaitForEachSnippetForPostfixAsyncEnumerableTest(string asyncKeyword)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System.Collections.Generic;
                
                class C
                {
                    {{asyncKeyword}}void M(IAsyncEnumerable&lt;int&gt; asyncEnumerable)
                    {
                        asyncEnumerable.$$
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;
                
                class C
                {
                    {{asyncKeyword}}void M(IAsyncEnumerable<int> asyncEnumerable)
                    {
                        await foreach (var item in asyncEnumerable)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
        public async Task InsertInlineForEachSnippetWhenDottingBeforeContextualKeywordTest1()
        {
            var markupBeforeCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> ints)
                    {
                        ints.$$
                        var a = 0;
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> ints)
                    {
                        foreach (var item in ints)
                        {
                            $$
                        }
                        var a = 0;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
        public async Task InsertInlineForEachSnippetWhenDottingBeforeContextualKeywordTest2()
        {
            var markupBeforeCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> ints, Task t)
                    {
                        ints.$$
                        await t;
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> ints, Task t)
                    {
                        foreach (var item in ints)
                        {
                            $$
                        }
                        await t;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
        [InlineData("Task")]
        [InlineData("Task<int>")]
        [InlineData("System.Threading.Tasks.Task<int>")]
        public async Task InsertInlineForEachSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> ints)
                    {
                        ints.$$
                        {{nameSyntax}} t = null;
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M(IEnumerable<int> ints)
                    {
                        foreach (var item in ints)
                        {
                            $$
                        }
                        {{nameSyntax}} t = null;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
        public async Task InsertInlineAwaitForEachSnippetWhenDottingBeforeContextualKeywordTest1()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System.Collections.Generic;
                
                class C
                {
                    void M(IAsyncEnumerable&lt;int&gt; asyncInts)
                    {
                        asyncInts.$$
                        var a = 0;
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(IAsyncEnumerable<int> asyncInts)
                    {
                        await foreach (var item in asyncInts)
                        {
                            $$
                        }
                        var a = 0;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
        public async Task InsertInlineAwaitForEachSnippetWhenDottingBeforeContextualKeywordTest2()
        {
            var markupBeforeCommit = """
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System.Collections.Generic;
                
                class C
                {
                    void M(IAsyncEnumerable&lt;int&gt; asyncInts, Task t)
                    {
                        asyncInts.$$
                        await t;
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(IAsyncEnumerable<int> asyncInts, Task t)
                    {
                        await foreach (var item in asyncInts)
                        {
                            $$
                        }
                        await t;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
        [InlineData("Task")]
        [InlineData("Task<int>")]
        [InlineData("System.Threading.Tasks.Task<int>")]
        public async Task InsertInlineAwaitForEachSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System.Collections.Generic;
                
                class C
                {
                    void M(IAsyncEnumerable&lt;int&gt; asyncInts)
                    {
                        asyncInts.$$
                        {{nameSyntax.Replace("<", "&lt;").Replace(">", "&gt;")}} t = null;
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M(IAsyncEnumerable<int> asyncInts)
                    {
                        await foreach (var item in asyncInts)
                        {
                            $$
                        }
                        {{nameSyntax}} t = null;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
