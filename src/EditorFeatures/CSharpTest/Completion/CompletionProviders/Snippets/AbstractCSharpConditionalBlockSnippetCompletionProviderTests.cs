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
    public abstract class AbstractCSharpConditionalBlockSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        [WpfFact]
        public async Task InsertSnippetInMethodTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                        $$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    public void Method()
                    {
                        {{ItemToCommit}} (true)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertSnippetInGlobalContextTest()
        {
            var markupBeforeCommit = """
                Ins$$
                """;

            var expectedCodeAfterCommit = $$"""
                {{ItemToCommit}} (true)
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoSnippetInBlockNamespaceTest()
        {
            var markupBeforeCommit = """
                namespace Namespace
                {
                    $$
                    class Program
                    {
                        public async Task MethodAsync()
                        {
                        }
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInFileScopedNamespaceTest()
        {
            var markupBeforeCommit = """
                namespace Namespace;
                $$
                class Program
                {
                    public async Task MethodAsync()
                    {
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task InsertSnippetInConstructorTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public Program()
                    {
                        var x = 5;
                        $$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    public Program()
                    {
                        var x = 5;
                        {{ItemToCommit}} (true)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertSnippettInLocalFunctionTest()
        {
            var markupBeforeCommit = """
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

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    public void Method()
                    {
                        var x = 5;
                        void LocalMethod()
                        {
                            {{ItemToCommit}} (true)
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
        public async Task InsertSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit = """
                public delegate void Print(int value);

                static void Main(string[] args)
                {
                    Print print = delegate(int val) {
                        $$
                    };

                }
                """;

            var expectedCodeAfterCommit = $$"""
                public delegate void Print(int value);

                static void Main(string[] args)
                {
                    Print print = delegate(int val) {
                        {{ItemToCommit}} (true)
                        {
                            $$
                        }
                    };

                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertSnippetInParenthesizedLambdaExpressionTest()
        {
            var markupBeforeCommit = """
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    $$
                    return x == y;
                };
                """;

            var expectedCodeAfterCommit = $$"""
                Func<int, int, bool> testForEquality = (x, y) =>
                {
                    {{ItemToCommit}} (true)
                    {
                        $$
                    }
                    return x == y;
                };
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoSnippetInSwitchExpression()
        {
            var markupBeforeCommit = """
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
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInSingleLambdaExpression()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                       Func<int, int> f = x => $$;
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInStringTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                        var str = "$$";
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInObjectInitializerTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                        var str = new Test($$);
                    }
                }

                class Test
                {
                    private string val;

                    public Test(string val)
                    {
                        this.val = val;
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInParameterListTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method(int x, $$)
                    {
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInRecordDeclarationTest()
        {
            var markupBeforeCommit = """
                public record Person
                {
                    $$
                    public string FirstName { get; init; } = default!;
                    public string LastName { get; init; } = default!;
                };
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoSnippetInVariableDeclarationTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                        var x = $$
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task InsertSnippetWithInvocationBeforeAndAfterCursorTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                        Wr$$Blah
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    public void Method()
                    {
                        {{ItemToCommit}} (true)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertSnippetWithInvocationUnderscoreBeforeAndAfterCursorTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    public void Method()
                    {
                        _Wr$$Blah_
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    public void Method()
                    {
                        {{ItemToCommit}} (true)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInlineSnippetForCorrectTypeTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    void M(bool arg)
                    {
                        arg.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(bool arg)
                    {
                        {{ItemToCommit}} (arg)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoInlineSnippetForIncorrectTypeTest()
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
        public async Task NoInlineSnippetWhenNotDirectlyExpressionStatementTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    void M(bool arg)
                    {
                        System.Console.WriteLine(arg.$$);
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
                    void M(bool arg)
                    {
                        {{trivia}}
                        arg.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(bool arg)
                    {
                        {{trivia}}
                        {{ItemToCommit}} (arg)
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
                    void M(bool arg)
                    {
                {{trivia}}
                        arg.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(bool arg)
                    {
                {{trivia}}
                        {{ItemToCommit}} (arg)
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
                true.$$
                """;

            var expectedCodeAfterCommit = $$"""
                {{trivia}}
                {{ItemToCommit}} (true)
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
                true.$$
                """;

            var expectedCodeAfterCommit = $$"""

                {{trivia}}
                {{ItemToCommit}} (true)
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
