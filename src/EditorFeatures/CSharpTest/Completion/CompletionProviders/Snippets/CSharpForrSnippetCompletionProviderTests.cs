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
    public class CSharpForrSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "forr";

        [WpfFact]
        public async Task InsertForrSnippetInMethodTest()
        {
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public void Method()
                    {
                        $$
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        for (int i = length - 1; i >= 0; i--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInMethodUsedIncrementorTest()
        {
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public void Method()
                    {
                        int i;
                        $$
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        int i;
                        for (int j = length - 1; j >= 0; j--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInMethodUsedIncrementorsTest()
        {
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public void Method()
                    {
                        int i, j, k;
                        $$
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        int i, j, k;
                        for (int i1 = length - 1; i1 >= 0; i1--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInGlobalContextTest()
        {
            await VerifyCustomCommitProviderAsync("""
                $$
                """, ItemToCommit, """
                for (int i = length - 1; i >= 0; i--)
                {
                    $$
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInConstructorTest()
        {
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public Program()
                    {
                        $$
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public Program()
                    {
                        for (int i = length - 1; i >= 0; i--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInLocalFunctionTest()
        {
            // TODO: fix this test when bug with simplifier failing to find correct node is fixed
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public void Method()
                    {
                        void LocalFunction()
                        {
                            $$
                        }
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        void LocalFunction()
                        {
                            for (global::System.Int32 i = (length) - (1); i >= 0; i--)
                            {
                                $$
                            }
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInAnonymousFunctionTest()
        {
            // TODO: fix this test when bug with simplifier failing to find correct node is fixed
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public void Method()
                    {
                        var action = delegate()
                        {
                            $$
                        };
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        var action = delegate()
                        {
                            for (global::System.Int32 i = (length) - (1); i >= 0; i--)
                            {
                                $$
                            }
                        };
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForrSnippetInParenthesizedLambdaExpressionTest()
        {
            // TODO: fix this test when bug with simplifier failing to find correct node is fixed
            await VerifyCustomCommitProviderAsync("""
                class Program
                {
                    public void Method()
                    {
                        var action = () =>
                        {
                            $$
                        };
                    }
                }
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        var action = () =>
                        {
                            for (global::System.Int32 i = (length) - (1); i >= 0; i--)
                            {
                                $$
                            }
                        };
                    }
                }
                """);
        }

        [WpfFact]
        public async Task TryToProduceVarWithSpecificCodeStyleTest()
        {
            // In non-inline reversed for snippet type of expression `length - 1` is unknown,
            // so it cannot be simplified to `var`. Therefore having explicit `int` type here is expected
            await VerifyCustomCommitProviderAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">class Program
                {
                    public void Method()
                    {
                        $$
                    }
                }</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                root = true

                [*]
                # IDE0008: Use explicit type
                csharp_style_var_for_built_in_types = true
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, ItemToCommit, """
                class Program
                {
                    public void Method()
                    {
                        for (int i = length - 1; i >= 0; i--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfTheory]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("nint")]
        [InlineData("nuint")]
        public async Task InsertInlineForrSnippetInMethodTest(string inlineExpressionType)
        {
            await VerifyCustomCommitProviderAsync($$"""
                class Program
                {
                    public void Method({{inlineExpressionType}} l)
                    {
                        l.$$
                    }
                }
                """, ItemToCommit, $$"""
                class Program
                {
                    public void Method({{inlineExpressionType}} l)
                    {
                        for ({{inlineExpressionType}} i = l - 1; i >= 0; i--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfTheory]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("nint")]
        [InlineData("nuint")]
        public async Task InsertInlineForrSnippetInGlobalContextTest(string inlineExpressionType)
        {
            await VerifyCustomCommitProviderAsync($$"""
                {{inlineExpressionType}} l;
                l.$$
                """, ItemToCommit, $$"""
                {{inlineExpressionType}} l;
                for ({{inlineExpressionType}} i = l - 1; i >= 0; i--)
                {
                    $$
                }
                """);
        }

        [WpfTheory]
        [InlineData("string")]
        [InlineData("System.DateTime")]
        [InlineData("System.Action")]
        public async Task NoInlineForrSnippetForIncorrectTypeInMethodTest(string inlineExpressionType)
        {
            var markup = $$"""
                class Program
                {
                    public void Method({{inlineExpressionType}} l)
                    {
                        l.$$
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("string")]
        [InlineData("System.DateTime")]
        [InlineData("System.Action")]
        public async Task NoInlineForrSnippetForIncorrectTypeInGlobalContextTest(string inlineExpressionType)
        {
            var markup = $$"""
                {{inlineExpressionType}} l;
                l.$$
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfFact]
        public async Task ProduceVarWithSpecificCodeStyleForInlineSnippetTest()
        {
            await VerifyCustomCommitProviderAsync("""
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">class Program
                {
                    public void Method(int l)
                    {
                        l.$$
                    }
                }</Document>
                <AnalyzerConfigDocument FilePath="/.editorconfig">
                root = true

                [*]
                # IDE0008: Use explicit type
                csharp_style_var_for_built_in_types = true
                    </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """, ItemToCommit, """
                class Program
                {
                    public void Method(int l)
                    {
                        for (var i = l - 1; i >= 0; i--)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task NoInlineForrSnippetNotDirectlyExpressionStatementTest()
        {
            var markup = """
                class Program
                {
                    public void Method(int l)
                    {
                        System.Console.WriteLine(l.$$);
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
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
                    void M(int len)
                    {
                        {{trivia}}
                        len.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(int len)
                    {
                        {{trivia}}
                        for (int i = len - 1; i >= 0; i--)
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
                    void M(int len)
                    {
                {{trivia}}
                        len.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(int len)
                    {
                {{trivia}}
                        for (int i = len - 1; i >= 0; i--)
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
                10.$$
                """;

            var expectedCodeAfterCommit = $$"""
                {{trivia}}
                for (int i = 10 - 1; i >= 0; i--)
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
                10.$$
                """;

            var expectedCodeAfterCommit = $$"""

                {{trivia}}
                for (int i = 10 - 1; i >= 0; i--)
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
