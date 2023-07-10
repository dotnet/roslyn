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
@"class Program
{
    public void Method()
    {
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        foreach (var item in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInMethodItemUsedTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var item = 5;
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var item = 5;
        foreach (var item1 in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
@"Ins$$
";

            var expectedCodeAfterCommit =
@"foreach (var item in collection)
{
    $$
}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public Program()
    {
        foreach (var item in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetWithCollectionTest()
        {
            var markupBeforeCommit =
@"using System;
using System.Collections.Generic;

class Program
{
    public Program()
    {
        var list = new List<int> { 1, 2, 3 };
        $$
    }
}";

            var expectedCodeAfterCommit =
@"using System;
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
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            $$
        }
    }
}";

            var expectedCodeAfterCommit =
@"class Program
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
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        $$
    };
}";

            var expectedCodeAfterCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        foreach (var item in args)
        {
            $$
        }
    };
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionRegularTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in args)
    {
        $$
    }
    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionScriptTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in collection)
    {
        $$
    }
    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact]
        public async Task InsertInlineForEachSnippetForCorrectTypeTest()
        {
            var markupBeforeCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(List<int> list)
                    {
                        list.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                using System.Collections.Generic;
                
                class C
                {
                    void M(List<int> list)
                    {
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
    }
}
