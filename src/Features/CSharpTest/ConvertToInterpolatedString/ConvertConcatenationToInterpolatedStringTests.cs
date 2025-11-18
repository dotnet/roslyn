// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertConcatenationToInterpolatedStringRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
public sealed class ConvertConcatenationToInterpolatedStringTests
{
    [Fact]
    public Task TestMissingOnSimpleString()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]"string";
                }
            }
            """);

    [Fact]
    public Task TestMissingOnConcatenatedStrings1()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]"string" + "string";
                }
            }
            """);

    [Fact]
    public Task TestMissingOnConcatenatedStrings2()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = "string" + [||]"string";
                }
            }
            """);

    [Fact]
    public Task TestMissingOnConcatenatedStrings3()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = "string" + '.' + [||]"string";
                }
            }
            """);

    [Fact]
    public Task TestWithStringOnLeft()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]"string" + 1;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"string{1}";
                }
            }
            """);

    [Fact]
    public Task TestRightSideOfString()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "string"[||] + 1;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"string{1}";
                }
            }
            """);

    [Fact]
    public Task TestWithStringOnRight()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + [||]"string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1}string";
                }
            }
            """);

    [Fact]
    public Task TestWithComplexExpressionOnLeft()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + 2 + [||]"string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1 + 2}string";
                }
            }
            """);

    [Fact]
    public Task TestWithTrivia1()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v =
                        // Leading trivia
                        1 + 2 + [||]"string" /* trailing trivia */;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v =
                        // Leading trivia
                        $"{1 + 2}string" /* trailing trivia */;
                }
            }
            """);

    [Fact]
    public Task TestWithComplexExpressions()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + 2 + [||]"string" + 3 + 4;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1 + 2}string{3}{4}";
                }
            }
            """);

    [Fact]
    public Task TestWithEscapes1()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "\r" + 2 + [||]"string" + 3 + "\n";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"\r{2}string{3}\n";
                }
            }
            """);

    [Fact]
    public Task TestWithEscapes2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "\\r" + 2 + [||]"string" + 3 + "\\n";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"\\r{2}string{3}\\n";
                }
            }
            """);

    [Fact]
    public Task TestWithVerbatimString1()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + [||]@"string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $@"{1}string";
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMixedStringTypes1()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = 1 + [||]@"string" + 2 + "string";
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMixedStringTypes2()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = 1 + @"string" + 2 + [||]"string";
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMixedStringTypes3()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = 1 + @"string" + 2 + [||]'\n';
                }
            }
            """);

    [Fact]
    public Task TestWithOverloadedOperator()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class D
            {
                public static bool operator +(D d, string s) => false;
                public static bool operator +(string s, D d) => false;
            }

            public class C
            {
                void M()
                {
                    D d = null;
                    var v = 1 + [||]"string" + d;
                }
            }
            """,
            """
            public class D
            {
                public static bool operator +(D d, string s) => false;
                public static bool operator +(string s, D d) => false;
            }

            public class C
            {
                void M()
                {
                    D d = null;
                    var v = $"{1}string" + d;
                }
            }
            """);

    [Fact]
    public Task TestWithOverloadedOperator2()
        => VerifyCS.VerifyRefactoringAsync("""
            public class D
            {
                public static int operator +(D d, string s) => 0;
                public static int operator +(string s, D d) => 0;
            }

            public class C
            {
                void M()
                {
                    D d = null;
                    var v = d + [||]"string" + 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
    public Task TestWithMultipleStringConcatenations()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "A" + 1 + [||]"B" + "C";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"A{1}BC";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
    public Task TestWithMultipleStringConcatenations2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "A" + [||]"B" + "C" + 1;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"ABC{1}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
    public Task TestWithMultipleStringConcatenations3()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "A" + 1 + [||]"B" + "C" + 2 +"D"+ "E"+ "F" + 3;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"A{1}BC{2}DEF{3}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
    public Task TestWithMultipleStringConcatenations4()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = "A" + 1 + [||]"B" + @"C";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20943")]
    public Task TestMissingWithDynamic1()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M()
                {
                    dynamic a = "b";
                    string c = [||]"d" + a + "e";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20943")]
    public Task TestMissingWithDynamic2()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                void M()
                {
                    dynamic dynamic = null;
                    var x = dynamic.someVal + [||]" $";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
    public Task TestWithStringLiteralWithBraces()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + [||]"{string}";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1}{{string}}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
    public Task TestWithStringLiteralWithBraces2()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + [||]"{string}" + "{string}";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1}{{string}}{{string}}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
    public Task TestWithStringLiteralWithDoubleBraces()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + [||]"{{string}}";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1}{{{{string}}}}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
    public Task TestWithMultipleStringLiteralsWithBraces()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = "{" + 1 + [||]"}";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{{{1}}}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
    public Task TestWithVerbatimStringWithBraces()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 + [||]@"{string}";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $@"{1}{{string}}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
    public Task TestWithMultipleVerbatimStringsWithBraces()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = @"{" + 1 + [||]@"}";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $@"{{{1}}}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithSelectionOnEntireToBeInterpolatedString()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [|"string" + 1|];
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"string{1}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestMissingWithSelectionOnPartOfToBeInterpolatedStringPrefix()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [|"string" + 1|] + "string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestMissingWithSelectionOnPartOfToBeInterpolatedStringSuffix()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = "string" + [|1 + "string"|];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestMissingWithSelectionOnMiddlePartOfToBeInterpolatedString()
        => VerifyCS.VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = "a" + [|1 + "string"|] + "b";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithSelectionExceedingToBeInterpolatedString()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    [|var v = "string" + 1|];
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"string{1}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithCaretBeforeNonStringToken()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]3 + "string" + 1 + "string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{3}string{1}string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithCaretAfterNonStringToken()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 3[||] + "string" + 1 + "string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{3}string{1}string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithCaretBeforePlusToken()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 3 [||]+ "string" + 1 + "string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{3}string{1}string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithCaretAfterPlusToken()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 3 +[||] "string" + 1 + "string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{3}string{1}string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithCaretBeforeLastPlusToken()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 3 + "string" + 1 [||]+ "string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{3}string{1}string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
    public Task TestWithCaretAfterLastPlusToken()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 3 + "string" + 1 +[||] "string";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{3}string{1}string";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32864")]
    public Task TestConcatenationWithNoStringLiterals()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = 1 [||]+ ("string");
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var v = $"{1}{"string"}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")]
    public Task TestConcatenationWithChar()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var hello = "hello";
                    var world = "world";
                    var str = hello [||]+ ' ' + world;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var hello = "hello";
                    var world = "world";
                    var str = $"{hello} {world}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")]
    public Task TestConcatenationWithCharAfterStringLiteral()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var world = "world";
                    var str = "hello" [||]+ ' ' + world;
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var world = "world";
                    var str = $"hello {world}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")]
    public Task TestConcatenationWithCharBeforeStringLiteral()
        => VerifyCS.VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var hello = "hello";
                    var str = hello [||]+ ' ' + "world";
                }
            }
            """,
            """
            public class C
            {
                void M()
                {
                    var hello = "hello";
                    var str = $"{hello} world";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
    public Task TestConcatenationWithConstMemberCSharp9()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            class C
            {
                const string Hello = "Hello";
                const string World = "World";
                const string Message = Hello + " " + [||]World;
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
    public Task TestConcatenationWithConstMember()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = """
                class C
                {
                    const string Hello = "Hello";
                    const string World = "World";
                    const string Message = Hello + " " + [||]World;
                }
                """,
            FixedCode = """
                class C
                {
                    const string Hello = "Hello";
                    const string World = "World";
                    const string Message = $"{Hello} {World}";
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
    public async Task TestConcatenationWithConstDeclaration()
    {
        var code = """
            class C
            {
                void M() {
                    const string Hello = "Hello";
                    const string World = "World";
                    const string Message = Hello + " " + [||]World;
                }
            }
            """;
        await new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = code,
        }.RunAsync();

        await new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.Preview,
            TestCode = code,
            FixedCode = """
            class C
            {
                void M() {
                    const string Hello = "Hello";
                    const string World = "World";
                    const string Message = $"{Hello} {World}";
                }
            }
            """,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
    public Task TestConcatenationWithInlineString()
        => VerifyCS.VerifyRefactoringAsync("""
            using System;
            class C
            {
                void M() {
                    const string Hello = "Hello";
                    const string World = "World";
                    Console.WriteLine(Hello + " " + [||]World);
                }
            }
            """,
            """
            using System;
            class C
            {
                void M() {
                    const string Hello = "Hello";
                    const string World = "World";
                    Console.WriteLine($"{Hello} {World}");
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")]
    [InlineData(@"[|""a"" + $""{1:000}""|]",
                 """
                 $"a{1:000}"
                 """)]
    [InlineData(@"[|""a"" + $""b{1:000}""|]",
                 """
                 $"ab{1:000}"
                 """)]
    [InlineData(@"[|$""a{1:000}"" + ""b""|]",
                 """
                 $"a{1:000}b"
                 """)]
    [InlineData(@"[|""a"" + $""b{1:000}c"" + ""d""|]",
                 """
                 $"ab{1:000}cd"
                 """)]
    [InlineData(@"[|""a"" + $""{1:000}b"" + ""c""|]",
                 """
                 $"a{1:000}bc"
                 """)]
    [InlineData(@"[|""a"" + $""{1:000}"" + $""{2:000}"" + ""b""|]",
                 """
                 $"a{1:000}{2:000}b"
                 """)]
    [InlineData(@"[|@""a"" + @$""{1:000}""|]",
                 """
                 $@"a{1:000}"
                 """)]
    [InlineData(@"[|@""a"" + $""{1:000}""|]",
                 """
                 $@"a{$"{1:000}"}"
                 """)]
    [InlineData(@"[|""a"" + @$""{1:000}""|]",
                 """
                 $"a{@$"{1:000}"}"
                 """)]
    public Task TestInliningOfInterpolatedString(string before, string after)
        => VerifyCS.VerifyRefactoringAsync($$"""
            class C
            {
                void M() {
                    _ = {{before}};
                }
            }
            """, $$"""
            class C
            {
                void M() {
                    _ = {{after}};
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")]
    [InlineData("""
        "\t" [|+|] 1
        """,
        """
        $"\t{1}"
        """)]
    [InlineData("""
        "😀" [|+|] 1
        """,
        """
        $"😀{1}"
        """)]
    [InlineData("""
        "\u2764" [|+|] 1
        """,
        """
        $"\u2764{1}"
        """)]
    [InlineData("""
        "\"" [|+|] 1
        """,
        """
        $"\"{1}"
        """)]
    [InlineData("""
        "{}" [|+|] 1
        """,
        """
        $"{{}}{1}"
        """)]
    public Task TestUnicodeAndEscapeHandling(string before, string after)
        => VerifyCS.VerifyRefactoringAsync($$"""
            class C
            {
                void M() {
                    _ = {{before}};
                }
            }
            """, $$"""
            class C
            {
                void M() {
                    _ = {{after}};
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")]
    [InlineData("""
        "a" [|+|] (1 + 1)
        """,
        """
        $"a{1 + 1}"
        """)]
    [InlineData("""
        "a" [||]+ (1 + 1) + "b" + (2 + 2)
        """,
        """
        $"a{1 + 1}b{2 + 2}"
        """)]
    [InlineData("""
        "a" [|+|] (true ? "t" : "f")
        """,
        """
        $"a{(true ? "t" : "f")}"
        """)]
    [InlineData("""
        "a" [|+|] $"{(1 + 1)}"
        """,
        """
        $"a{(1 + 1)}"
        """)]
    public Task TestRemovalOfSuperflousParenthesis(string before, string after)
        => VerifyCS.VerifyRefactoringAsync($$"""
            class C
            {
                void M() {
                    _ = {{before}};
                }
            }
            """, $$"""
            class C
            {
                void M() {
                    _ = {{after}};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69721")]
    public Task TestToString1()
        => new VerifyCS.Test
        {
            TestCode = """
                struct ValueTuple<T>
                {
                    public T Item1;
                    public T Item2;

                    public override string ToString()
                    {
                        return [||]"(" + Item1.ToString() + ", " + Item2.ToString() + ")";
                    }
                }
                """,
            FixedCode = """
                struct ValueTuple<T>
                {
                    public T Item1;
                    public T Item2;
                
                    public override string ToString()
                    {
                        return $"({Item1.ToString()}, {Item2.ToString()})";
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69721")]
    public Task TestToString1_Net6()
        => new VerifyCS.Test
        {
            TestCode = """
                struct ValueTuple<T>
                {
                    public T Item1;
                    public T Item2;

                    public override string ToString()
                    {
                        return [||]"(" + Item1.ToString() + ", " + Item2.ToString() + ")";
                    }
                }
                """,
            FixedCode = """
                struct ValueTuple<T>
                {
                    public T Item1;
                    public T Item2;
                
                    public override string ToString()
                    {
                        return $"({Item1}, {Item2})";
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69721")]
    public Task TestToString2()
        => new VerifyCS.Test
        {
            TestCode = """
                struct ValueTuple<T>
                {
                    public override string ToString()
                    {
                        return [||]"(" + (1 + 1).ToString() + ", " + (2 + 2).ToString() + ")";
                    }
                }
                """,
            FixedCode = """
                struct ValueTuple<T>
                {
                    public override string ToString()
                    {
                        return $"({(1 + 1).ToString()}, {(2 + 2).ToString()})";
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69721")]
    public Task TestToString2_Net6()
        => new VerifyCS.Test
        {
            TestCode = """
                struct ValueTuple<T>
                {
                    public override string ToString()
                    {
                        return [||]"(" + (1 + 1).ToString() + ", " + (2 + 2).ToString() + ")";
                    }
                }
                """,
            FixedCode = """
                struct ValueTuple<T>
                {
                    public override string ToString()
                    {
                        return $"({1 + 1}, {2 + 2})";
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61256")]
    public Task TestWithRawString()
        => new VerifyCS.Test
        {
            TestCode = """"
                struct ValueTuple
                {
                    public void Goo()
                    {
                        var someVariable = "Some text";

                        var fullText = someVariable [||]+ """
                            Appended line
                            """;
                    }
                }
                """",
            LanguageVersion = LanguageVersion.CSharp11,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68425")]
    public Task TestQuoteCharacter1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    static void Main(string[] args)
                    {
                        var v = [||]'"' + args[0] + '"';
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    static void Main(string[] args)
                    {
                        var v = $"\"{args[0]}\"";
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68425")]
    public Task TestQuoteCharacter2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    static void Main(string[] args)
                    {
                        var v = [||]@"a" + args[0] + '"';
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
}
