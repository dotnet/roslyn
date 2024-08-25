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
public sealed class CSharpReversedForSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "forr";

    [Fact]
    public async Task InsertReversedForSnippetInMethodTest()
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
                    for (int {|0:i|} = {|1:length|} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInMethodUsedIncrementorTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    int i;
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    int i;
                    for (int {|0:j|} = {|1:length|} - 1; {|0:j|} >= 0; {|0:j|}--)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInMethodUsedIncrementorsTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    int i, j, k;
                    $$
                }
            }
            """, """
            class Program
            {
                public void Method()
                {
                    int i, j, k;
                    for (int {|0:i1|} = {|1:length|} - 1; {|0:i1|} >= 0; {|0:i1|}--)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInGlobalContextTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            for (int {|0:i|} = {|1:length|} - 1; {|0:i|} >= 0; {|0:i|}--)
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInConstructorTest()
    {
        await VerifySnippetAsync("""
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
                    for (int {|0:i|} = {|1:length|} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInLocalFunctionTest()
    {
        // TODO: fix this test when bug with simplifier failing to find correct node is fixed
        await VerifySnippetAsync("""
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
            """, """
            class Program
            {
                public void Method()
                {
                    void LocalFunction()
                    {
                        for (global::System.Int32 {|0:i|} = {|1:(length)|} - (1); {|0:i|} >= 0; {|0:i|}--)
                        {
                            $$
                        }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInAnonymousFunctionTest()
    {
        // TODO: fix this test when bug with simplifier failing to find correct node is fixed
        await VerifySnippetAsync("""
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
            """, """
            class Program
            {
                public void Method()
                {
                    var action = delegate()
                    {
                        for (global::System.Int32 {|0:i|} = {|1:(length)|} - (1); {|0:i|} >= 0; {|0:i|}--)
                        {
                            $$
                        }
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task InsertReversedForSnippetInParenthesizedLambdaExpressionTest()
    {
        // TODO: fix this test when bug with simplifier failing to find correct node is fixed
        await VerifySnippetAsync("""
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
            """, """
            class Program
            {
                public void Method()
                {
                    var action = () =>
                    {
                        for (global::System.Int32 {|0:i|} = {|1:(length)|} - (1); {|0:i|} >= 0; {|0:i|}--)
                        {
                            $$
                        }
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task TryToProduceVarWithSpecificCodeStyleTest()
    {
        // In non-inline reversed for snippet type of expression `length - 1` is unknown,
        // so it cannot be simplified to `var`. Therefore having explicit `int` type here is expected
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
                    for (int {|0:i|} = {|1:length|} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """,
            editorconfig: """
            root = true
            
            [*]
            csharp_style_var_for_built_in_types = true
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertInlineReversedForSnippetInMethodTest(string inlineExpressionType)
    {
        await VerifySnippetAsync($$"""
            class Program
            {
                public void Method({{inlineExpressionType}} l)
                {
                    l.$$
                }
            }
            """, $$"""
            class Program
            {
                public void Method({{inlineExpressionType}} l)
                {
                    for ({{inlineExpressionType}} {|0:i|} = l - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertInlineReversedForSnippetInGlobalContextTest(string inlineExpressionType)
    {
        await VerifySnippetAsync($$"""
            {{inlineExpressionType}} l;
            l.$$
            """, $$"""
            {{inlineExpressionType}} l;
            for ({{inlineExpressionType}} {|0:i|} = l - 1; {|0:i|} >= 0; {|0:i|}--)
            {
                $$
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.NotIntegerTypesWithoutLengthOrCountProperty), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoInlineReversedForSnippetForIncorrectTypeInMethodTest(string inlineExpressionType)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class Program
            {
                public void Method({{inlineExpressionType}} l)
                {
                    l.$$
                }
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.NotIntegerTypesWithoutLengthOrCountProperty), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoInlineReversedForSnippetForIncorrectTypeInGlobalContextTest(string inlineExpressionType)
    {
        await VerifySnippetIsAbsentAsync($$"""
            {{inlineExpressionType}} l;
            l.$$
            """);
    }

    [Fact]
    public async Task ProduceVarWithSpecificCodeStyleForInlineSnippetTest()
    {
        await VerifySnippetAsync("""
            class Program
            {
                public void Method(int l)
                {
                    l.$$
                }
            }
            """, """
            class Program
            {
                public void Method(int l)
                {
                    for (var {|0:i|} = l - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """,
            editorconfig: """
            root = true
            
            [*]
            csharp_style_var_for_built_in_types = true
            """);
    }

    [Fact]
    public async Task NoInlineReversedForSnippetNotDirectlyExpressionStatementTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method(int l)
                {
                    System.Console.WriteLine(l.$$);
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
                void M(int len)
                {
                    {{trivia}}
                    len.$$
                }
            }
            """, $$"""
            class Program
            {
                void M(int len)
                {
                    {{trivia}}
                    for (int {|0:i|} = len - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
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
                void M(int len)
                {
            {{trivia}}
                    len.$$
                }
            }
            """, $$"""
            class Program
            {
                void M(int len)
                {
            {{trivia}}
                    for (int {|0:i|} = len - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """);
    }

    [Theory]
    [InlineData("// comment")]
    [InlineData("/* comment */")]
    public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest1(string trivia)
    {
        await VerifySnippetAsync($$"""
            {{trivia}}
            10.$$
            """, $$"""
            {{trivia}}
            for (int {|0:i|} = 10 - 1; {|0:i|} >= 0; {|0:i|}--)
            {
                $$
            }
            """);
    }

    [Theory]
    [InlineData("#region test")]
    [InlineData("#if true")]
    [InlineData("#pragma warning disable CS0108")]
    [InlineData("#nullable enable")]
    public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest2(string trivia)
    {
        await VerifySnippetAsync($$"""
            {{trivia}}
            10.$$
            """, $$"""

            {{trivia}}
            for (int {|0:i|} = 10 - 1; {|0:i|} >= 0; {|0:i|}--)
            {
                $$
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertInlineReversedForSnippetWhenDottingBeforeContextualKeywordTest1(string intType)
    {
        await VerifySnippetAsync($$"""
            using System.Collections.Generic;

            class C
            {
                void M({{intType}} @int)
                {
                    @int.$$
                    var a = 0;
                }
            }
            """, $$"""
            using System.Collections.Generic;

            class C
            {
                void M({{intType}} @int)
                {
                    for ({{intType}} {|0:i|} = @int - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                    var a = 0;
                }
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertInlineReversedForSnippetWhenDottingBeforeContextualKeywordTest2(string intType)
    {
        await VerifySnippetAsync($$"""
            using System.Collections.Generic;

            class C
            {
                async void M({{intType}} @int, Task t)
                {
                    @int.$$
                    await t;
                }
            }
            """, $$"""
            using System.Collections.Generic;

            class C
            {
                async void M({{intType}} @int, Task t)
                {
                    for ({{intType}} {|0:i|} = @int - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                    await t;
                }
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/69598")]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("System.Threading.Tasks.Task<int>")]
    public async Task InsertInlineReversedForSnippetWhenDottingBeforeNameSyntaxTest(string nameSyntax)
    {
        await VerifySnippetAsync($$"""
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void M(int @int)
                {
                    @int.$$
                    {{nameSyntax}} t = null;
                }
            }
            """, $$"""
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void M(int @int)
                {
                    for (int {|0:i|} = @int - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                    {{nameSyntax}} t = null;
                }
            }
            """);
    }

    [Theory]
    [InlineData("int[]", "Length")]
    [InlineData("Span<byte>", "Length")]
    [InlineData("ReadOnlySpan<long>", "Length")]
    [InlineData("ImmutableArray<C>", "Length")]
    [InlineData("List<int>", "Count")]
    [InlineData("HashSet<byte>", "Count")]
    [InlineData("Dictionary<long>", "Count")]
    [InlineData("ImmutableList<C>", "Count")]
    public async Task InsertInlineReversedForSnippetForCommonTypesWithLengthOrCountPropertyTest(string type, string propertyName)
    {
        await VerifySnippetAsync($$"""
            using System;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            
            public class C
            {
                void M({{type}} type)
                {
                    type.$$
                }
            }
            """, $$"""
            using System;
            using System.Collections.Generic;
            using System.Collections.Immutable;

            public class C
            {
                void M({{type}} type)
                {
                    for (int {|0:i|} = type.{{propertyName}} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }
            """,
            referenceAssemblies: ReferenceAssemblies.Net.Net80);
    }

    [Theory]
    [CombinatorialData]
    public async Task InsertInlineReversedForSnippetForTypeWithAccessibleLengthOrCountPropertyTest(
        [CombinatorialValues("public", "internal", "protected internal")] string propertyAccessibility,
        [CombinatorialValues("Length", "Count")] string propertyName)
    {
        await VerifySnippetAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }

            public class MyType
            {
                {{propertyAccessibility}} int {{propertyName}} { get; }
            }
            """, $$"""
            class C
            {
                void M(MyType type)
                {
                    for (int {|0:i|} = type.{{propertyName}} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }

            public class MyType
            {
                {{propertyAccessibility}} int {{propertyName}} { get; }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task InsertInlineReversedForSnippetForTypeWithAccessibleLengthOrCountPropertyGetterTest(
        [CombinatorialValues("", "internal", "protected internal")] string getterAccessibility,
        [CombinatorialValues("Length", "Count")] string propertyName)
    {
        await VerifySnippetAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }

            public class MyType
            {
                public int {{propertyName}} { {{getterAccessibility}} get; }
            }
            """, $$"""
            class C
            {
                void M(MyType type)
                {
                    for (int {|0:i|} = type.{{propertyName}} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }

            public class MyType
            {
                public int {{propertyName}} { {{getterAccessibility}} get; }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task InsertInlineReversedForSnippetForTypesWithLengthOrCountPropertyOfDifferentIntegerTypesTest(
        [CombinatorialValues("byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "nint", "nuint")] string integerType,
        [CombinatorialValues("Length", "Count")] string propertyName)
    {
        await VerifySnippetAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }

            public class MyType
            {
                public {{integerType}} {{propertyName}} { get; }
            }
            """, $$"""
            class C
            {
                void M(MyType type)
                {
                    for ({{integerType}} {|0:i|} = type.{{propertyName}} - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }

            public class MyType
            {
                public {{integerType}} {{propertyName}} { get; }
            }
            """);
    }

    [Theory]
    [InlineData("Length")]
    [InlineData("Count")]
    public async Task InsertInlineReversedForSnippetForTypeWithLengthOrCountPropertyInBaseClassTest(string propertyName)
    {
        await VerifySnippetAsync($$"""
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
                public int {{propertyName}} { get; }
            }
            """, $$"""
            class C
            {
                void M(MyType type)
                {
                    for (int {|0:i|} = type.{{propertyName}} - 1; {|0:i|} >= 0; {|0:i|}--)
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
                public int {{propertyName}} { get; }
            }
            """);
    }

    [Theory]
    [InlineData("Length")]
    [InlineData("Count")]
    public async Task NoInlineReversedForSnippetWhenLengthOrCountPropertyHasNoGetterTest(string propertyName)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }
            
            public class MyType
            {
                public int {{propertyName}} { set { } }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task NoInlineReversedForSnippetForInaccessibleLengthPropertyTest(
        [CombinatorialValues("private", "protected", "private protected")] string propertyAccessibility,
        [CombinatorialValues("Length", "Count")] string propertyName)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }
            
            public class MyType
            {
                {{propertyAccessibility}} int {{propertyName}} { get; }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task NoInlineReversedForSnippetForInaccessibleLengthOrCountPropertyGetterTest(
        [CombinatorialValues("private", "protected", "private protected")] string getterAccessibility,
        [CombinatorialValues("Length", "Count")] string propertyName)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }
            
            public class MyType
            {
                public int {{propertyName}} { {{getterAccessibility}} get; }
            }
            """);
    }

    [Theory]
    [CombinatorialData]
    public async Task NoInlineReversedForSnippetForLengthPropertyOfIncorrectTypeTest(
        [CombinatorialValues("object", "string", "System.DateTime", "System.Action")] string notIntegerType,
        [CombinatorialValues("Length", "Count")] string propertyName)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M(MyType type)
                {
                    type.$$
                }
            }
            
            public class MyType
            {
                public {{notIntegerType}} {{propertyName}} { get; }
            }
            """);
    }

    [Fact]
    public async Task NoInlineReversedForSnippetForTypeWithBothLengthAndCountPropertyTest()
    {
        await VerifySnippetIsAbsentAsync("""
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
            """);
    }

    [Theory]
    [InlineData("MyType")]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoInlineReversedForSnippetForTypeItselfTest(string validTypes)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M()
                {
                    {{validTypes}}.$$
                }
            }

            class MyType
            {
                public int Count => 0;
            }
            """);
    }

    [Theory]
    [InlineData("MyType")]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoInlineReversedForSnippetForTypeItselfTest_Parenthesized(string validTypes)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class C
            {
                void M()
                {
                    ({{validTypes}}).$$
                }
            }

            class MyType
            {
                public int Count => 0;
            }
            """);
    }

    [Theory]
    [InlineData("MyType")]
    [MemberData(nameof(CommonSnippetTestData.IntegerTypes), MemberType = typeof(CommonSnippetTestData))]
    public async Task NoInlineReversedForSnippetForTypeItselfTest_BeforeContextualKeyword(string validTypes)
    {
        await VerifySnippetIsAbsentAsync($$"""
            using System.Threading.Tasks;

            class C
            {
                async void M()
                {
                    {{validTypes}}.$$
                    await Task.Delay(10);
                }
            }

            class MyType
            {
                public int Count => 0;
            }
            """);
    }

    [Fact]
    public async Task InsertInlineReversedForSnippetForVariableNamedLikeTypeTest()
    {
        await VerifySnippetAsync("""
            class C
            {
                void M()
                {
                    MyType MyType = default;
                    MyType.$$
                }
            }

            class MyType
            {
                public int Length => 0;
            }
            """, """
            class C
            {
                void M()
                {
                    MyType MyType = default;
                    for (int {|0:i|} = MyType.Length - 1; {|0:i|} >= 0; {|0:i|}--)
                    {
                        $$
                    }
                }
            }

            class MyType
            {
                public int Length => 0;
            }
            """);
    }
}
