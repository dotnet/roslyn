// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using CSharpLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString.CSharpConvertConcatenationToInterpolatedStringRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
    public class ConvertConcatenationToInterpolatedStringTests
    {
        [Fact]
        public async Task TestMissingOnSimpleString()
        {
            var code = @"
public class C
{
    void M()
    {
        var v = [||]""string"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestMissingOnConcatenatedStrings1()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""string"" + ""string"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestMissingOnConcatenatedStrings2()
        {
            var code = @"public class C
{
    void M()
    {
        var v = ""string"" + [||]""string"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestMissingOnConcatenatedStrings3()
        {
            var code = @"public class C
{
    void M()
    {
        var v = ""string"" + '.' + [||]""string"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestWithStringOnLeft()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"" + 1;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string{1}"";
    }
}");
        }

        [Fact]
        public async Task TestRightSideOfString()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = ""string""[||] + 1;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string{1}"";
    }
}");
        }

        [Fact]
        public async Task TestWithStringOnRight()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1}string"";
    }
}");
        }

        [Fact]
        public async Task TestWithComplexExpressionOnLeft()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 1 + 2 + [||]""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1 + 2}string"";
    }
}");
        }

        [Fact]
        public async Task TestWithTrivia1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
public class C
{
    void M()
    {
        var v =
            // Leading trivia
            1 + 2 + [||]""string"" /* trailing trivia */;
    }
}",
@"
public class C
{
    void M()
    {
        var v =
            // Leading trivia
            $""{1 + 2}string"" /* trailing trivia */;
    }
}");
        }

        [Fact]
        public async Task TestWithComplexExpressions()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 1 + 2 + [||]""string"" + 3 + 4;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1 + 2}string{3}{4}"";
    }
}");
        }

        [Fact]
        public async Task TestWithEscapes1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
public class C
{
    void M()
    {
        var v = ""\r"" + 2 + [||]""string"" + 3 + ""\n"";
    }
}",
@"
public class C
{
    void M()
    {
        var v = $""\r{2}string{3}\n"";
    }
}");
        }

        [Fact]
        public async Task TestWithEscapes2()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"
public class C
{
    void M()
    {
        var v = ""\\r"" + 2 + [||]""string"" + 3 + ""\\n"";
    }
}",
@"
public class C
{
    void M()
    {
        var v = $""\\r{2}string{3}\\n"";
    }
}");
        }

        [Fact]
        public async Task TestWithVerbatimString1()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]@""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $@""{1}string"";
    }
}");
        }

        [Fact]
        public async Task TestMissingWithMixedStringTypes1()
        {
            var code = @"public class C
{
    void M()
    {
        var v = 1 + [||]@""string"" + 2 + ""string"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestMissingWithMixedStringTypes2()
        {
            var code = @"public class C
{
    void M()
    {
        var v = 1 + @""string"" + 2 + [||]""string"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestMissingWithMixedStringTypes3()
        {
            var code = @"public class C
{
    void M()
    {
        var v = 1 + @""string"" + 2 + [||]'\n';
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact]
        public async Task TestWithOverloadedOperator()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class D
{
    public static bool operator +(D d, string s) => false;
    public static bool operator +(string s, D d) => false;
}

public class C
{
    void M()
    {
        D d = null;
        var v = 1 + [||]""string"" + d;
    }
}",
@"public class D
{
    public static bool operator +(D d, string s) => false;
    public static bool operator +(string s, D d) => false;
}

public class C
{
    void M()
    {
        D d = null;
        var v = $""{1}string"" + d;
    }
}");
        }

        [Fact]
        public async Task TestWithOverloadedOperator2()
        {
            var code = @"public class D
{
    public static int operator +(D d, string s) => 0;
    public static int operator +(string s, D d) => 0;
}

public class C
{
    void M()
    {
        D d = null;
        var v = d + [||]""string"" + 1;
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
        public async Task TestWithMultipleStringConcatinations()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + ""C"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""A{1}BC"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
        public async Task TestWithMultipleStringConcatinations2()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + [||]""B"" + ""C"" + 1;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""ABC{1}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
        public async Task TestWithMultipleStringConcatinations3()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + ""C"" + 2 +""D""+ ""E""+ ""F"" + 3;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""A{1}BC{2}DEF{3}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16820")]
        public async Task TestWithMultipleStringConcatinations4()
        {
            var code = @"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + @""C"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20943")]
        public async Task TestMissingWithDynamic1()
        {
            var code = @"class C
{
    void M()
    {
        dynamic a = ""b"";
        string c = [||]""d"" + a + ""e"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20943")]
        public async Task TestMissingWithDynamic2()
        {
            var code = @"class C
{
    void M()
    {
        dynamic dynamic = null;
        var x = dynamic.someVal + [||]"" $"";
    }
}";

            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
        public async Task TestWithStringLiteralWithBraces()
        {
            {
                await VerifyCS.VerifyRefactoringAsync(
    @"public class C
{
    void M()
    {
        var v = 1 + [||]""{string}"";
    }
}",
    @"public class C
{
    void M()
    {
        var v = $""{1}{{string}}"";
    }
}");
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
        public async Task TestWithStringLiteralWithBraces2()
        {
            {
                await VerifyCS.VerifyRefactoringAsync(
    @"public class C
{
    void M()
    {
        var v = 1 + [||]""{string}"" + ""{string}"";
    }
}",
    @"public class C
{
    void M()
    {
        var v = $""{1}{{string}}{{string}}"";
    }
}");
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
        public async Task TestWithStringLiteralWithDoubleBraces()
        {
            {
                await VerifyCS.VerifyRefactoringAsync(
    @"public class C
{
    void M()
    {
        var v = 1 + [||]""{{string}}"";
    }
}",
    @"public class C
{
    void M()
    {
        var v = $""{1}{{{{string}}}}"";
    }
}");
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
        public async Task TestWithMultipleStringLiteralsWithBraces()
        {
            {
                await VerifyCS.VerifyRefactoringAsync(
    @"public class C
{
    void M()
    {
        var v = ""{"" + 1 + [||]""}"";
    }
}",
    @"public class C
{
    void M()
    {
        var v = $""{{{1}}}"";
    }
}");
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
        public async Task TestWithVerbatimStringWithBraces()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]@""{string}"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $@""{1}{{string}}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23536")]
        public async Task TestWithMultipleVerbatimStringsWithBraces()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = @""{"" + 1 + [||]@""}"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $@""{{{1}}}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithSelectionOnEntireToBeInterpolatedString()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = [|""string"" + 1|];
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string{1}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestMissingWithSelectionOnPartOfToBeInterpolatedStringPrefix()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [|""string"" + 1|] + ""string"";
    }
}";

            // see comment in AbstractConvertConcatenationToInterpolatedStringRefactoringProvider:ComputeRefactoringsAsync
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestMissingWithSelectionOnPartOfToBeInterpolatedStringSuffix()
        {
            var code = @"public class C
{
    void M()
    {
        var v = ""string"" + [|1 + ""string""|];
    }
}";

            // see comment in AbstractConvertConcatenationToInterpolatedStringRefactoringProvider:ComputeRefactoringsAsync
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestMissingWithSelectionOnMiddlePartOfToBeInterpolatedString()
        {
            var code = @"public class C
{
    void M()
    {
        var v = ""a"" + [|1 + ""string""|] + ""b"";
    }
}";

            // see comment in AbstractConvertConcatenationToInterpolatedStringRefactoringProvider:ComputeRefactoringsAsync
            await VerifyCS.VerifyRefactoringAsync(code, code);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithSelectionExceedingToBeInterpolatedString()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        [|var v = ""string"" + 1|];
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string{1}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithCaretBeforeNonStringToken()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = [||]3 + ""string"" + 1 + ""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{3}string{1}string"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithCaretAfterNonStringToken()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 3[||] + ""string"" + 1 + ""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{3}string{1}string"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithCaretBeforePlusToken()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 3 [||]+ ""string"" + 1 + ""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{3}string{1}string"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithCaretAfterPlusToken()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 3 +[||] ""string"" + 1 + ""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{3}string{1}string"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithCaretBeforeLastPlusToken()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 3 + ""string"" + 1 [||]+ ""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{3}string{1}string"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16981")]
        public async Task TestWithCaretAfterLastPlusToken()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 3 + ""string"" + 1 +[||] ""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{3}string{1}string"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32864")]
        public async Task TestConcatenationWithNoStringLiterals()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = 1 [||]+ (""string"");
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1}{""string""}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")]
        public async Task TestConcatenationWithChar()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var hello = ""hello"";
        var world = ""world"";
        var str = hello [||]+ ' ' + world;
    }
}",
@"public class C
{
    void M()
    {
        var hello = ""hello"";
        var world = ""world"";
        var str = $""{hello} {world}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")]
        public async Task TestConcatenationWithCharAfterStringLiteral()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var world = ""world"";
        var str = ""hello"" [||]+ ' ' + world;
    }
}",
@"public class C
{
    void M()
    {
        var world = ""world"";
        var str = $""hello {world}"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37324")]
        public async Task TestConcatenationWithCharBeforeStringLiteral()
        {
            await VerifyCS.VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var hello = ""hello"";
        var str = hello [||]+ ' ' + ""world"";
    }
}",
@"public class C
{
    void M()
    {
        var hello = ""hello"";
        var str = $""{hello} world"";
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
        public async Task TestConcatenationWithConstMember()
        {
            var code = @"
class C
{
    const string Hello = ""Hello"";
    const string World = ""World"";
    const string Message = Hello + "" "" + [||]World;
}";
            var fixedCode = @"
class C
{
    const string Hello = ""Hello"";
    const string World = ""World"";
    const string Message = $""{Hello} {World}"";
}";

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = code,
                FixedCode = code,
            }.RunAsync();

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.Preview,
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
        public async Task TestConcatenationWithConstDeclaration()
        {
            var code = @"
class C
{
    void M() {
        const string Hello = ""Hello"";
        const string World = ""World"";
        const string Message = Hello + "" "" + [||]World;
    }
}";
            var fixedCode = @"
class C
{
    void M() {
        const string Hello = ""Hello"";
        const string World = ""World"";
        const string Message = $""{Hello} {World}"";
    }
}";

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.CSharp9,
                TestCode = code,
                FixedCode = code,
            }.RunAsync();

            await new VerifyCS.Test
            {
                LanguageVersion = CSharpLanguageVersion.Preview,
                TestCode = code,
                FixedCode = fixedCode,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40413")]
        public async Task TestConcatenationWithInlineString()
        {
            await VerifyCS.VerifyRefactoringAsync(@"
using System;
class C
{
    void M() {
        const string Hello = ""Hello"";
        const string World = ""World"";
        Console.WriteLine(Hello + "" "" + [||]World);
    }
}",
@"
using System;
class C
{
    void M() {
        const string Hello = ""Hello"";
        const string World = ""World"";
        Console.WriteLine($""{Hello} {World}"");
    }
}");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")]
        [InlineData(@"[|""a"" + $""{1:000}""|]",
                     @"$""a{1:000}""")]
        [InlineData(@"[|""a"" + $""b{1:000}""|]",
                     @"$""ab{1:000}""")]
        [InlineData(@"[|$""a{1:000}"" + ""b""|]",
                     @"$""a{1:000}b""")]
        [InlineData(@"[|""a"" + $""b{1:000}c"" + ""d""|]",
                     @"$""ab{1:000}cd""")]
        [InlineData(@"[|""a"" + $""{1:000}b"" + ""c""|]",
                     @"$""a{1:000}bc""")]
        [InlineData(@"[|""a"" + $""{1:000}"" + $""{2:000}"" + ""b""|]",
                     @"$""a{1:000}{2:000}b""")]
        [InlineData(@"[|@""a"" + @$""{1:000}""|]",
                     @"$@""a{1:000}""")]
        [InlineData(@"[|@""a"" + $""{1:000}""|]",
                     @"$@""a{$""{1:000}""}""")]
        [InlineData(@"[|""a"" + @$""{1:000}""|]",
                     @"$""a{@$""{1:000}""}""")]
        public async Task TestInliningOfInterpolatedString(string before, string after)
        {
            var initialMarkup = $@"
class C
{{
    void M() {{
        _ = {before};
    }}
}}";
            var expected = $@"
class C
{{
    void M() {{
        _ = {after};
    }}
}}";
            await VerifyCS.VerifyRefactoringAsync(initialMarkup, expected);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")]
        [InlineData(@"""\t"" [|+|] 1",
                   @"$""\t{1}""")]
        [InlineData(@"""😀"" [|+|] 1",
                   @"$""😀{1}""")]
        [InlineData(@"""\u2764"" [|+|] 1",
                   @"$""\u2764{1}""")]
        [InlineData(@"""\"""" [|+|] 1",
                   @"$""\""{1}""")]
        [InlineData(@"""{}"" [|+|] 1",
                   @"$""{{}}{1}""")]
        public async Task TestUnicodeAndEscapeHandling(string before, string after)
        {
            var initialMarkup = $@"
class C
{{
    void M() {{
        _ = {before};
    }}
}}";
            var expected = $@"
class C
{{
    void M() {{
        _ = {after};
    }}
}}";
            await VerifyCS.VerifyRefactoringAsync(initialMarkup, expected);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49229")]
        [InlineData(@"""a"" [|+|] (1 + 1)",
                   @"$""a{1 + 1}""")]
        [InlineData(@"""a"" [||]+ (1 + 1) + ""b"" + (2 + 2)",
                   @"$""a{1 + 1}b{2 + 2}""")]
        [InlineData(@"""a"" [|+|] (true ? ""t"" : ""f"")",
                   @"$""a{(true ? ""t"" : ""f"")}""")]
        [InlineData(@"""a"" [|+|] $""{(1 + 1)}""",
                   @"$""a{(1 + 1)}""")]
        public async Task TestRemovalOfSuperflousParenthesis(string before, string after)
        {
            var initialMarkup = $@"
class C
{{
    void M() {{
        _ = {before};
    }}
}}";
            var expected = $@"
class C
{{
    void M() {{
        _ = {after};
    }}
}}";
            await VerifyCS.VerifyRefactoringAsync(initialMarkup, expected);
        }
    }
}
