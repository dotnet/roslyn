// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        [MemberData(nameof(IntegerTypes))]
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
        [MemberData(nameof(IntegerTypes))]
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
        [MemberData(nameof(NotIntegerTypes))]
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
        [MemberData(nameof(NotIntegerTypes))]
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
        [MemberData(nameof(IntegerTypes))]
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
        [MemberData(nameof(IntegerTypes))]
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

        [WpfTheory]
        [InlineData("int[]")]
        [InlineData("Span<byte>")]
        [InlineData("ReadOnlySpan<long>")]
        [InlineData("ImmutableArray<C>")]
        public async Task InsertInlineForSnippetForCommonTypesWithLengthPropertyTest(string type)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                public class C
                {
                    void M({{type.Replace("<", "&lt;").Replace(">", "&gt;")}} type)
                    {
                        type.$$
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                public class C
                {
                    void M({{type}} type)
                    {
                        for (int i = 0; i < type.Length; i++)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public async Task InsertInlineForSnippetForTypeWithAccessibleLengthPropertyTest(string lengthPropertyAccessibility)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType
                {
                    {{lengthPropertyAccessibility}} int Length { get; }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        for (int i = 0; i < type.Length; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType
                {
                    {{lengthPropertyAccessibility}} int Length { get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public async Task InsertInlineForSnippetForTypeWithAccessibleLengthPropertyGetterTest(string getterAccessibility)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType
                {
                    public int Length { {{getterAccessibility}} get; }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        for (int i = 0; i < type.Length; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType
                {
                    public int Length { {{getterAccessibility}} get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [MemberData(nameof(IntegerTypes))]
        public async Task InsertInlineForSnippetForTypesWithLengthPropertyOfDifferentIntegerTypesTest(string integerType)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType
                {
                    public {{integerType}} Length { get; }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        for ({{integerType}} i = 0; i < type.Length; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType
                {
                    public {{integerType}} Length { get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInlineForSnippetForTypeWithLengthPropertyInBaseClassTest()
        {
            var markupBeforeCommit = """
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType : MyTypeBase
                {
                }

                public class MyTypeBase
                {
                    public int Length { get; }
                }
                """;

            var expectedCodeAfterCommit = """
                class C
                {
                    void M(MyType type)
                    {
                        for (int i = 0; i < type.Length; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType : MyTypeBase
                {
                }
                
                public class MyTypeBase
                {
                    public int Length { get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoInlineForSnippetWhenLengthPropertyHasNoGetterTest()
        {
            var markup = """
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public int Length { set { } }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        public async Task NoInlineForSnippetForInaccessibleLengthPropertyTest(string lengthPropertyAccessibility)
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    {{lengthPropertyAccessibility}} int Length { get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        public async Task NoInlineForSnippetForInaccessibleLengthPropertyGetterTest(string getterAccessibility)
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public int Length { {{getterAccessibility}} get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [MemberData(nameof(NotIntegerTypes))]
        public async Task NoInlineForSnippetForLengthPropertyOfIncorrectTypeTest(string notIntegerType)
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public {{notIntegerType}} Length { get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("List<int>")]
        [InlineData("HashSet<byte>")]
        [InlineData("Dictionary<long>")]
        [InlineData("ImmutableList<C>")]
        public async Task InsertInlineForSnippetForCommonTypesWithCountPropertyTest(string type)
        {
            var markupBeforeCommit = $$"""
                <Workspace>
                    <Project Language="C#" CommonReferencesNet7="true">
                        <Document>using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                public class C
                {
                    void M({{type.Replace("<", "&lt;").Replace(">", "&gt;")}} type)
                    {
                        type.$$
                    }
                }</Document>
                    </Project>
                </Workspace>
                """;

            var expectedCodeAfterCommit = $$"""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                public class C
                {
                    void M({{type}} type)
                    {
                        for (int i = 0; i < type.Count; i++)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public async Task InsertInlineForSnippetForTypeWithAccessibleCountPropertyTest(string countPropertyAccessibility)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType
                {
                    {{countPropertyAccessibility}} int Count { get; }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        for (int i = 0; i < type.Count; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType
                {
                    {{countPropertyAccessibility}} int Count { get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("internal")]
        [InlineData("protected internal")]
        public async Task InsertInlineForSnippetForTypeWithAccessibleCountPropertyGetterTest(string getterAccessibility)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType
                {
                    public int Count { {{getterAccessibility}} get; }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        for (int i = 0; i < type.Count; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType
                {
                    public int Count { {{getterAccessibility}} get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [MemberData(nameof(IntegerTypes))]
        public async Task InsertInlineForSnippetForTypesWithCountPropertyOfDifferentIntegerTypesTest(string integerType)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType
                {
                    public {{integerType}} Count { get; }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        for ({{integerType}} i = 0; i < type.Count; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType
                {
                    public {{integerType}} Count { get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertInlineForSnippetForTypeWithCountPropertyInBaseClassTest()
        {
            var markupBeforeCommit = """
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }

                public class MyType : MyTypeBase
                {
                }

                public class MyTypeBase
                {
                    public int Count { get; }
                }
                """;

            var expectedCodeAfterCommit = """
                class C
                {
                    void M(MyType type)
                    {
                        for (int i = 0; i < type.Count; i++)
                        {
                            $$
                        }
                    }
                }

                public class MyType : MyTypeBase
                {
                }
                
                public class MyTypeBase
                {
                    public int Count { get; }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoInlineForSnippetWhenCountPropertyHasNoGetterTest()
        {
            var markup = """
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public int Count { set { } }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        public async Task NoInlineForSnippetForInaccessibleCountPropertyTest(string countPropertyAccessibility)
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    {{countPropertyAccessibility}} int Count { get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("private protected")]
        public async Task NoInlineForSnippetForInaccessibleCountPropertyGetterTest(string getterAccessibility)
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public int Count { {{getterAccessibility}} get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfTheory]
        [MemberData(nameof(NotIntegerTypes))]
        public async Task NoInlineForSnippetForCountPropertyOfIncorrectTypeTest(string notIntegerType)
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public {{notIntegerType}} Count { get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        [WpfFact]
        public async Task NoInlineForSnippetForTypeWithBothLengthAndCountPropertyTest()
        {
            var markup = $$"""
                class C
                {
                    void M(MyType type)
                    {
                        type.$$
                    }
                }
                
                public class MyType
                {
                    public int Length { get; }
                    public int Count { get; }
                }
                """;

            await VerifyItemIsAbsentAsync(markup, ItemToCommit);
        }

        public static IEnumerable<object[]> IntegerTypes
        {
            get
            {
                return
                [
                    ["byte"],
                    ["sbyte"],
                    ["short"],
                    ["ushort"],
                    ["int"],
                    ["uint"],
                    ["long"],
                    ["ulong"],
                    ["nint"],
                    ["nuint"]
                ];
            }
        }

        public static IEnumerable<object[]> NotIntegerTypes
        {
            get
            {
                return
                [
                    ["string"],
                    ["System.DateTime"],
                    ["System.Action"]
                ];
            }
        }
    }
}
