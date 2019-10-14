// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString
{
    public class ConvertConcatenationToInterpolatedStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertConcatenationToInterpolatedStringRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnSimpleString()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnConcatenatedStrings1()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"" + ""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnConcatenatedStrings2()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""string"" + [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnConcatenatedStrings3()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""string"" + '.' + [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithStringOnLeft()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestRightSideOfString()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithStringOnRight()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithComplexExpressionOnLeft()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithTrivia1()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithComplexExpressions()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithEscapes1()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithEscapes2()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithVerbatimString1()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithMixedStringTypes1()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]@""string"" + 2 + ""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithMixedStringTypes2()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + @""string"" + 2 + [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithMixedStringTypes3()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + @""string"" + 2 + [||]'\n';
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithOverloadedOperator()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithOverloadedOperator2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        var v = d + [||]""string"" + 1;
    }
}");
        }

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations2()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations3()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations4()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + @""C"";
    }
}");
        }

        [WorkItem(20943, "https://github.com/dotnet/roslyn/issues/20943")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithDynamic1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        dynamic a = ""b"";
        string c = [||]""d"" + a + ""e"";
    }
}");
        }

        [WorkItem(20943, "https://github.com/dotnet/roslyn/issues/20943")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithDynamic2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        dynamic dynamic = null;
        var x = dynamic.someVal + [||]"" $"";
    }
}");
        }

        [WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithStringLiteralWithBraces()
        {
            {
                await TestInRegularAndScriptAsync(
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

        [WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithStringLiteralWithDoubleBraces()
        {
            {
                await TestInRegularAndScriptAsync(
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

        [WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringLiteralsWithBraces()
        {
            {
                await TestInRegularAndScriptAsync(
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

        [WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithVerbatimStringWithBraces()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(23536, "https://github.com/dotnet/roslyn/issues/23536")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleVerbatimStringsWithBraces()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithSelectionOnEntireToBeInterpolatedString()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithSelectionOnPartOfToBeInterpolatedStringPrefix()
        {
            // see comment in AbstractConvertConcatenationToInterpolatedStringRefactoringProvider:ComputeRefactoringsAsync
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [|""string"" + 1|] + ""string"";
    }
}");
        }

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithSelectionOnPartOfToBeInterpolatedStringSuffix()
        {
            // see comment in AbstractConvertConcatenationToInterpolatedStringRefactoringProvider:ComputeRefactoringsAsync
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""string"" + [|1 + ""string""|];
    }
}");
        }

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithSelectionOnMiddlePartOfToBeInterpolatedString()
        {
            // see comment in AbstractConvertConcatenationToInterpolatedStringRefactoringProvider:ComputeRefactoringsAsync
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""a"" + [|1 + ""string""|] + ""b"";
    }
}");
        }

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithSelectionExceedingToBeInterpolatedString()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithCaretBeforeNonStringToken()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithCaretAfterNonStringToken()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithCaretBeforePlusToken()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithCaretAfterPlusToken()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithCaretBeforeLastPlusToken()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(16981, "https://github.com/dotnet/roslyn/issues/16981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithCaretAfterLastPlusToken()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(32864, "https://github.com/dotnet/roslyn/issues/32864")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestConcatenationWithNoStringLiterals()
        {
            await TestInRegularAndScriptAsync(
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
        var v = $""{1}{(""string"")}"";
    }
}");
        }

        [WorkItem(37324, "https://github.com/dotnet/roslyn/issues/37324")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestConcatenationWithChar()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(37324, "https://github.com/dotnet/roslyn/issues/37324")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestConcatenationWithCharAfterStringLiteral()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(37324, "https://github.com/dotnet/roslyn/issues/37324")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestConcatenationWithCharBeforeStringLiteral()
        {
            await TestInRegularAndScriptAsync(
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
    }
}
