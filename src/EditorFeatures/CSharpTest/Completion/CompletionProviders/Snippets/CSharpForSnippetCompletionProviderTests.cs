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
    public class CSharpForSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "for";

        [WpfFact]
        public async Task InsertForSnippetInMethodTest()
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
                        for (int i = 0; i < length; i++)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInMethodUsedIncrementorTest()
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
                        for (int j = 0; j < length; j++)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInMethodUsedIncrementorsTest()
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
                        for (int i1 = 0; i1 < length; i1++)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInGlobalContextTest()
        {
            await VerifyCustomCommitProviderAsync("""
                $$
                """, ItemToCommit, """
                for (int i = 0; i < length; i++)
                {
                    $$
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInConstructorTest()
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
                        for (int i = 0; i < length; i++)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInLocalFunctionTest()
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
                            for (global::System.Int32 i = 0; i < length; i++)
                            {
                                $$
                            }
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInAnonymousFunctionTest()
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
                            for (global::System.Int32 i = 0; i < length; i++)
                            {
                                $$
                            }
                        };
                    }
                }
                """);
        }

        [WpfFact]
        public async Task InsertForSnippetInParenthesizedLambdaExpressionTest()
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
                            for (global::System.Int32 i = 0; i < length; i++)
                            {
                                $$
                            }
                        };
                    }
                }
                """);
        }

        [WpfFact]
        public async Task ProduceVarWithSpecificCodeStyleTest()
        {
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
                        for (var i = 0; i < length; i++)
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
        public async Task InsertInlineForSnippetInMethodTest(string inlineExpressionType)
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
                        for ({{inlineExpressionType}} i = 0; i < l; i++)
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
        public async Task InsertInlineForSnippetInGlobalContextTest(string inlineExpressionType)
        {
            await VerifyCustomCommitProviderAsync($$"""
                {{inlineExpressionType}} l;
                l.$$
                """, ItemToCommit, $$"""
                {{inlineExpressionType}} l;
                for ({{inlineExpressionType}} i = 0; i < l; i++)
                {
                    $$
                }
                """);
        }

        [WpfTheory]
        [InlineData("string")]
        [InlineData("System.DateTime")]
        [InlineData("System.Action")]
        public async Task NoInlineForSnippetForIncorrectTypeInMethodTest(string inlineExpressionType)
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
        public async Task NoInlineForSnippetForIncorrectTypeInGlobalContextTest(string inlineExpressionType)
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
                        for (var i = 0; i < l; i++)
                        {
                            $$
                        }
                    }
                }
                """);
        }

        [WpfFact]
        public async Task NoInlineForSnippetNotDirectlyExpressionStatementTest()
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
                        for (int i = 0; i < len; i++)
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
                        for (int i = 0; i < len; i++)
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
                for (int i = 0; i < 10; i++)
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
                for (int i = 0; i < 10; i++)
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
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
        public async Task InsertInlineForSnippetWhenDottingBeforeContextualKeywordTest1(string intType)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M({{intType}} @int)
                    {
                        @int.$$
                        var a = 0;
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M({{intType}} @int)
                    {
                        for ({{intType}} i = 0; i < @int; i++)
                        {
                            $$
                        }
                        var a = 0;
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
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
        public async Task InsertInlineForSnippetWhenDottingBeforeContextualKeywordTest2(string intType)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M({{intType}} @int, Task t)
                    {
                        @int.$$
                        await t;
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M({{intType}} @int, Task t)
                    {
                        for ({{intType}} i = 0; i < @int; i++)
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
        public async Task InsertInlineForSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M(int @int)
                    {
                        @int.$$
                        {{nameSyntax}} t = null;
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    void M(int @int)
                    {
                        for (int i = 0; i < @int; i++)
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
